// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Degrees(number:n)
    // Equivalent Excel function: Degrees
    internal sealed class DegreesFunction : MathOneArgFunction
    {
        public override bool RequiresErrorContext => true;

        public DegreesFunction()
            : base("Degrees", TexlStrings.AboutDegrees, FunctionCategories.MathAndStat)
        { }
    }

    // Degrees(E:*[n])
    // Table overload that computes the degrees values of each item in the input table.
    internal sealed class DegreesTableFunction : MathOneArgTableFunction
    {
        public override bool RequiresErrorContext => true;

        public DegreesTableFunction()
            : base("Degrees", TexlStrings.AboutDegreesT, FunctionCategories.Table)
        { }
    }
}
