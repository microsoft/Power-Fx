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

        [Fact]
        public void TestNamedFormulaAttributes()
        {
            var result = UserDefinitions.Parse(
            @"
                [SomeName Ident]
                Foo = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            Assert.Equal("Foo", result.NamedFormulas.First().Ident.Name.Value);
            Assert.Equal("SomeName", result.NamedFormulas.First().Attribute.AttributeName.Name.Value);
            Assert.Equal("Ident", result.NamedFormulas.First().Attribute.AttributeOperation.Name.Value);
        }

        [Fact]
        public void TestMultipleNamedFormulaAttributes()
        {
            var result = UserDefinitions.Parse(
            @"
                [SomeName Ident]
                Foo = 123;

                [SomeName1 Ident1]
                Foo1 = 123;

                [SomeName2 Ident2]
                Foo2 = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var formulas = result.NamedFormulas.ToList();

            Assert.Equal(3, result.NamedFormulas.Count());

            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("SomeName", formulas[0].Attribute.AttributeName.Name.Value);
            Assert.Equal("Ident", formulas[0].Attribute.AttributeOperation.Name.Value);

            Assert.Equal("Foo1", formulas[1].Ident.Name.Value);
            Assert.Equal("SomeName1", formulas[1].Attribute.AttributeName.Name.Value);
            Assert.Equal("Ident1", formulas[1].Attribute.AttributeOperation.Name.Value);

            Assert.Equal("Foo2", formulas[2].Ident.Name.Value);
            Assert.Equal("SomeName2", formulas[2].Attribute.AttributeName.Name.Value);
            Assert.Equal("Ident2", formulas[2].Attribute.AttributeOperation.Name.Value);
        }

        [Fact]
        public void TestErrorNamedFormulaAttributes()
        {
            var result = UserDefinitions.Parse(
            @"
                [SomeNa.me Iden()t]
                Foo = 123;

                [SomeName1 Ident1]
                Foo1 = 123;

                [SomeName2 Ident2]
                Foo2 = 123;
            ", _parseOptions);

            Assert.True(result.HasErrors);

            Assert.Equal(2, result.NamedFormulas.Count());

            // Error in the first definition's attribute, it gets skipped and we restart on the next one
            var formulas = result.NamedFormulas.ToList();

            Assert.Equal("Foo1", formulas[0].Ident.Name.Value);
            Assert.Equal("SomeName1", formulas[0].Attribute.AttributeName.Name.Value);
            Assert.Equal("Ident1", formulas[0].Attribute.AttributeOperation.Name.Value);

            Assert.Equal("Foo2", formulas[1].Ident.Name.Value);
            Assert.Equal("SomeName2", formulas[1].Attribute.AttributeName.Name.Value);
            Assert.Equal("Ident2", formulas[1].Attribute.AttributeOperation.Name.Value);
        }
    }
}
