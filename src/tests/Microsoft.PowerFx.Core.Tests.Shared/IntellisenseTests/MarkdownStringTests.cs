// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Intellisense.SignatureHelp;
using Xunit;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    public class MarkdownStringTests
    {
        [Theory]
        [InlineData("1*2", @"1\*2")]
        [InlineData("1#2", @"1\#2")]
        public void Escape(string plainText, string markdown)
        {
            var md = MarkdownString.FromString(plainText);
            Assert.Equal(markdown, md.Markdown); // escaped

            // if we treat it as markdown, then no escaping. 
            var md2 = MarkdownString.FromMarkdown(plainText);
            Assert.Equal(plainText, md2.Markdown);
        }

        [Fact]
        public void Add()
        {
            MarkdownString md1 = MarkdownString.FromMarkdown("1");
            MarkdownString md2 = MarkdownString.FromMarkdown("2");

            MarkdownString md3 = md1 + md2;
            Assert.Equal("12", md3.Markdown);
        }
    }
}
