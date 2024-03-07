// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Numerics;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base class for all functions "with scope", i.e. that take lambda parameters and participate in filter query server delegation. For example, Filter, LookUp.
    internal abstract class FilterFunctionBase : FunctionWithTableInput
    {
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Filter;

        public override bool HasEcsExcemptLambdas => true;

        public override bool IsSelfContained => true;

        public FilterFunctionBase(string name, TexlStrings.StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : base(name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public override bool TryGetDelegationMetadata(CallNode node, TexlBinding binding, out IDelegationMetadata metadata)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            metadata = null;

            // Get metadata if it's an entity.
            if (binding.TryGetEntityInfo(node.Args.Children[0], out var entityInfo))
            {
                Contracts.AssertValue(entityInfo.ParentDataSource);
                Contracts.AssertValue(entityInfo.ParentDataSource.DataEntityMetadataProvider);

                var metadataProvider = entityInfo.ParentDataSource.DataEntityMetadataProvider;

                if (!metadataProvider.TryGetEntityMetadata(entityInfo.Identity, out var entityMetadata))
                {
                    return false;
                }

                metadata = entityMetadata.DelegationMetadata.VerifyValue();
                return true;
            }

            if (!TryGetValidDataSourceForDelegation(node, binding, FunctionDelegationCapability, out var ds))
            {
                return false;
            }

            metadata = ds.DelegationMetadata;
            return true;
        }

        // Determine whether a node can be delegated as part of a filter predicate.
        // The enforeceBoolean flag determines whether to enforce the return type of the node.  If the node is part of a filter predicate directly, it must return a boolean type.
        // If the node is used in other places, such as a LookUp reduction formula inside a filter, it can return any type.
        protected bool IsValidDelegatableFilterPredicateNode(TexlNode dsNode, TexlBinding binding, FilterOpMetadata filterMetadata, bool generateHints = true, bool enforceBoolean = true)
        {
            Contracts.AssertValue(dsNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(filterMetadata);

            var firstNameStrategy = GetFirstNameNodeDelegationStrategy();
            var dottedNameStrategy = GetDottedNameNodeDelegationStrategy();
            var cNodeStrategy = GetCallNodeDelegationStrategy();

            NodeKind kind;
            kind = dsNode.Kind;

            ErrorContainer originalErrorContainer = null;
            try
            {
                // if hints should not be generated, create a temporary override error container to hold the unwanted warnings/hints
                if (!generateHints)
                {
                    originalErrorContainer = binding.ErrorContainer;
                    binding.OverrideErrorContainer(new ErrorContainer());
                }

                switch (kind)
                {
                    case NodeKind.BinaryOp:
                        {
                            var opNode = dsNode.AsBinaryOp();
                            var binaryOpNodeValidationStrategy = GetOpDelegationStrategy(opNode.Op, opNode);
                            Contracts.AssertValue(opNode);

                            if (!binaryOpNodeValidationStrategy.IsSupportedOpNode(opNode, filterMetadata, binding))
                            {
                                return false;
                            }

                            break;
                        }

                    case NodeKind.FirstName:
                        {
                            if (enforceBoolean && !IsNodeBooleanOptionSetorBooleanFieldorView(dsNode, binding))
                            {
                                SuggestDelegationHint(dsNode, binding);
                                return false;
                            }

                            if (!firstNameStrategy.IsValidFirstNameNode(dsNode.AsFirstName(), binding, null))
                            {
                                return false;
                            }

                            break;
                        }

                    case NodeKind.DottedName:
                        {
                            if (enforceBoolean && !IsNodeBooleanOptionSetorBooleanFieldorView(dsNode, binding))
                            {
                                SuggestDelegationHint(dsNode, binding);
                                return false;
                            }

                            if (!dottedNameStrategy.IsValidDottedNameNode(dsNode.AsDottedName(), binding, filterMetadata, null))
                            {
                                return false;
                            }

                            break;
                        }

                    case NodeKind.UnaryOp:
                        {
                            var opNode = dsNode.AsUnaryOpLit();
                            var unaryOpNodeValidationStrategy = GetOpDelegationStrategy(opNode.Op);
                            Contracts.AssertValue(opNode);

                            if (!unaryOpNodeValidationStrategy.IsSupportedOpNode(opNode, filterMetadata, binding))
                            {
                                SuggestDelegationHint(dsNode, binding);
                                return false;
                            }

                            break;
                        }

                    case NodeKind.Call:
                        {
                            if (!cNodeStrategy.IsValidCallNode(dsNode.AsCall(), binding, filterMetadata))
                            {
                                return false;
                            }

                            break;
                        }

                    default:
                        {
                            if (enforceBoolean && kind != NodeKind.BoolLit)
                            {
                                SuggestDelegationHint(dsNode, binding, string.Format(CultureInfo.InvariantCulture, "Not supported node {0}.", kind));
                                return false;
                            }

                            break;
                        }
                }
            }
            finally
            {
                // restore the original error container, if necessary, discarding any warnings added
                if (originalErrorContainer != null)
                {
                    binding.OverrideErrorContainer(originalErrorContainer);
                }
            }

            return true;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            if (binding.Features.PowerFxV1CompatibilityRules)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    var node = args[i];

                    // If a filter function contains a side effect call as predicate, this is a compilation error.
                    if (binding.HasSideEffects(node))
                    {
                        errors.EnsureError(node, TexlStrings.ErrFilterFunctionBahaviorAsPredicate);
                    }
                }
            }
        }

        private bool IsNodeBooleanOptionSetorBooleanFieldorView(TexlNode dsNode, TexlBinding binding)
        {
            // Only boolean option set, boolean fields and views are allowed to delegate
            var nodeDType = binding.GetType(dsNode);
            return binding.IsValidBooleanDelegableNode(dsNode) || (nodeDType == DType.ViewValue);
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
