// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class AttributeParserTests
    {
        private readonly ParserOptions _parseOptions = new ParserOptions() { AllowAttributes = true };

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

            Assert.Equal("Foo", result.NamedFormulas.First().Ident.Name.Value);
            Assert.Equal("SomeName", result.NamedFormulas.First().Attribute.AttributeName.Name.Value);
            Assert.Equal("Ident", result.NamedFormulas.First().Attribute.AttributeOperation.Name.Value);
        }
    }
}
