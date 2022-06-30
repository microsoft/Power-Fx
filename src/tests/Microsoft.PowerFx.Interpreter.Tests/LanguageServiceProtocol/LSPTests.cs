// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.LanguageServiceProtocol
{
    public class LSPTests
    {
        [Theory]
        [InlineData("abcdef", 0, 1, 0, 1, 1, 1)]
        [InlineData("abcdef", 1, 2, 1, 2, 1, 1)]
        [InlineData("abcdef", 5, 6, 5, 6, 1, 1)]
        [InlineData("a", 0, 0, 0, 0, 1, 1)]
        [InlineData("a", 1, 1, 1, 1, 1, 1)]
        [InlineData("a\r\nb", 1, 1, 1, 1, 1, 1)]
        [InlineData("a\r\nb", 0, 1, 0, 1, 1, 1)]
        [InlineData("a\r\nb", 1, 2, 1, 1, 1, 1)]
        [InlineData("a\r\nb", 2, 2, 1, 1, 1, 1)]
        [InlineData("a\r\nb", 2, 3, 1, 0, 1, 2)]
        [InlineData("a\r\nb", 2, 4, 1, 1, 1, 2)]
        [InlineData("a\r\nb", 3, 4, 0, 1, 2, 2)]
        [InlineData("abcdefgh\r\nb", 7, 11, 7, 1, 1, 2)]
        public void TestGetRange(string expression, int min, int lim, char startChar, char endChar, int startLine, int endLine)
        {
            var range = LanguageServer.GetRange(expression, new Span(min, lim));

            Assert.Equal(startChar, range.Start.Character);
            Assert.Equal(endChar, range.End.Character);
            Assert.Equal(startLine, range.Start.Line);
            Assert.Equal(endLine, range.End.Line);
        }
    }
}
