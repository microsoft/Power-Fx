// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // LinearGradient(startColor:c, endColor:c, angle:n) : Gradient
    internal sealed class LinearGradientFunction : BuiltinFunction
    {
        public const string LinearGradientInvariantName = "LinearGradient";

        public override bool IsSelfContained => true;

        public LinearGradientFunction()
            : base(
                LinearGradientInvariantName,
                TexlStrings.AboutLinearGradient,
                FunctionCategories.Color,
                DType.Gradient,
                0,
                3,
                3,
                DType.Color,
                DType.Color,
                DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[]
            {
                TexlStrings.LinearGradientArg1,
                TexlStrings.LinearGradientArg2,
                TexlStrings.LinearGradientArg3,
            };
        }
    }
}
