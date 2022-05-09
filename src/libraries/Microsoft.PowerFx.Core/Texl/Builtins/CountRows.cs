// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // CountRows(source:*)
    internal sealed class CountRowsFunction : FunctionWithTableInput
    {
        public const string CountRowsInvariantFunctionName = "CountRows";

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Count;

        public CountRowsFunction()
            : base(CountRowsInvariantFunctionName, TexlStrings.AboutCountRows, FunctionCategories.Table | FunctionCategories.MathAndStat, DType.Number, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            return false;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CountArg1 };
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            return TryGetValidDataSourceForDelegation(callNode, binding, out var dataSource, out var preferredFunctionDelegationCapability);
        }

        // See if CountDistinct delegation is available. If true, we can make use of it on primary key as a workaround for CountRows delegation
        internal bool TryGetValidDataSourceForDelegation(CallNode callNode, TexlBinding binding, out IExternalDataSource dataSource, out DelegationCapability preferredFunctionDelegationCapability)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            preferredFunctionDelegationCapability = FunctionDelegationCapability;

            // We ensure Document is available because some tests run with a null Document.
            if ((binding.Document != null
                && binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled)
                && TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out dataSource)
                && !ExpressionContainsView(callNode, binding))
            {
                // Check that target table is not an expanded entity (1-N/N-N relationships)
                // TASK 9966488: Enable CountRows/CountIf delegation for table relationships
                var args = callNode.Args.Children.VerifyValue();
                if (args.Length > 0)
                {
                    if (binding.GetType(args[0]).HasExpandInfo)
                    {
                        SuggestDelegationHint(callNode, binding);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.CountDistinct, out dataSource);
            if (dataSource != null && dataSource.IsDelegatable)
            {
                binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, callNode, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Name);
            }

            return false;
        }

        private bool ExpressionContainsView(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            var viewFinderVisitor = new ViewFinderVisitor(binding);
            callNode.Accept(viewFinderVisitor);

            return viewFinderVisitor.ContainsView;
        }
    }

    // CountRows(arg: O)
    internal sealed class CountRowsFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public CountRowsFunction_UO()
            : base(CountRowsFunction.CountRowsInvariantFunctionName, TexlStrings.AboutCountRows, FunctionCategories.Table | FunctionCategories.MathAndStat, DType.Number, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CountArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
