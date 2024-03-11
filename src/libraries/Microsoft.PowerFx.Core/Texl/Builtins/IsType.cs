// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsType(record:P, table:*[]): b
    internal sealed class IsTypeFunction : BuiltinFunction
    {
        public const string IsTypeInvariantFunctionName = "IsType";

        public override bool IsAsync => true;
        
        public override bool IsSelfContained => true;
        
        public override bool SupportsParamCoercion => false;

        public IsTypeFunction()
            : base(IsTypeInvariantFunctionName, TexlStrings.AboutIsType, FunctionCategories.Table, DType.Boolean, 0, 2, 2, DType.Error /* Polymorphic type is checked in override */, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsTypeArg1, TexlStrings.IsTypeArg2 };
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == 2);
            Contracts.Assert(argTypes.Length == 2);
            Contracts.AssertValue(errors);

            // Check if first argument is poly type or an activity pointer
            if (!argTypes[0].IsPolymorphic && !argTypes[0].IsActivityPointer)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrBadType_ExpectedType_ProvidedType, DKind.Polymorphic.ToString(), argTypes[0].GetKindString());
                return;
            }

            // !!! This code can be shared among functions that require a connected data source.
            bool isConnected = binding.EntityScope != null
                && binding.EntityScope.TryGetDataSource(args[1], out IExternalDataSource dataSourceInfo)
                && (dataSourceInfo.Kind == DataSourceKind.Connected || dataSourceInfo.Kind == DataSourceKind.CdsNative);

            if (!isConnected)
            {
                errors.EnsureError(args[0], TexlStrings.ErrAsTypeAndIsTypeExpectConnectedDataSource);
            }
        }

        protected override bool RequiresPagedDataForParamCore(TexlNode[] args, int paramIndex, TexlBinding binding)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.Assert(paramIndex >= 0 && paramIndex < args.Length);
            Contracts.AssertValue(binding);
            Contracts.Assert(binding.IsPageable(args[paramIndex].VerifyValue()));

            // For the second argument, we need only metadata. No actual data from datasource is required.
            return paramIndex != 1;
        }
    }
}
