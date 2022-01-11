// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.SourceInformation
{
    internal class IdentifierSource : ITexlSource
    {
        public Identifier Identifier { get; }

        public IEnumerable<Token> Tokens => new[] { Identifier.Token };

        public IEnumerable<ITexlSource> Sources => new[] { this };

        public IdentifierSource(Identifier identifier)
        {
            Contracts.AssertValue(identifier);
            Identifier = identifier;
        }

        public ITexlSource Clone(Dictionary<TexlNode, TexlNode> newNodes, Span span)
        {
            Contracts.AssertValue(newNodes);
            Contracts.AssertAllValues(newNodes.Values);
            Contracts.AssertAllValues(newNodes.Keys);
            return new IdentifierSource(Identifier.Clone(span));
        }
    }
}
