// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class IdentTests : PowerFxTest
    {
        [Theory]
        [InlineData("x")]
        [InlineData("ident with space")]
        [InlineData("ident with    multiple  spaces")]
        [InlineData(" name ")]
        [InlineData("123\u00ae")]
        [InlineData(" ", "_ ")]
        [InlineData("  ", "_  ")]
        [InlineData("'")]
        [InlineData("''")]
        [InlineData("", "_")]
        [InlineData("***", null)]
        public void MakeValidIdentifier(string input, string expected = null)
        {
            if (expected == null)
            {
                expected = input;
            }

            var validIdentifier = IdentToken.MakeValidIdentifier(input);
            var token = TexlLexer.LocalizedInstance.GetTokens(validIdentifier)[0] as IdentToken;
            Assert.Equal(expected, token.Name.Value);
        }

        [Fact]
        public void MakeValidIdentifierNull()
        {
            Assert.Throws<ArgumentNullException>(() => IdentToken.MakeValidIdentifier(null));
        }
    }
}
