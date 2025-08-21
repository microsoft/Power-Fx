// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.Shared.IntellisenseTests
{
    public class UDFIntellisenseTests
    {
        [Theory]
        [InlineData("AddNumbers(x: Number, y: Number): Number = x + y; |")]
        [InlineData("AddNumbers(x: Number, y: Number): Number = x + |")]
        [InlineData("AddNumbers(x: Number, y: Number): Number = x + Su|")]
        [InlineData("AddNumbers(x: Number, y: Number): |")]
        [InlineData("AddNumbers(x: Number, y: |")]
        public void UDFSuggestionTest(string expression)
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var scope = engine.CreateEditorScope();

            // engine.AddUserDefinedFunction(expression);

            var iResult = scope.Suggest(expression, expression.IndexOf('|'), LSPMode.UserDefiniedFunction);

            var suggestions = iResult.Suggestions;

            var sugg = string.Join(",", suggestions.Select(s => s.DisplayText.Text));
            Assert.Equal(" ", sugg);
        }
    }
}
