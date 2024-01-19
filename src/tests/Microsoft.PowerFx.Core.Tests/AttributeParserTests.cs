// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class AttributeParserTests
    {
        private readonly ParserOptions _parseOptions = new ParserOptions() { };

        [Theory]
        [InlineData(
        @"
            [SomeName Ident]
            Foo = 123;
        ")]
        public void TestNamedFormulaAttributes(string script)
        {
            var result = UserDefinitions.Parse(script, _parseOptions);

            Assert.False(result.HasErrors);
        }
    }
}
