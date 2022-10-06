// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // First(source:*)
    // Last(source:*)
    internal sealed class FirstLastFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        private readonly bool _isFirst;

        public FirstLastFunction(bool isFirst)
            : base(isFirst ? "First" : "Last", isFirst ? TexlStrings.AboutFirst : TexlStrings.AboutLast, FunctionCategories.Table, DType.EmptyRecord, 0, 1, 1, DType.EmptyTable)
        {
            _isFirst = isFirst;
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Top;

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            return false;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.FirstLastArg1 };
        }

        public override bool CheckTypes(BindingConfig config, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var fArgsValid = CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var arg0Type = argTypes[0];
            if (arg0Type.IsTable)
            {
                returnType = arg0Type.ToRecord();
            }
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

            // Only delegate First, not last
            if (!_isFirst)
            {
                return false;
            }

            // If has top capability (e.g. Dataverse)
            if (TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out var dataSource))
            {
                return true;
            }

            // If is a client-side pageable data source
            if (TryGetDataSource(callNode, binding, out dataSource) && dataSource.Kind == DataSourceKind.Connected && dataSource.IsPageable)
            {
                return true;
            }

            if (dataSource != null && dataSource.IsDelegatable)
            {
                binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, callNode, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Name);
            }

            return false;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
