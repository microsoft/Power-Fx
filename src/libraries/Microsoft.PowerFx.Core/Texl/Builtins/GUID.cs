// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        // Multiple invocations may produce different return values.
        public override bool IsStateless => false;
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => true;

        public GUIDNoArgFunction()
            : base("GUID", TexlStrings.AboutGUID, FunctionCategories.Text, DType.Guid, 0, 0, 0)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[0];
        }
    }

    // GUID(GuidString:s)
    internal sealed class GUIDPureFunction : BuiltinFunction
    {
        public override bool RequiresErrorContext => true;
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => false;

        public GUIDPureFunction()
            : base("GUID", TexlStrings.AboutGUID, FunctionCategories.Text, DType.Guid, 0, 1, 1, DType.String)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.GUIDArg };
        }
    }

}
