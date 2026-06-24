// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal sealed class Attribute
    {
        public readonly IdentToken Name;

        public readonly IReadOnlyList<string> Arguments;

        public readonly IReadOnlyList<Token> ArgumentTokens;

        public readonly Token OpenBracket;

        // Argument-list and closing tokens, when present. Preserved so consumers (such as
        // IntelliSense) can locate attribute regions even when the argument list is empty or
        // the closing delimiters have not been typed yet.
        public readonly Token OpenParen;

        public readonly Token CloseParen;

        public readonly Token CloseBracket;

        public Attribute(IdentToken name, IReadOnlyList<Token> argumentTokens, Token openBracket, Token openParen = null, Token closeParen = null, Token closeBracket = null)
        {
            Name = name;
            Arguments = argumentTokens.Select(t => t is IdentToken ident ? ident.Name.Value : t.As<StrLitToken>().Value).ToList();
            ArgumentTokens = argumentTokens;
            OpenBracket = openBracket;
            OpenParen = openParen;
            CloseParen = closeParen;
            CloseBracket = closeBracket;
        }
    }
}
