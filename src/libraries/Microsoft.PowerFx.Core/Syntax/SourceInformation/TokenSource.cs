// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.SourceInformation
{
    /// <summary>
    /// An individual non-whitespace token that is part of the source for its
    /// holding TexlNode.
    /// </summary>
    internal sealed class TokenSource : ITexlSource
    {
        public Token Token { get; }

        public IEnumerable<Token> Tokens => new[] { Token };

        public IEnumerable<ITexlSource> Sources => new[] { this };

        public TokenSource(Token token)
        {
            Contracts.AssertValue(token);
            Token = token;
        }

        public ITexlSource Clone(Dictionary<TexlNode, TexlNode> newNodes, Span newSpan)
        {
            Contracts.AssertValue(newNodes);
            Contracts.AssertAllValues(newNodes.Values);
            Contracts.AssertAllValues(newNodes.Keys);
            return new TokenSource(Token.Clone(newSpan));
        }

        public override string ToString()
        {
            return Enum.GetName(typeof(TokKind), Token.Kind);
        }
    }
}
