// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // LookUp(source:*, predicate, [projectionFunc])
    internal sealed class LookUpFunction : FilterFunctionBase
    {
        public override bool HasPreciseErrors => true;

        public LookUpFunction()
            : base("LookUp", TexlStrings.AboutLookUp, FunctionCategories.Table, DType.Unknown, 0x6, 2, 3, DType.EmptyTable, DType.Boolean)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.LookUpArg1, TexlStrings.LookUpArg2 };
            yield return new[] { TexlStrings.LookUpArg1, TexlStrings.LookUpArg2, TexlStrings.LookUpArg3 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 2 && args.Length <= 3);
            Contracts.AssertValue(errors);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // The return type is dictated by the last argument (projection) if one exists. Otherwise it's based on first argument (source).
            returnType = args.Length == 2 ? argTypes[0].ToRecord() : argTypes[2];

            // Ensure that the arg at index 1 is boolean or can coerece to boolean if they are OptionSetValues.
            if (argTypes[1].Kind == DKind.OptionSetValue && argTypes[1].CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, context.Features))
            {
                CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], DType.Boolean, allowDupes: true);
            }
            else if (!DType.Boolean.Accepts(argTypes[1], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrBooleanExpected);
                fValid = false;
            }

            return fValid;
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            // LookUp always generates non-pageable result.
            return false;
        }

        // Verifies if given callnode can be server delegatable or not.
        // Return true if
        //        - Arg0 is delegatable ds and supports filter operation.
        //        - All predicates to filter are delegatable if each firstname/binary/unary/dottedname/call node in each predicate satisfies delegation criteria set by delegation strategy for each node.
        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            FilterOpMetadata metadata = null;
            if (TryGetEntityMetadata(callNode, binding, out IDelegationMetadata delegationMetadata))
            {
                if (!TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.ArrayLookup, out _))
                {
                    SuggestDelegationHint(callNode, binding);
                    return false;
                }

                metadata = delegationMetadata.FilterDelegationMetadata.VerifyValue();
            }
            else
            {
                if (!TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out var dataSource))
                {
                    return false;
                }

                metadata = dataSource.DelegationMetadata.FilterDelegationMetadata;
            }

            var args = callNode.Args.Children.VerifyValue();
            if (args.Count < 2)
            {
                return false;
            }

            return IsValidDelegatableFilterPredicateNode(args[1], binding, metadata);
        }

        public override bool IsEcsExcemptedLambda(int index)
        {
            // Only the second argument for lookup is an ECS excempted lambda
            return index == 1;
        }

        public override ICallNodeDelegatableNodeValidationStrategy GetCallNodeDelegationStrategy()
        {
            return new LookUpCallNodeDelegationStrategy(this);
        }

        public bool IsValidDelegatableReductionNode(CallNode callNode, TexlNode node, TexlBinding binding)
        {
            // TODO: how to separate out this logic without duplications
            FilterOpMetadata filterMetadata = null;
            if (TryGetEntityMetadata(callNode, binding, out IDelegationMetadata delegationMetadata))
            {
                if (!TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.ArrayLookup, out _))
                {
                    SuggestDelegationHint(callNode, binding);
                    return false;
                }

                filterMetadata = delegationMetadata.FilterDelegationMetadata.VerifyValue();
            }
            else
            {
                if (!TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out var dataSource))
                {
                    return false;
                }

                filterMetadata = dataSource.DelegationMetadata.FilterDelegationMetadata;
            }

            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            var firstNameStrategy = GetFirstNameNodeDelegationStrategy();
            var dottedNameStrategy = GetDottedNameNodeDelegationStrategy();
            var cNodeStrategy = GetCallNodeDelegationStrategy();

            NodeKind kind;
            kind = node.Kind;

            // what cases actually need to be handled here?
            switch (kind)
            {
                case NodeKind.BinaryOp:
                    {
                        var opNode = node.AsBinaryOp();
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
                        if (!firstNameStrategy.IsValidFirstNameNode(node.AsFirstName(), binding, null))
                        {
                            return false;
                        }

                        break;
                    }

                case NodeKind.DottedName:
                    {
                        if (!dottedNameStrategy.IsValidDottedNameNode(node.AsDottedName(), binding, filterMetadata, null))
                        {
                            return false;
                        }

                        break;
                    }

                case NodeKind.UnaryOp:
                    {
                        var opNode = node.AsUnaryOpLit();
                        var unaryOpNodeValidationStrategy = GetOpDelegationStrategy(opNode.Op);
                        Contracts.AssertValue(opNode);

                        if (!unaryOpNodeValidationStrategy.IsSupportedOpNode(opNode, filterMetadata, binding))
                        {
                            SuggestDelegationHint(node, binding);
                            return false;
                        }

                        break;
                    }

                case NodeKind.Call:
                    {
                        if (!cNodeStrategy.IsValidCallNode(node.AsCall(), binding, filterMetadata))
                        {
                            return false;
                        }

                        break;
                    }

                default:
                    SuggestDelegationHint(node, binding, string.Format(CultureInfo.InvariantCulture, "Not supported node {0}.", kind));
                    return false;
            }

            return true;
        }
    }

    internal sealed class LookUpCallNodeDelegationStrategy : DelegationValidationStrategy
    {
        public LookUpCallNodeDelegationStrategy(TexlFunction function)
            : base(function)
        {
        }

        public override bool IsValidCallNode(CallNode node, TexlBinding binding, OperationCapabilityMetadata metadata, TexlFunction trackingFunction = null)
        {
            var function = binding.GetInfo(node)?.Function;
            var args = node.Args.Children.VerifyValue();          

            // TODO: for now, try to use the top level filtering logic for filters
            if (function is LookUpFunction lookup && args.Count > 2 && !lookup.IsValidDelegatableReductionNode(node, args[2], binding))
            {
                this.SuggestDelegationHint(args[2], binding);
                return false;
            }

            return IsValidCallNodeInternal(node, binding, metadata, trackingFunction ?? Function);
        }
    }
}
