// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class UserDefinedTests
    {
        private static readonly ReadOnlySymbolTable _primitiveTypes = ReadOnlySymbolTable.PrimitiveTypesTableInstance;

        [Theory]
        [InlineData("x=1;y=2;z=x+y;", "Float(Abs(-(x+y+z)))", 6d)]
        [InlineData("x=1;y=2;Foo(x: Number): Number = Abs(x);", "Foo(-(y*y)+x)", 3d)]
        [InlineData("myvar=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Bar(1) + myvar", 2d)]
        public void NamedFormulaEntryTest(string script, string expression, double expected)
        {
            var engine = new RecalcEngine();

            engine.AddUserDefinitions(script);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = (NumberValue)check.GetEvaluator().Eval();
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); };")]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(abc, { bcd: 1 }) };")]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(abc, { bcd: 1 }) }; num = 3;")]
        public void ValidUDFBodyTest(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("F1(x:Number) : Number = { Set(a, x); x+1; };")]
        [InlineData("F1(x:Number) : Boolean = { Set(a, x); Today(); };")]
        public void ValidUDFBodyTest2(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b);")]
        public void InvalidUDFBodyTest(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.NotEmpty(errors);
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(,) ;}; num = 3;", 1, 0, 1)]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(,) ;};;;;;;;; num = 3;", 1, 0, 1)]
        public void InvalidUDFBodyTest2(string script, int udfCount, int validUdfCount, int nfCount)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.NotEmpty(errors);
            Assert.Equal(udfCount, parseResult.UDFs.Count());
            Assert.Equal(validUdfCount, udfs.Count());
            Assert.Equal(nfCount, parseResult.NamedFormulas.Count());
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); };")]
        public void ImperativeUdfsEnabledOrDisabled(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = false,
                Culture = CultureInfo.InvariantCulture
            };

            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.NotEmpty(errors);

            options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            parseResult = UserDefinitions.Parse(script, options);
            udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.Empty(errors);
        }

        [Fact]
        public void TestUdfsAndNfsAreMarkedAsAsync()
        {
            // Arrange
            var script = "test(): Text = Button1InScreen2.Text;Nf=Button1InScreen2.Text;Test2(): Void={Set(x, Button1InScreen2.Text);};";
            var parseResult = TexlParser.ParseUserDefinitionScript(script, new ParserOptions() { AllowsSideEffects = true });
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            var symbolTable = new MockSymbolTable();
            var dummyContol = new DummyExternalControl() { DisplayName = "Button1InScreen2" };
            symbolTable.AddControl("Button1InScreen2", dummyContol, TypeTree.Create(Enumerable.Empty<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));
            var config = new BindingConfig(markAsAsyncOnLazilyLoadedControlRef: true);

            // Act & Assert
            foreach (var udf in udfs)
            {
                udf.BindBody(symbolTable, new MockGlue(), config);
                Assert.True(udf.IsAsync);
            }

            foreach (var nf in parseResult.NamedFormulas)
            {
                var binding = TexlBinding.Run(new MockGlue(), nf.Formula.ParseTree, symbolTable, config, DType.EmptyRecord);
                Assert.True(binding.IsAsync(binding.Top));
            }
        }

        [Fact]
        public void TestFormulaUsingUdfsOrNamedFormulaReferringControlAsAsync()
        {
            // Arrange
            var script = "test(): Text = Button1InScreen2.Text;Nf=Button1InScreen2.Text;Test2(): Void={Set(x, Button1InScreen2.Text);};";
            var parseResult = TexlParser.ParseUserDefinitionScript(script, new ParserOptions() { AllowsSideEffects = true });
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            var symbolTable = new MockSymbolTable();
            var dummyContol = new DummyExternalControl() { DisplayName = "Button1InScreen2" };
            symbolTable.AddControl("Button1InScreen2", dummyContol, TypeTree.Create(Enumerable.Empty<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));
            var config = new BindingConfig(markAsAsyncOnLazilyLoadedControlRef: true);
            var symbolTable2 = new MockSymbolTable();
            var symbolTable3 = new SymbolTable();
            foreach (var udf in udfs)
            {
                udf.BindBody(symbolTable, new MockGlue(), config);
                Assert.True(udf.IsAsync);
                symbolTable3.AddFunction(udf);
            }

            foreach (var nf in parseResult.NamedFormulas)
            {
                var binding = TexlBinding.Run(new MockGlue(), nf.Formula.ParseTree, symbolTable, config, DType.EmptyRecord);
                Assert.True(binding.IsAsync(binding.Top));
                symbolTable2.Add(nf.Ident.Name, new NameLookupInfo(BindKind.PowerFxResolvedObject, binding.GetType(binding.Top), DPath.Root, 0, isAsync: binding.IsAsync(binding.Top)));
            }

            var combinedSymbolTable = ReadOnlySymbolTable.Compose(symbolTable2, symbolTable3);

            // Act
            var checkResult1 = new Engine().Check("test() & \"ddd\"", null, combinedSymbolTable);
            var checkResult2 = new Engine().Check("Nf & \"ddd\"", null, combinedSymbolTable);
            var checkResult3 = new Engine().Check("Test2();", new ParserOptions { AllowsSideEffects = true }, combinedSymbolTable);
            var checkResults = new List<CheckResult> { checkResult1, checkResult2, checkResult3 };

            // Assert
            foreach (var checkResult in checkResults)
            {
                Assert.True(checkResult.IsSuccess);
                Assert.True(checkResult.Binding.IsAsync(checkResult.Parse.Root));
            }
        }

        [Fact]
        public void TestUdfsAreNotMarkedAsAsynWhenReferringToNonLazilyLoadedControl()
        {
            // Arrange
            var script = "test(): Text = App.Text;";
            var parseResult = TexlParser.ParseUserDefinitionScript(script, new ParserOptions() { AllowsSideEffects = true });
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            var symbolTable = new MockSymbolTable();
            var dummyContol = new DummyExternalControl() { DisplayName = "App", IsAppInfoControl = true };
            symbolTable.AddControl("App", dummyContol, TypeTree.Create(Enumerable.Empty<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));
            var config = new BindingConfig(markAsAsyncOnLazilyLoadedControlRef: true);

            // Act & Assert
            foreach (var udf in udfs)
            {
                udf.BindBody(symbolTable, new MockGlue(), config);
                Assert.False(udf.IsAsync);
            }
        }

        [Theory]
        [InlineData("f():Number = First(ShowColumns([1,2,3], Value)).Value;", "f()", true, 1)] // SupportColumnNamesAsIdentifiers
        [InlineData("f():Number = First(ShowColumns([1,2,3], \"Value\")).Value;", "f()", false, 1)] // SupportColumnNamesAsIdentifiers
        [InlineData("f():Number = First([{a:1}]).a;", "f()", true, 1)] // TableSyntaxDoesntWrapRecords
        [InlineData("f():Number = First([{a:1}]).Value.a;", "f()", false, 1)] // TableSyntaxDoesntWrapRecords
        public void UDFBodyUsesFeatures(string script, string eval, bool powerFxV1, double expected)
        {
            // easy way

            var configWorks = new PowerFxConfig(powerFxV1 ? Features.PowerFxV1 : Features.None);
            var engineWorks = new RecalcEngine(configWorks);

            engineWorks.AddUserDefinitions(script);

            var checkEvalWorks = engineWorks.Check(eval);
            Assert.True(checkEvalWorks.IsSuccess);

            var resultWorks = (NumberValue)checkEvalWorks.GetEvaluator().Eval();
            Assert.Equal(expected, resultWorks.Value);

            // harder way, using DefinitionCheckResult, that passes through a different UDF body path than above

            var configDCR = new PowerFxConfig(powerFxV1 ? Features.PowerFxV1 : Features.None);
            var engineDCR = new RecalcEngine(configDCR);

            var addDCR = engineDCR.AddUserDefinedFunction(script);
            var errorsDCR = addDCR.ApplyErrors();

            Assert.Empty(errorsDCR);

            var checkEvalDCR = engineDCR.Check(eval);
            Assert.True(checkEvalDCR.IsSuccess);

            var resultDCR = (NumberValue)checkEvalDCR.GetEvaluator().Eval();
            Assert.Equal(expected, resultDCR.Value);

            // harder way, using DefinitionCheckResult, inverted Features setting

            var configDCRNot = new PowerFxConfig(powerFxV1 ? Features.None : Features.PowerFxV1);
            var engineDCRNot = new RecalcEngine(configDCRNot);

            var addDCRNot = engineDCRNot.AddUserDefinedFunction(script);
            var errorsDCRNot = addDCRNot.ApplyErrors();

            Assert.NotEmpty(errorsDCRNot);
        }
    }
}
