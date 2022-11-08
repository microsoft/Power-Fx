// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

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

        /// <summary>
        /// This is used at IR phase to convert all possible blank args to zero.
        /// </summary>
        internal override IR.Nodes.CallNode CreateIRCallNode(IRTranslator.IRTranslatorContext context, PowerFx.Syntax.CallNode node, List<IntermediateNode> args, ScopeSymbol scope)
        {
            args[0] = BlankToZeroIRCallNode(args[0], context.GetIRContext(node.Args.Children[0]));
            return base.CreateIRCallNode(context, node, args, scope);
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
