// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // ColorFade(color:c, fadeDelta:n)
    internal sealed class ColorFadeFunction : BuiltinFunction
    {
        public override bool IsTrackedInTelemetry => false;
        public override bool SupportsInlining => true;
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => true;

        public ColorFadeFunction()
            : base("ColorFade", TexlStrings.AboutColorFade, FunctionCategories.Color, DType.Color, 0, 2, 2, DType.Color, DType.Number)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Parameters are a numeric color value, and a fadeDelta (-1 to 1)
            yield return new [] { TexlStrings.ColorFadeArg1, TexlStrings.ColorFadeArg2 };
        }
    }
}
