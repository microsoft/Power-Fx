// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Syntax
{
    /// <summary>
    /// The identifier during parsing.
    /// </summary>
    [ThreadSafeImmutable]
    public sealed class Identifier
    {
        internal readonly Token AtToken; // The "@" token, if any. May be null.
        internal readonly IdentToken Token;

        /// <summary>
        /// The simple name of the identifier.
        /// </summary>
        public DName Name { get; }

        /// <summary>
        /// The namespace of the identifier.
        /// </summary>
        public DPath Namespace { get; }

        internal bool HasAtToken => AtToken != null;

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
