// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using static Microsoft.PowerFx.Core.IR.IRTranslator;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abs(number:n)
    // Equivalent DAX function: Abs
    internal sealed class AbsFunction : MathOneArgFunction
    {
        public AbsFunction()
            : base("Abs", TexlStrings.AboutAbs, FunctionCategories.MathAndStat)
        {
        }

        internal override IRFunctionPtr GetIRPreProcessorReplaceBlank()
        {
            return IRReplaceBlank;
        }
    }

    // Abs(E:*[n])
    // Table overload that computes the absolute values of each item in the input table.
    internal sealed class AbsTableFunction : MathOneArgTableFunction
    {
        public AbsTableFunction()
            : base("Abs", TexlStrings.AboutAbsT, FunctionCategories.Table)
        {
        }
    }
}
