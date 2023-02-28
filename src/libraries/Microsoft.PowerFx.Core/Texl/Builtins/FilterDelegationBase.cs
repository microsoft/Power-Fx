// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        protected bool IsValidDelegatableFilterPredicateNode(TexlNode dsNode, TexlBinding binding, FilterOpMetadata filterMetadata, bool generateHints = true)
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
                            // Only boolean option sets and boolean fields are allowed to delegate
                            var nodeDType = binding.GetType(dsNode);
                            if (!((nodeDType.IsOptionSet && nodeDType.OptionSetInfo != null && nodeDType.OptionSetInfo.IsBooleanValued) || (nodeDType == DType.Boolean && dsNode.Kind != NodeKind.BoolLit))) 
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
                            // Only boolean option set, boolean fields and views are allowed to delegate
                            var nodeDType = binding.GetType(dsNode);
                            if (!((nodeDType.IsOptionSet && nodeDType.OptionSetInfo != null && nodeDType.OptionSetInfo.IsBooleanValued) 
                                || (nodeDType == DType.Boolean && dsNode.Kind != NodeKind.BoolLit)
                                || (nodeDType == DType.ViewValue)))
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
                            if (kind != NodeKind.BoolLit)
                            {
                                SuggestDelegationHint(dsNode, binding, string.Format("Not supported node {0}.", kind));
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
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
