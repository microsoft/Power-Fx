﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base class for all statistical functions with similar signatures that take
    // a table as the first argument, and a value function as the second argument.
    internal abstract class StatisticalTableFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        private readonly bool _nativeDecimal = false;

        public StatisticalTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false)
            : base(name, description, fc, DType.Number, 0x02, 2, 2, DType.EmptyTable, DType.Number)
        {
            ScopeInfo = new FunctionScopeInfo(this, usesAllFieldsInScope: false, acceptsLiteralPredicates: false);
            _nativeDecimal = nativeDecimal;
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            return false;
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
                    TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.NotANumberArgType, callNode, binding, this, DelegationTelemetryInfo.CreateUnsupportArgTelemetryInfo(binding.GetType(args[1])));
                }
                else
                {
                    TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.InvalidArgType, callNode, binding, this, DelegationTelemetryInfo.CreateUnsupportArgTelemetryInfo(binding.GetType(args[1])));
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

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();

            returnType = DetermineNumericFunctionReturnType(_nativeDecimal, context.NumberIsFloat, argTypes[1]);

            if (!CheckType(context, args[1], argTypes[1], returnType, DefaultErrorContainer, ref nodeToCoercedTypeMap))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrNumberExpected);
                nodeToCoercedTypeMap = null;
                return false;
            }

            ScopeInfo?.CheckLiteralPredicates(args, errors);

            return true;
        }
    }
}
