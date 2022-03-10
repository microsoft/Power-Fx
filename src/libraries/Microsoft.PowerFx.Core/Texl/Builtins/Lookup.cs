// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // LookUp(source:*, predicate, [projectionFunc])
    internal sealed class LookUpFunction : FilterFunctionBase
    {
        public override bool RequiresErrorContext => true;

        public override bool SupportsParamCoercion => true;

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

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 2 && args.Length <= 3);
            Contracts.AssertValue(errors);

            var fValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // The return type is dictated by the last argument (projection) if one exists. Otherwise it's based on first argument (source).
            returnType = args.Length == 2 ? argTypes[0].ToRecord() : argTypes[2];

            // Ensure that the arg at index 1 is boolean or can coerece to boolean if they are OptionSetValues.
            if (argTypes[1].Kind == DKind.OptionSetValue && argTypes[1].CoercesTo(DType.Boolean))
            {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], DType.Boolean, allowDupes: true);
            }
            else if (!DType.Boolean.Accepts(argTypes[1]))
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
                if (!binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled ||
                    !TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.ArrayLookup, out _))
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
            if (args.Length > 2 && binding.IsDelegatable(args[2]))
            {
                SuggestDelegationHint(args[2], binding);
                return false;
            }

            if (args.Length < 2)
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
    }
}
