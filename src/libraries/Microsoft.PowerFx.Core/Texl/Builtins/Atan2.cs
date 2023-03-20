// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Atan2(number:x, number:y)
    // Equivalent Excel function: Atan2
    internal sealed class Atan2Function : MathFunction
    {
        public Atan2Function()
            : base("Atan2", TexlStrings.AboutAbs, FunctionCategories.MathAndStat, 2, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.AboutAtan2Arg1, TexlStrings.AboutAtan2Arg2 };
        }
    }

    // Decimal TODO: Atan2 is missing table version
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
