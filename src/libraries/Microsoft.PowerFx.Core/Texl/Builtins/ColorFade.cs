// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // ColorFade(color:c, fadeDelta:n)
    internal sealed class ColorFadeFunction : BuiltinFunction
    {
        public override bool IsTrackedInTelemetry => false;

        public override bool SupportsInlining => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public ColorFadeFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "ColorFade", TexlStrings.AboutColorFade, FunctionCategories.Color, DType.Color, 0, 2, 2, DType.Color, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Parameters are a numeric color value, and a fadeDelta (-1 to 1)
            yield return new[] { TexlStrings.ColorFadeArg1, TexlStrings.ColorFadeArg2 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.ColorEnumString };
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
