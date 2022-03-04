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

        public int Offset { get; }

        public NamedFormula(IdentToken name, TexlNode node, int offset)
        {
            Name = name;
            Node = node;
            Offset = offset;
        }
    }
}
