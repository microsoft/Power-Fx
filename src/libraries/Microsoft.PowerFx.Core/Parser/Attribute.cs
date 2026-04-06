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

        public Attribute(IdentToken name, IReadOnlyList<Token> argumentTokens, Token openBracket)
        {
            Name = name;
            Arguments = argumentTokens.Select(t => t.As<StrLitToken>().Value).ToList();
            ArgumentTokens = argumentTokens;
            OpenBracket = openBracket;
        }
    }
}
