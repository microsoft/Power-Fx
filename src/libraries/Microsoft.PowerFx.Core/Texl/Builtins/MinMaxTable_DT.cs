// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal abstract class MinMaxTableFunction_DT : FunctionWithTableInput
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        private readonly DelegationCapability _delegationCapability;

        public override bool RequiresErrorContext => true;

        public override DelegationCapability FunctionDelegationCapability => _delegationCapability;

        public bool IsMin;

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            return false;
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            if (IsMin)
            {
                return BuiltinFunctionsCore.MinT.GetUniqueTexlRuntimeName(isPrefetching);
            }
            else
            {
                return BuiltinFunctionsCore.MaxT.GetUniqueTexlRuntimeName(isPrefetching);
            }
        }

        public MinMaxTableFunction_DT(bool isMin)
            : base(isMin ? "Min" : "Max", isMin ? TexlStrings.AboutMinT : TexlStrings.AboutMaxT, FunctionCategories.Table, DType.DateTime, 0x02, 2, 2, DType.EmptyTable, DType.DateTime)
        {
            IsMin = isMin;
            ScopeInfo = new FunctionScopeInfo(this, usesAllFieldsInScope: false, acceptsLiteralPredicates: false);
            _delegationCapability = isMin ? DelegationCapability.Min : DelegationCapability.Max;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StatisticalTArg1, TexlStrings.StatisticalTArg2 };
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (FunctionDelegationCapability.Capabilities == DelegationCapability.None)
            {
                return false;
            }

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            if (!TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out var dataSource))
            {
                if (dataSource != null && dataSource.IsDelegatable)
                {
                    binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, callNode, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Name);
                }

                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            if (binding.GetType(args[0]).HasExpandInfo
            || (!binding.IsFullRecordRowScopeAccess(args[1]) && args[1].Kind != NodeKind.FirstName)
            || !binding.IsRowScope(args[1])
            || binding.GetType(args[1]) != DType.Number
            || ExpressionContainsView(callNode, binding))
            {
                SuggestDelegationHint(callNode, binding);

                if (binding.GetType(args[1]) != DType.Number)
                {
                    TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.NotANumberArgType, callNode, binding, this, DelegationTelemetryInfo.CreateEmptyDelegationTelemetryInfo());
                }
                else
                {
                    TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.InvalidArgType, callNode, binding, this, DelegationTelemetryInfo.CreateEmptyDelegationTelemetryInfo());
                }

                return false;
            }

            if (binding.IsFullRecordRowScopeAccess(args[1]))
            {
                return GetDottedNameNodeDelegationStrategy().IsValidDottedNameNode(args[1].AsDottedName(), binding, null, null);
            }

            var firstNameStrategy = GetFirstNameNodeDelegationStrategy().VerifyValue();
            return firstNameStrategy.IsValidFirstNameNode(args[1].AsFirstName(), binding, null);
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
}
