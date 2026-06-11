// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
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
        public void TestAnnotationWithIdentArgs()
        {
            var result = UserDefinitions.Parse(
            @"
                [MyAttr(foo, bar)]
                Baz = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var attribute = result.NamedFormulas.First().Attributes[0];
            Assert.Equal("MyAttr", attribute.Name.Name.Value);
            Assert.Equal(2, attribute.Arguments.Count);
            Assert.Equal("foo", attribute.Arguments[0]);
            Assert.Equal("bar", attribute.Arguments[1]);
        }

        [Fact]
        public void TestAnnotationWithMixedArgs()
        {
            var result = UserDefinitions.Parse(
            @"
                [MyAttr(""hello"", world)]
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
        public void TestAnnotationWithIdentArgOnUDF()
        {
            var result = UserDefinitions.Parse(
            @"
                [MyAttr(someIdent)]
                MyFunc(): Number = 1;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var udf = result.UDFs.First();
            Assert.Equal("MyAttr", udf.Attributes[0].Name.Name.Value);
            Assert.Single(udf.Attributes[0].Arguments);
            Assert.Equal("someIdent", udf.Attributes[0].Arguments[0]);
        }

        [Fact]
        public void TestAnnotationDelimiterTokensCaptured()
        {
            var result = UserDefinitions.Parse(
            @"
                [MyAttr(someIdent)]
                MyFunc(): Number = 1;
            ", _parseOptions);

            Assert.False(result.HasErrors);

            var attribute = result.UDFs.First().Attributes[0];
            Assert.NotNull(attribute.OpenParen);
            Assert.Equal(TokKind.ParenOpen, attribute.OpenParen.Kind);
            Assert.NotNull(attribute.CloseParen);
            Assert.Equal(TokKind.ParenClose, attribute.CloseParen.Kind);
            Assert.NotNull(attribute.CloseBracket);
            Assert.Equal(TokKind.BracketClose, attribute.CloseBracket.Kind);
        }

        [Fact]
        public void TestAnnotationWithoutArgsHasNoParenTokens()
        {
            var result = UserDefinitions.Parse(
            @"
                [SomeName]
                Foo = 123;
            ", _parseOptions);

            Assert.False(result.HasErrors);
            var attribute = result.NamedFormulas.First().Attributes[0];
            Assert.Null(attribute.OpenParen);
            Assert.Null(attribute.CloseParen);
            Assert.NotNull(attribute.CloseBracket);
        }

        // While the maker is mid-typing an attribute (e.g. "[RecordLink(") no definition name
        // follows, so the parser previously discarded the attribute entirely. It is now surfaced
        // through ParseUserDefinitionResult.IncompleteAttributes so IntelliSense can react.
        [Fact]
        public void TestIncompleteAttribute_OpenArgList()
        {
            var result = UserDefinitions.Parse("[RecordLink(", _parseOptions);

            Assert.Empty(result.UDFs);
            Assert.Empty(result.NamedFormulas);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("RecordLink", attribute.Name.Name.Value);
            Assert.NotNull(attribute.OpenParen);
            Assert.Equal(TokKind.ParenOpen, attribute.OpenParen.Kind);
            Assert.Null(attribute.CloseParen);
            Assert.Null(attribute.CloseBracket);
        }

        [Fact]
        public void TestIncompleteAttribute_EmptyArgList()
        {
            var result = UserDefinitions.Parse("[RecordLink()]", _parseOptions);

            Assert.Empty(result.UDFs);
            Assert.Empty(result.NamedFormulas);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("RecordLink", attribute.Name.Name.Value);
            Assert.Empty(attribute.Arguments);
            Assert.NotNull(attribute.OpenParen);
            Assert.NotNull(attribute.CloseParen);
            Assert.NotNull(attribute.CloseBracket);
        }

        [Fact]
        public void TestIncompleteAttribute_NameOnly()
        {
            var result = UserDefinitions.Parse("[RecordLink", _parseOptions);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("RecordLink", attribute.Name.Name.Value);
            Assert.Null(attribute.OpenParen);
            Assert.Null(attribute.CloseParen);
            Assert.Null(attribute.CloseBracket);
        }

        [Theory]
        [InlineData("[RecordLink()] OpenAccount")]
        [InlineData("[RecordLink()] OpenAccount()")]
        [InlineData("[RecordLink()] Foo =")]
        [InlineData("[RecordLink()] Foo :=")]
        [InlineData("[RecordLink()] Foo;")]
        public void TestIncompleteAttribute_IncompleteDefinitionShape(string script)
        {
            var result = UserDefinitions.Parse(script, _parseOptions);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("RecordLink", attribute.Name.Name.Value);
            Assert.NotNull(attribute.OpenParen);
            Assert.NotNull(attribute.CloseParen);
            Assert.NotNull(attribute.CloseBracket);
        }

        [Fact]
        public void TestIncompleteAttribute_MultipleAttributes()
        {
            var result = UserDefinitions.Parse("[A][RecordLink()] OpenAccount", _parseOptions);

            var attributes = result.IncompleteAttributes.ToList();
            Assert.Equal(2, attributes.Count);
            Assert.Equal("A", attributes[0].Name.Name.Value);
            Assert.Equal("RecordLink", attributes[1].Name.Name.Value);
        }

        [Fact]
        public void TestIncompleteAttribute_ResumesAtNextDefinition()
        {
            var parseOptions = new ParserOptions() { AllowAttributes = true, AllowEqualOnlyNamedFormulas = true, AllowsSideEffects = true };
            var result = UserDefinitions.Parse(
            @"
                Foo = 123;

                [Record
                Bar(): Void = { Notify(""Abc"") };

                Baz = ""Valid?"";
            ", parseOptions);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("Record", attribute.Name.Name.Value);
            Assert.Null(attribute.CloseBracket);

            var formulas = result.NamedFormulas.ToList();
            Assert.Equal(2, formulas.Count);
            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("Baz", formulas[1].Ident.Name.Value);

            var udf = Assert.Single(result.UDFs);
            Assert.Equal("Bar", udf.Ident.Name.Value);
            Assert.Empty(udf.Attributes);
        }

        [Fact]
        public void TestIncompleteAttribute_ResumesBeforeDefinitionWithBracketedBody()
        {
            var parseOptions = new ParserOptions() { AllowAttributes = true, AllowEqualOnlyNamedFormulas = true, AllowsSideEffects = true };
            var result = UserDefinitions.Parse(
            @"
                Foo = 123;

                [Record
                Bar(): Void = { Notify(""Abc""); CountRows([1, 2]) };

                Baz = ""Valid?"";
            ", parseOptions);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("Record", attribute.Name.Name.Value);
            Assert.Null(attribute.CloseBracket);

            var formulas = result.NamedFormulas.ToList();
            Assert.Equal(2, formulas.Count);
            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("Baz", formulas[1].Ident.Name.Value);

            var udf = Assert.Single(result.UDFs);
            Assert.Equal("Bar", udf.Ident.Name.Value);
            Assert.Empty(udf.Attributes);
        }

        [Fact]
        public void TestIncompleteAttribute_ResumesAtNextNamedFormula()
        {
            var result = UserDefinitions.Parse(
            @"
                Foo = 123;

                [Record
                Baz = ""Valid?"";
            ", _parseOptions);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("Record", attribute.Name.Name.Value);
            Assert.Null(attribute.CloseBracket);

            var formulas = result.NamedFormulas.ToList();
            Assert.Equal(2, formulas.Count);
            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("Baz", formulas[1].Ident.Name.Value);
            Assert.Empty(formulas[1].Attributes);
        }

        [Fact]
        public void TestIncompleteAttribute_MissingCloseBracketAfterClosedArgsResumesAtNextDefinition()
        {
            var parseOptions = new ParserOptions() { AllowAttributes = true, AllowEqualOnlyNamedFormulas = true, AllowsSideEffects = true };
            var result = UserDefinitions.Parse(
            @"
                Foo = 123;

                [RecordLink()
                Bar(): Void = { Notify(""Abc"") };

                Baz = ""Valid?"";
            ", parseOptions);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("RecordLink", attribute.Name.Name.Value);
            Assert.NotNull(attribute.OpenParen);
            Assert.NotNull(attribute.CloseParen);
            Assert.Null(attribute.CloseBracket);

            var formulas = result.NamedFormulas.ToList();
            Assert.Equal(2, formulas.Count);
            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("Baz", formulas[1].Ident.Name.Value);

            var udf = Assert.Single(result.UDFs);
            Assert.Equal("Bar", udf.Ident.Name.Value);
            Assert.Empty(udf.Attributes);
        }

        [Theory]
        [InlineData("[RecordLink(foo")]
        [InlineData("[RecordLink(foo,")]
        public void TestIncompleteAttribute_PartialArgsResumeAtNextDefinition(string incompleteAttribute)
        {
            var parseOptions = new ParserOptions() { AllowAttributes = true, AllowEqualOnlyNamedFormulas = true, AllowsSideEffects = true };
            var result = UserDefinitions.Parse(
            $@"
                Foo = 123;

                {incompleteAttribute}
                Bar(): Void = {{ Notify(""Abc"") }};

                Baz = ""Valid?"";
            ", parseOptions);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("RecordLink", attribute.Name.Name.Value);
            var argument = Assert.Single(attribute.Arguments);
            Assert.Equal("foo", argument);
            Assert.NotNull(attribute.OpenParen);
            Assert.Null(attribute.CloseParen);
            Assert.Null(attribute.CloseBracket);

            var formulas = result.NamedFormulas.ToList();
            Assert.Equal(2, formulas.Count);
            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("Baz", formulas[1].Ident.Name.Value);

            var udf = Assert.Single(result.UDFs);
            Assert.Equal("Bar", udf.Ident.Name.Value);
            Assert.Empty(udf.Attributes);
        }

        [Fact]
        public void TestIncompleteAttribute_LaterIncompleteAttributeDoesNotAttachEarlierAttributeToNextDefinition()
        {
            var parseOptions = new ParserOptions() { AllowAttributes = true, AllowEqualOnlyNamedFormulas = true, AllowsSideEffects = true };
            var result = UserDefinitions.Parse(
            @"
                Foo = 123;

                [A(""arg"")][Record
                Bar(): Void = { Notify(""Abc"") };

                Baz = ""Valid?"";
            ", parseOptions);

            var attributes = result.IncompleteAttributes.ToList();
            Assert.Equal(2, attributes.Count);
            Assert.Equal("A", attributes[0].Name.Name.Value);
            var firstAttributeArgument = Assert.Single(attributes[0].Arguments);
            Assert.Equal("arg", firstAttributeArgument);
            Assert.NotNull(attributes[0].CloseBracket);
            Assert.Equal("Record", attributes[1].Name.Name.Value);
            Assert.Null(attributes[1].CloseBracket);

            var formulas = result.NamedFormulas.ToList();
            Assert.Equal(2, formulas.Count);
            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("Baz", formulas[1].Ident.Name.Value);

            var udf = Assert.Single(result.UDFs);
            Assert.Equal("Bar", udf.Ident.Name.Value);
            Assert.Empty(udf.Attributes);
        }

        [Fact]
        public void TestIncompleteAttribute_ResumesBeforeDefinitionWithBracketedStringsAndCommentsInBody()
        {
            var parseOptions = new ParserOptions() { AllowAttributes = true, AllowEqualOnlyNamedFormulas = true, AllowsSideEffects = true };
            var result = UserDefinitions.Parse(
            @"
                Foo = 123;

                [Record
                Bar(): Void = { Notify(""[""); // ]
                    Notify(""]"") };

                Baz = ""Valid?"";
            ", parseOptions);

            var attribute = Assert.Single(result.IncompleteAttributes);
            Assert.Equal("Record", attribute.Name.Name.Value);
            Assert.Null(attribute.CloseBracket);

            var formulas = result.NamedFormulas.ToList();
            Assert.Equal(2, formulas.Count);
            Assert.Equal("Foo", formulas[0].Ident.Name.Value);
            Assert.Equal("Baz", formulas[1].Ident.Name.Value);

            var udf = Assert.Single(result.UDFs);
            Assert.Equal("Bar", udf.Ident.Name.Value);
            Assert.Empty(udf.Attributes);
        }

        [Fact]
        public void TestCompleteDefinitionHasNoIncompleteAttributes()
        {
            var result = UserDefinitions.Parse(
            @"
                [MyAttr(someIdent)]
                MyFunc(): Number = 1;
            ", _parseOptions);
            Assert.False(result.HasErrors);
            Assert.Empty(result.IncompleteAttributes);
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
            symbolTable.AddAttribute(new MockAttributeDefinition("MyAttr", 0, 0));

            var checkResult = GetCheckResult("[MyAttr] X = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void TestRegisteredAttributeOnUDFSucceeds()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition("MyAttr", 0, 0));

            var checkResult = GetCheckResult("[MyAttr] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void TestAttributeArgCountTooFew()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition("Foo", 1, 2));

            var checkResult = GetCheckResult("[Foo] X = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.False(checkResult.IsSuccess);
            Assert.Contains(checkResult.Errors, e => e.Message.Contains("Foo"));
        }

        [Fact]
        public void TestAttributeArgCountTooMany()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition("Foo", 0, 1));

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
            table1.AddAttribute(new MockAttributeDefinition("AttrA", 0, 0));

            var table2 = new SymbolTable();
            table2.AddAttribute(new MockAttributeDefinition("AttrB", 1, 1));

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
            symbolTable.AddAttribute(new MockAttributeDefinition("MyAttr", 0, 0));

            Assert.Throws<InvalidOperationException>(() =>
                symbolTable.AddAttribute(new MockAttributeDefinition("MyAttr", 1, 1)));
        }

        [Fact]
        public void TestRegisteredAttributeWithCorrectArgs()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition("Tag", 1, 3));

            var checkResult = GetCheckResult(@"[Tag(""hello"", ""world"")] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void TestCustomValidatorAcceptsMatchingSignature()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition(
                "RequiresNumber",
                0,
                0,
                validate: ctx =>
                {
                    if (ctx.ReturnType != FormulaType.Number)
                    {
                        return new[] { new TexlError(ctx.AttributeNameToken, DocumentErrorSeverity.Warning, TexlStrings.ErrGeneralError) };
                    }

                    return Array.Empty<TexlError>();
                }));

            var checkResult = GetCheckResult("[RequiresNumber] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
            Assert.DoesNotContain(checkResult.Errors, e => e.MessageKey == TexlStrings.ErrGeneralError.Key);
        }

        [Fact]
        public void TestCustomValidatorRejectsWrongReturnType()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddType(new DName("Text"), FormulaType.String);
            symbolTable.AddAttribute(new MockAttributeDefinition(
                "RequiresNumber",
                0,
                0,
                validate: ctx =>
                {
                    if (ctx.ReturnType != FormulaType.Number)
                    {
                        return new[] { new TexlError(ctx.AttributeNameToken, DocumentErrorSeverity.Warning, TexlStrings.ErrGeneralError) };
                    }

                    return Array.Empty<TexlError>();
                }));

            var checkResult = GetCheckResult(@"[RequiresNumber] MyFunc(): Text = ""hello"";", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            // Custom validation errors are warnings — IsSuccess stays true
            Assert.True(checkResult.IsSuccess);
            Assert.Contains(checkResult.Errors, e => e.MessageKey == TexlStrings.ErrGeneralError.Key && e.IsWarning);
        }

        [Fact]
        public void TestCustomValidatorChecksParameters()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition(
                "NeedsNumberParam",
                0,
                0,
                validate: ctx =>
                {
                    if (ctx.Parameters.Count != 1 || ctx.Parameters[0].Type != FormulaType.Number)
                    {
                        return new[] { new TexlError(ctx.AttributeNameToken, DocumentErrorSeverity.Warning, TexlStrings.ErrGeneralError) };
                    }

                    return Array.Empty<TexlError>();
                }));

            // Matching signature
            var checkResult = GetCheckResult("[NeedsNumberParam] MyFunc(x: Number): Number = x;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();
            Assert.DoesNotContain(checkResult.Errors, e => e.MessageKey == TexlStrings.ErrGeneralError.Key);

            // Non-matching: no parameters
            var checkResult2 = GetCheckResult("[NeedsNumberParam] MyFunc(): Number = 1;", symbolTable);
            checkResult2.ApplyCreateUserDefinedFunctions();
            Assert.Contains(checkResult2.Errors, e => e.MessageKey == TexlStrings.ErrGeneralError.Key && e.IsWarning);
        }

        [Fact]
        public void TestNullValidatorNoOp()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition("NoValidator", 0, 0));

            var checkResult = GetCheckResult("[NoValidator] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void TestValidatorReturnsEmpty()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition("AlwaysOk", 0, 0, validate: ctx => Array.Empty<TexlError>()));

            var checkResult = GetCheckResult("[AlwaysOk] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void TestMultipleValidatorErrorMessages()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition("Strict", 0, 0, validate: ctx => new[]
            {
                new TexlError(ctx.AttributeNameToken, DocumentErrorSeverity.Warning, TexlStrings.ErrBadToken),
                new TexlError(ctx.AttributeNameToken, DocumentErrorSeverity.Warning, TexlStrings.ErrOperandExpected)
            }));

            var checkResult = GetCheckResult("[Strict] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.Contains(checkResult.Errors, e => e.MessageKey == TexlStrings.ErrBadToken.Key && e.IsWarning);
            Assert.Contains(checkResult.Errors, e => e.MessageKey == TexlStrings.ErrOperandExpected.Key && e.IsWarning);
        }

        [Fact]
        public void TestCustomValidatorErrorsAreWarnings()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition("WarnMe", 0, 0, validate: ctx => new[] { new TexlError(ctx.AttributeNameToken, DocumentErrorSeverity.Warning, TexlStrings.ErrGeneralError) }));

            var checkResult = GetCheckResult("[WarnMe] MyFunc(): Number = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            // IsSuccess should still be true because custom validation errors are warnings
            Assert.True(checkResult.IsSuccess);
            var warning = Assert.Single(checkResult.Errors, e => e.MessageKey == TexlStrings.ErrGeneralError.Key);
            Assert.True(warning.IsWarning);
        }

        [Fact]
        public void TestCustomValidatorDoesNotRunOnNF()
        {
            var validatorCalled = false;
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition(
                "UdfOnly",
                0,
                0,
                validate: ctx =>
                {
                    validatorCalled = true;
                    return new[] { new TexlError(ctx.AttributeNameToken, DocumentErrorSeverity.Warning, TexlStrings.ErrGeneralError) };
                }));

            var checkResult = GetCheckResult("[UdfOnly] X = 1;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.False(validatorCalled);
            Assert.DoesNotContain(checkResult.Errors, e => e.MessageKey == TexlStrings.ErrGeneralError.Key);
        }

        [Fact]
        public void TestCustomValidatorReceivesCorrectContext()
        {
            AttributeValidationContext capturedContext = null;
            var symbolTable = new SymbolTable();
            symbolTable.AddAttribute(new MockAttributeDefinition(
                "Capture",
                1,
                2,
                validate: ctx =>
                {
                    capturedContext = ctx;
                    return Array.Empty<TexlError>();
                }));

            var checkResult = GetCheckResult(@"[Capture(""arg1"", ""arg2"")] MyFunc(x: Number): Number = x;", symbolTable);
            checkResult.ApplyCreateUserDefinedFunctions();

            Assert.NotNull(capturedContext);
            Assert.Equal("MyFunc", capturedContext.DefinitionName);
            Assert.NotNull(capturedContext.AttributeNameToken);
            Assert.Equal("Capture", capturedContext.AttributeNameToken.Name.Value);
            Assert.Equal(2, capturedContext.AttributeArguments.Count);
            Assert.Equal("arg1", capturedContext.AttributeArguments[0].As<StrLitToken>().Value);
            Assert.Equal("arg2", capturedContext.AttributeArguments[1].As<StrLitToken>().Value);
            Assert.Equal(FormulaType.Number, capturedContext.ReturnType);
            Assert.Single(capturedContext.Parameters);
            Assert.Equal("x", capturedContext.Parameters[0].Name.Value);
            Assert.Equal(FormulaType.Number, capturedContext.Parameters[0].Type);
        }
    }
}
