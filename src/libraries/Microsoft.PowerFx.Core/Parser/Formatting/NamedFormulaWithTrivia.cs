// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Parser
{
    /// <summary>
    /// Named formula and all the trivia in it, this is solely used for formatting.
    /// </summary>
    internal sealed class NamedFormulaWithTrivia
    {
        internal IdentifierWithTrivia Ident { get; }

        internal TexlNodeWithTrivia NodeWithTrivia { get; }

        public NamedFormulaWithTrivia(IdentifierWithTrivia ident, TexlNodeWithTrivia node)
        {
            Contracts.AssertValue(ident);
            Contracts.AssertValue(node);

            Ident = ident;
            NodeWithTrivia = node;
        }
    }
}
