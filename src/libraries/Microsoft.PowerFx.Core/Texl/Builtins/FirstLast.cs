// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // First(source:*)
    // Last(source:*)
    internal sealed class FirstLastFunction : FunctionWithTableInput
    {
        public override bool RequiresErrorContext { get { return _isFirst; } }
        public override bool IsSelfContained => true;        
        public override bool SupportsParamCoercion => false;

        private bool _isFirst;

        public FirstLastFunction(bool isFirst)
            : base(isFirst ? "First" : "Last", isFirst ? TexlStrings.AboutFirst : TexlStrings.AboutLast, FunctionCategories.Table, DType.EmptyRecord, 0, 1, 1, DType.EmptyTable)
        {
            _isFirst = isFirst;
        }

        public override DelegationCapability FunctionDelegationCapability { get { return DelegationCapability.Top; } }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding) { return false; }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new [] { TexlStrings.FirstLastArg1 };
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            bool fArgsValid = base.CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            DType arg0Type = argTypes[0];
            if (arg0Type.IsTable)
                returnType = arg0Type.ToRecord();
            else
            {
                returnType = arg0Type.IsRecord ? arg0Type : DType.Error;
                fArgsValid = false;
            }

            return fArgsValid;
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            IExternalDataSource dataSource = null;

            // Only delegate First, not last
            if (!_isFirst)
                return false;

            // If has top capability (e.g. Dataverse)
            if (TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out dataSource))
            {
                return true;
            }

            // If is a client-side pageable data source
            if (TryGetDataSource(callNode, binding, out dataSource) && dataSource.Kind == DataSourceKind.Connected && dataSource.IsPageable) {
                return true;
            }

            if (dataSource != null && dataSource.IsDelegatable)
                binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, callNode, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Name);

            return false;
        }
    }
}
