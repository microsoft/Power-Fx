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
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // CountIf(source:*, predicate:b [, predicate:b, ...])
    // Corresponding DAX function: CountAX, CountX
    internal sealed class CountIfFunction : FilterFunctionBase
    {
        public override bool SupportsParamCoercion => true;

        public override bool HasPreciseErrors => true;

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Filter | DelegationCapability.Count;

        public CountIfFunction()
            : base("CountIf", TexlStrings.AboutCountIf, FunctionCategories.Table | FunctionCategories.MathAndStat, DType.Number, -2, 2, int.MaxValue, DType.EmptyTable, DType.Boolean)
        {
            ScopeInfo = new FunctionScopeInfo(this, usesAllFieldsInScope: false);
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            return false;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CountIfArg1, TexlStrings.CountIfArg2 };
            yield return new[] { TexlStrings.CountIfArg1, TexlStrings.CountIfArg2, TexlStrings.CountIfArg2 };
            yield return new[] { TexlStrings.CountIfArg1, TexlStrings.CountIfArg2, TexlStrings.CountIfArg2, TexlStrings.CountIfArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.CountIfArg1, TexlStrings.CountIfArg2);
            }

            return base.GetSignatures(arity);
        }

        protected override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var fValid = base.CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType == DType.Number);

            // Ensure that all the args starting at index 1 are booleans or coerece to boolean if they are OptionSetValues.
            for (var i = 1; i < args.Length; i++)
            {
                if (argTypes[i].Kind == DKind.OptionSetValue && argTypes[i].CoercesTo(DType.Boolean))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], DType.Boolean, allowDupes: true);
                }
                else if (!DType.Boolean.Accepts(argTypes[i]))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrBooleanExpected);
                    fValid = false;
                }
            }

            return fValid;
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            IExternalDataSource dataSource = null;

            // We ensure Document is available because some tests run with a null Document.
            if ((binding.Document != null && !binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled) || !TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out dataSource))
            {
                if (dataSource != null && dataSource.IsDelegatable)
                {
                    binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, callNode, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Name);
                }

                return false;
            }

            var args = callNode.Args.Children.VerifyValue();

            if (args.Length == 0)
            {
                return false;
            }

            // Don't delegate 1-N/N-N counts
            // TASK 9966488: Enable CountRows/CountIf delegation for table relationships
            if (binding.GetType(args[0]).HasExpandInfo)
            {
                SuggestDelegationHint(callNode, binding);
                return false;
            }

            var metadata = dataSource.DelegationMetadata.FilterDelegationMetadata;

            // Validate for each predicate node.
            for (var i = 1; i < args.Length; i++)
            {
                if (!IsValidDelegatableFilterPredicateNode(args[i], binding, metadata))
                {
                    SuggestDelegationHint(callNode, binding);
                    return false;
                }
            }

            return true;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
