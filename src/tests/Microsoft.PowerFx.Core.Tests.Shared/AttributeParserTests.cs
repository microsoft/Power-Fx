// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class AttributeParserTests
    {
        private readonly ParserOptions _parseOptions = new ParserOptions() { AllowAttributes = true, AllowEqualOnlyNamedFormulas = true };

        private DefinitionsCheckResult GetCheckResult(string script, SymbolTable symbolTable = null)
        {
            var symbols = symbolTable ?? new SymbolTable();

            // Add primitive types needed for UDF validation
            if (!symbols.NamedTypes.ContainsKey(new DName("Number")))
            {
                symbols.AddType(new DName("Number"), FormulaType.Number);
            }

            var composedSymbolTable = ReadOnlySymbolTable.Compose(symbols);
            var checkResult = new DefinitionsCheckResult();
            return checkResult.SetText(script, _parseOptions)
                .SetBindingInfo(composedSymbolTable);
        }

        [Fact]
        public void TestNamedFormulaAnnotations()
        {
            var result = UserDefinitions.Parse(
            @"
                [SomeName]
                Foo = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            Assert.Equal("Foo", result.NamedFormulas.First().Ident.Name.Value);
            Assert.Equal("SomeName", result.NamedFormulas.First().Attributes[0].Name.Name.Value);
            Assert.Empty(result.NamedFormulas.First().Attributes[0].Arguments);
        }

        [Fact]
        public void TestAnnotationWithStringArgs()
        {
            var result = UserDefinitions.Parse(
            @"
                [MyAttr(""hello"", ""world"")]
                Foo = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var attribute = result.NamedFormulas.First().Attributes[0];
            Assert.Equal("MyAttr", attribute.Name.Name.Value);
            Assert.Equal(2, attribute.Arguments.Count);
            Assert.Equal("hello", attribute.Arguments[0]);
            Assert.Equal("world", attribute.Arguments[1]);
        }

        [Fact]
        public void TestNFAttributeSingleKeyAnd()
        {
            var result = UserDefinitions.Parse(
            @"
                [Partial(""And"")]
                Foo = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var formulas = result.NamedFormulas.ToList();

            Assert.Single(result.NamedFormulas);

            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("Partial", formulas[0].Attributes[0].Name.Name.Value);
            Assert.Equal("And", formulas[0].Attributes[0].Arguments[0]);
        }

        [Theory]
        [InlineData("And", "And")]
        [InlineData("Or", "Or")]
        [InlineData("Record", "MergeRecords")]
        [InlineData("Table", "Table")]
        public void TestNFAttributeOperationsCombined(string op, string combinedFunctionName)
        {
            var result = UserDefinitions.Parse(
            $@"
                [Partial(""{op}"")]
                Foo = false;
                [Partial(""{op}"")]
                Foo = true;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            // Process added the combined nf
            Assert.Equal(3, result.NamedFormulas.Count());

            var genNf = result.NamedFormulas.Last();
            Assert.StartsWith(combinedFunctionName, genNf.Formula.Script);

            // Check that the call in the generated merged expression is tagged as non-source
            var call = genNf.Formula.ParseTree.AsCall();
            Assert.NotNull(call);
            Assert.True(call.Head.Token.IsNonSourceIdentToken);
        }

        [Fact]
        public void TestNFAttributeOperationInvalid()
        {
            var result = UserDefinitions.Parse(
            $@"
                [Partial(""Unknown"")]
                Foo = false;
                [Partial(""Unknown"")]
                Foo = true;
            ", _parseOptions);

            Assert.True(result.HasErrors);

            // Combined expression not generated
            Assert.Equal(2, result.NamedFormulas.Count());
        }

        [Fact]
        public void TestMultipleNamedFormulaAnnotations()
        {
            var result = UserDefinitions.Parse(
            @"
                [SomeName(""arg1"")]
                Foo = 123;

                [SomeName1(""arg2"")]
                Foo1 = 123;

                [SomeName2(""arg3"")]
                Foo2 = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var formulas = result.NamedFormulas.ToList();

            Assert.Equal(3, result.NamedFormulas.Count());

            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("SomeName", formulas[0].Attributes[0].Name.Name.Value);
            Assert.Equal("arg1", formulas[0].Attributes[0].Arguments[0]);

            Assert.Equal("Foo1", formulas[1].Ident.Name.Value);
            Assert.Equal("SomeName1", formulas[1].Attributes[0].Name.Name.Value);
            Assert.Equal("arg2", formulas[1].Attributes[0].Arguments[0]);

            Assert.Equal("Foo2", formulas[2].Ident.Name.Value);
            Assert.Equal("SomeName2", formulas[2].Attributes[0].Name.Name.Value);
            Assert.Equal("arg3", formulas[2].Attributes[0].Arguments[0]);
        }

        [Fact]
        public void TestAnnotationOnUDF()
        {
            var result = UserDefinitions.Parse(
            @"
                [MyAnnotation(""Foo"")]
                MyFunc(): Number = 1;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var udf = result.UDFs.First();
            Assert.Equal("MyFunc", udf.Ident.Name.Value);
            Assert.Single(udf.Attributes);
            Assert.Equal("MyAnnotation", udf.Attributes[0].Name.Name.Value);
            Assert.Single(udf.Attributes[0].Arguments);
            Assert.Equal("Foo", udf.Attributes[0].Arguments[0]);
        }

        [Fact]
        public void TestMultipleAnnotationsOnUDF()
        {
            var result = UserDefinitions.Parse(
            @"
                [Ann1(""x"")] [Ann2(""y"", ""z"")]
                MyFunc(): Number = 1;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var udf = result.UDFs.First();
            Assert.Equal("MyFunc", udf.Ident.Name.Value);
            Assert.Equal(2, udf.Attributes.Count);

            Assert.Equal("Ann1", udf.Attributes[0].Name.Name.Value);
            Assert.Equal("x", udf.Attributes[0].Arguments[0]);

            Assert.Equal("Ann2", udf.Attributes[1].Name.Name.Value);
            Assert.Equal(2, udf.Attributes[1].Arguments.Count);
            Assert.Equal("y", udf.Attributes[1].Arguments[0]);
            Assert.Equal("z", udf.Attributes[1].Arguments[1]);
        }

        [Fact]
        public void TestAnnotationNoArgs()
        {
            var result = UserDefinitions.Parse(
            @"
                [NoArgs]
                Foo = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var formula = result.NamedFormulas.First();
            Assert.Single(formula.Attributes);
            Assert.Equal("NoArgs", formula.Attributes[0].Name.Name.Value);
            Assert.Empty(formula.Attributes[0].Arguments);
        }

        [Fact]
        public void TestAnnotationEmptyParens()
        {
            var result = UserDefinitions.Parse(
            @"
                [NoArgs()]
                Foo = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var formula = result.NamedFormulas.First();
            Assert.Single(formula.Attributes);
            Assert.Equal("NoArgs", formula.Attributes[0].Name.Name.Value);
            Assert.Empty(formula.Attributes[0].Arguments);
        }

        [Fact]
        public void TestAnnotationFlowsToUserDefinedFunction()
        {
            // Verify attributes are preserved on parsed UDF and survive through CreateFunctions
            var parseResult = UserDefinitions.Parse(
                @"[MyAnnotation(""TestValue"")] MyFunc(): Number = 1;",
                _parseOptions);

            Assert.False(parseResult.HasErrors);

            var parsedUdf = parseResult.UDFs.First();
            Assert.True(parsedUdf.IsParseValid);
            Assert.Single(parsedUdf.Attributes);
            Assert.Equal("MyAnnotation", parsedUdf.Attributes[0].Name.Name.Value);
            Assert.Equal("TestValue", parsedUdf.Attributes[0].Arguments[0]);

            // Verify attributes survive through CreateFunctions using a name resolver with built-in types
            var primitiveTypes = new SymbolTable();
            primitiveTypes.AddType(new DName("Number"), FormulaType.Number);

            var udfs = UserDefinedFunction.CreateFunctions(
                parseResult.UDFs.Where(u => u.IsParseValid),
                primitiveTypes,
                out var errors);

            Assert.Empty(errors);

            var func = udfs.First();
            Assert.Single(func.Attributes);
            Assert.Equal("MyAnnotation", func.Attributes[0].Name.Name.Value);
            Assert.Equal("TestValue", func.Attributes[0].Arguments[0]);
        }

        [Fact]
        public void TestUDFWithNoAnnotations()
        {
            var result = UserDefinitions.Parse(
            @"
                MyFunc(): Number = 1;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var udf = result.UDFs.First();
            Assert.Empty(udf.Attributes);
        }

        [Fact]
        public void TestMultipleAnnotationsOnMultipleUDFs()
        {
            var result = UserDefinitions.Parse(
            @"
                [A(""1"")]
                Func1(): Number = 1;
                [B(""2"")] [C(""3"")]
                Func2(): Number = 2;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var udfs = result.UDFs.ToList();
            Assert.Equal(2, udfs.Count);

            Assert.Single(udfs[0].Attributes);
            Assert.Equal("A", udfs[0].Attributes[0].Name.Name.Value);

            Assert.Equal(2, udfs[1].Attributes.Count);
            Assert.Equal("B", udfs[1].Attributes[0].Name.Name.Value);
            Assert.Equal("C", udfs[1].Attributes[1].Name.Name.Value);
        }

        [Fact]
        public void TestUnknownAttributeOnNFProducesError()
        {
            var checkResult = GetCheckResult("[UnknownAttr] X = 1;");
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.False(checkResult.IsSuccess);
            Assert.Contains(checkResult.Errors, e => e.Message.Contains("UnknownAttr"));
        }

        [Fact]
        public void TestUnknownAttributeOnUDFProducesError()
        {
            var checkResult = GetCheckResult("[UnknownAttr] MyFunc(): Number = 1;");
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.False(checkResult.IsSuccess);
            Assert.Contains(checkResult.Errors, e => e.Message.Contains("UnknownAttr"));
        }

        [Fact]
        public void TestRegisteredAttributeOnNFSucceeds()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new AttributeDefinition("MyAttr", 0, 0));

            var checkResult = GetCheckResult("[MyAttr] X = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void TestRegisteredAttributeOnUDFSucceeds()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new AttributeDefinition("MyAttr", 0, 0));

            var checkResult = GetCheckResult("[MyAttr] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void TestAttributeArgCountTooFew()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new AttributeDefinition("Foo", 1, 2));

            var checkResult = GetCheckResult("[Foo] X = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.False(checkResult.IsSuccess);
            Assert.Contains(checkResult.Errors, e => e.Message.Contains("Foo"));
        }

        [Fact]
        public void TestAttributeArgCountTooMany()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new AttributeDefinition("Foo", 0, 1));

            var checkResult = GetCheckResult(@"[Foo(""a"", ""b"")] X = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.False(checkResult.IsSuccess);
            Assert.Contains(checkResult.Errors, e => e.Message.Contains("Foo"));
        }

        [Fact]
        public void TestBuiltInPartialNeedsNoRegistration()
        {
            // Partial is a built-in attribute; no host registration needed
            var checkResult = GetCheckResult(
                @"[Partial(""And"")] X = 1; [Partial(""And"")] X = 2;");
            checkResult.ApplyCreateUserDefinedFunctions();

            // Should have no unknown-attribute errors
            Assert.DoesNotContain(checkResult.Errors, e => e.Message.Contains("Unknown attribute"));
        }

        [Fact]
        public void TestComposedSymbolTableAttributeLookup()
        {
            var table1 = new SymbolTable();
            table1.AddAttribute(new AttributeDefinition("AttrA", 0, 0));

            var table2 = new SymbolTable();
            table2.AddAttribute(new AttributeDefinition("AttrB", 1, 1));

            var composed = ReadOnlySymbolTable.Compose(table1, table2);

            Assert.True(composed.TryGetAttributeDefinition("AttrA", out var defA));
            Assert.Equal("AttrA", defA.Name);

            Assert.True(composed.TryGetAttributeDefinition("AttrB", out var defB));
            Assert.Equal("AttrB", defB.Name);

            Assert.False(composed.TryGetAttributeDefinition("Unknown", out _));
        }

        [Fact]
        public void TestAllowAttributesFalseSkipsValidation()
        {
            // With AllowAttributes = false, attributes aren't parsed at all, so no validation
            var options = new ParserOptions() { AllowAttributes = false, AllowEqualOnlyNamedFormulas = true };
            var checkResult = new DefinitionsCheckResult();
            checkResult.SetText("X = 1;", options)
                .SetBindingInfo(ReadOnlySymbolTable.Compose(new SymbolTable()));
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void TestDuplicateAttributeRegistrationThrows()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new AttributeDefinition("MyAttr", 0, 0));

            Assert.Throws<InvalidOperationException>(() =>
                symbolTable.AddAttribute(new AttributeDefinition("MyAttr", 1, 1)));
        }

        [Fact]
        public void TestRegisteredAttributeWithCorrectArgs()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new AttributeDefinition("Tag", 1, 3));

            var checkResult = GetCheckResult(@"[Tag(""hello"", ""world"")] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }
    }
}
