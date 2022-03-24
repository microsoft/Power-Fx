// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax
{
    public sealed class Identifier
    {
        internal readonly Token AtToken; // The "@" token, if any. May be null.
        internal readonly IdentToken Token;

        public DName Name { get; }

        public DPath Namespace { get; }

        public bool HasAtToken => AtToken != null;

        internal Identifier(DPath theNamespace, Token atToken, IdentToken tok)
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

        internal Identifier Clone(Span ts)
        {
            return new Identifier(
                Namespace,
                AtToken?.Clone(ts),
                Token.Clone(ts).As<IdentToken>());
        }

        internal Identifier(IdentToken token)
            : this(DPath.Root, null, token)
        {
        }

        internal Identifier(Token atToken, IdentToken token)
            : this(DPath.Root, atToken, token)
        {
        }
    }
}
