// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax
{
    internal sealed class Identifier
    {
        public readonly Token AtToken; // The "@" token, if any. May be null.
        public readonly IdentToken Token;
        public readonly DName Name;
        public readonly DPath Namespace;

        public Identifier(DPath theNamespace, Token atToken, IdentToken tok)
        {
            Contracts.Assert(theNamespace.IsValid);
            Contracts.AssertValueOrNull(atToken);
            Contracts.AssertValue(tok);
            Contracts.Assert(tok.Name.IsValid);

            Namespace = theNamespace;
            AtToken = atToken;
            Token = tok;
            Name = tok.Name;
        }

        public Identifier Clone(Span ts)
        {
            return new Identifier(
                Namespace,
                AtToken?.Clone(ts),
                Token.Clone(ts).As<IdentToken>());
        }

        public Identifier(IdentToken token)
            : this(DPath.Root, null, token)
        {
        }

        public Identifier(Token atToken, IdentToken token)
            : this(DPath.Root, atToken, token)
        {
        }
    }
}