// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

// These have separate defintions as the one with a string is a pure function
namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // GUID()
    internal sealed class GUIDNoArgFunction : BuiltinFunction
    {
        public const string GUIDFunctionInvariantName = "GUID";

        // Multiple invocations may produce different return values.
        public override bool IsStateless => false;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public GUIDNoArgFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, GUIDFunctionInvariantName, TexlStrings.AboutGUID, FunctionCategories.Text, DType.Guid, 0, 0, 0)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[0];
        }
    }

    // GUID(GuidString:s)
    internal sealed class GUIDPureFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public GUIDPureFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, GUIDNoArgFunction.GUIDFunctionInvariantName, TexlStrings.AboutGUID, FunctionCategories.Text, DType.Guid, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.GUIDArg };
        }
    }

    // GUID(GuidString:uo)
    internal sealed class GUIDPureFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public GUIDPureFunction_UO(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, GUIDNoArgFunction.GUIDFunctionInvariantName, TexlStrings.AboutGUID, FunctionCategories.Text, DType.Guid, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.GUIDArg };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}
