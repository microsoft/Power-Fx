// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Acot(number:n)
    // Equivalent Excel function: Acot
    internal sealed class AcotFunction : MathOneArgFunction
    {
        public override bool RequiresErrorContext => true;

        public AcotFunction()
            : base("Acot", TexlStrings.AboutAcot, FunctionCategories.MathAndStat)
        { }
    }

    // Acot(E:*[n])
    // Table overload that computes the arc cotangent of each item in the input table.
    internal sealed class AcotTableFunction : MathOneArgTableFunction
    {
        public override bool RequiresErrorContext => true;

        public AcotTableFunction()
            : base("Acot", TexlStrings.AboutAcotT, FunctionCategories.Table)
        { }
    }
}
