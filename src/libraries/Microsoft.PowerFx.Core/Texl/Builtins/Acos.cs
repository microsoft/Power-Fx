// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Acos(number:n)
    // Equivalent Excel function: acos
    internal sealed class AcosFunction : MathOneArgFunction
    {
        public override bool RequiresErrorContext => true;

        public AcosFunction()
            : base("Acos", TexlStrings.AboutAcos, FunctionCategories.MathAndStat)
        { }
    }

    // Acos(E:*[n])
    // Table overload that computes the arc cosine of each item in the input table.
    internal sealed class AcosTableFunction : MathOneArgTableFunction
    {
        public override bool RequiresErrorContext => true;

        public AcosTableFunction()
            : base("Acos", TexlStrings.AboutAcosT, FunctionCategories.Table)
        { }
    }
}
