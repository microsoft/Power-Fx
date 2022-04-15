// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax
{
    internal class NamedFormula
    {
        public IdentToken Name { get; }

        public TexlNode Node { get; }

        /// <summary>
        /// Spans for each named formula rule will be relative to this offset
        /// for named formulas x=1;y = 1 + 2; offset for x will be 2 and 7 for y .  
        /// </summary>
        public int Offset { get; }

        public NamedFormula(IdentToken name, TexlNode node, int offset)
        {
            Name = name;
            Node = node;
            Offset = offset;
        }
    }
}
