// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // RGBA(red, green, blue, alpha)
    internal sealed class RGBAFunction : BuiltinFunction
    {
        public override bool IsTrackedInTelemetry => false;
        public override bool SupportsParamCoercion => true;
        public override bool SupportsInlining => true;

        // This is important to set so that calls to RGBA(consts,...) are also considered const
        public override bool IsSelfContained => true;

        public RGBAFunction()
            : base("RGBA", TexlStrings.AboutRGBA, FunctionCategories.Color, DType.Color, 0, 4, 4, DType.Number, DType.Number, DType.Number, DType.Number)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RGBAArg1, TexlStrings.RGBAArg2, TexlStrings.RGBAArg3, TexlStrings.RGBAArg4 };
        }
    }
}
