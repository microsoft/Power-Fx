// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
        [Theory]
        [InlineData("x=1;y=2;z=x+y;", "Float(Abs(-(x+y+z)))", 6d)]
        [InlineData("x=1;y=2;z=x+y;", "Abs(-(x+y+z))", 6d)]
        [InlineData("x=1;y=2;Foo(x: Number): Number = Abs(x);", "Foo(-(y*y)+x)", 3d)]
        [InlineData("myvar=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Bar(1) + myvar", 2d)]
        [InlineData("a=2e200;b=2e200;", "a+b", 4e200d)]
        [InlineData("Add(a: Number, b: Number): Number = a+b;", "Add(2e200,2e200)", 4e200d)]
        [InlineData("x=1;y=2;Foo(x: Float): Float = Abs(x);", "Foo(-(y*y)+x)", 3d)]
        [InlineData("myvar=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Float): Float = x + x;", "Bar(1) + myvar", 2d)]
        [InlineData("Add(a: Float, b: Float): Float = a+b;", "Add(2e200,2e200)", 4e200d)]
        [InlineData("Add(a: Decimal, b:Decimal): Float = a+b;", "Add(Decimal(\"1.0000000000000000000000001\"),Decimal(\"1.0000000000000000000000001\"))", 2d)]
        [InlineData("Add(a: Number, b:Decimal): Float = a+b;", "Add(1,Decimal(\"1.0000000000000000000000001\"))", 2d)]
        [InlineData("Add(a: Decimal, b:Decimal): Number = a+b;", "Add(Decimal(\"1.0000000000000000000000001\"),Decimal(\"1.0000000000000000000000001\"))", 2d)]
        [InlineData("Add(a: Number, b:Decimal): Number = a+b;", "Add(1,Decimal(\"1.0000000000000000000000001\"))", 2d)]
        public void NamedFormulaEntryTest(string script, string expression, double expected)
        {
            var parserOptions = new ParserOptions()
            {
                AllowEqualOnlyNamedFormulas = true,
                NumberIsFloat = true,
            };
            var engine = new RecalcEngine(numberIsFloat: true);

            engine.TryAddUserDefinitions(script, out var errors, parserOptions);
            Assert.Empty(errors);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            // ToString for rounding for comparison
            var result = (NumberValue)check.GetEvaluator().Eval();
            var resultString = result.Value.ToString("E6");
            var expectedString = expected.ToString("E6");
            Assert.Equal(expectedString, resultString);
        }

        [Theory]

        // no colon
        [InlineData("x=1;y=2;z=x+y;", "Decimal(Abs(-(x+y+z)))", "6")]
        [InlineData("x=1;y=2;z=x+y;", "Abs(-(x+y+z))", "6")]
        [InlineData("x=1;y=2;Foo(x: Number): Number = Abs(x);", "Decimal(Foo(-(y*y)+x))", "3")]
        [InlineData("x=1;y=2;Foo(x: Number): Number = Abs(x);", "Foo(-(y*y)+x)", "3")]
        [InlineData("x=1;y=2;Foo(x: Decimal): Decimal = Abs(x);", "Foo(-(y*y)+x)", "3")]
        [InlineData("myvar=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Decimal(Bar(1) + myvar)", "2")]
        [InlineData("a=2.0000000000000000000001;b=2.0000000000000000000001;", "a+b", "4.0000000000000000000002")]
        [InlineData("myvar=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Decimal): Decimal = x + x;", "Bar(1) + myvar", "2")]
        [InlineData("myvar=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Bar(1) + myvar", "2")]
        [InlineData("Add(a: Decimal, b:Decimal): Decimal = a+b;", "Add(1.0000000000000000000000001,1.0000000000000000000000001)", "2.0000000000000000000000002")]
        [InlineData("Add(a: Number, b:Number): Number = a+b;", "Add(1.0000000000000000000000001,1.0000000000000000000000001)", "2.0000000000000000000000002")]
        [InlineData("Add(a: Decimal, b:Float): Decimal = a+b;", "Add(1.0000000000000000000000001,1.0000000000000000000000001)", "2")]
        [InlineData("Add(a: Number, b:Float): Decimal = a+b;", "Add(1.0000000000000000000000001,1.0000000000000000000000001)", "2")]

        // colon
        [InlineData("x:=1;y:=2;z:=x+y;", "Decimal(Abs(-(x+y+z)))", "6")]
        [InlineData("x:=1;y:=2;z:=x+y;", "Abs(-(x+y+z))", "6")]
        [InlineData("x:=1;y:=2;Foo(x: Number): Number = Abs(x);", "Decimal(Foo(-(y*y)+x))", "3")]
        [InlineData("x:=1;y:=2;Foo(x: Number): Number = Abs(x);", "Foo(-(y*y)+x)", "3")]
        [InlineData("x:=1;y:=2;Foo(x: Decimal): Decimal = Abs(x);", "Foo(-(y*y)+x)", "3")]
        [InlineData("myvar:=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Decimal(Bar(1) + myvar)", "2")]
        [InlineData("a:=2.0000000000000000000001;b:=2.0000000000000000000001;", "a+b", "4.0000000000000000000002")]
        [InlineData("myvar:=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Decimal): Decimal = x + x;", "Bar(1) + myvar", "2")]
        [InlineData("myvar:=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Bar(1) + myvar", "2")]
        public void NamedFormulaEntryTestDecimal(string script, string expression, string expected)
        {
            var engine = new RecalcEngine();
            var parserOptions = new ParserOptions() { NumberIsFloat = false, AllowEqualOnlyNamedFormulas = true };

            engine.TryAddUserDefinitions(script, out var errors, parserOptions);
            Assert.Empty(errors);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = (DecimalValue)check.GetEvaluator().Eval();
            Assert.True(decimal.TryParse(expected, out var expectedDecimal));
            Assert.Equal(expectedDecimal, result.Value);
        }

        [Theory]
        [InlineData("x:=1;y:=2;z:=x+y;", "Decimal(Abs(-(x+y+z)))", "6")]
        [InlineData("x:=1;y:=2;z:=x+y;", "Abs(-(x+y+z))", "6")]
        [InlineData("x:=1;y:=2;Foo(x: Number): Number = Abs(x);", "Decimal(Foo(-(y*y)+x))", "3")]
        [InlineData("x:=1;y:=2;Foo(x: Number): Number = Abs(x);", "Foo(-(y*y)+x)", "3")]
        [InlineData("x:=1;y:=2;Foo(x: Decimal): Decimal = Abs(x);", "Foo(-(y*y)+x)", "3")]
        [InlineData("myvar:=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Decimal(Bar(1) + myvar)", "2")]
        [InlineData("a:=2.0000000000000000000001;b:=2.0000000000000000000001;", "a+b", "4.0000000000000000000002")]
        [InlineData("myvar:=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Decimal): Decimal = x + x;", "Bar(1) + myvar", "2")]
        [InlineData("myvar:=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Bar(1) + myvar", "2")]
        public void NamedFormulaEntryTestDecimal_ColonEqualRequired(string script, string expression, string expected)
        {
            var engine = new RecalcEngine();
            var parserOptions = new ParserOptions() { NumberIsFloat = false };

            engine.TryAddUserDefinitions(script, out var errors, parserOptions);
            Assert.Empty(errors);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = (DecimalValue)check.GetEvaluator().Eval();
            Assert.True(decimal.TryParse(expected, out var expectedDecimal));
            Assert.Equal(expectedDecimal, result.Value);
        }

        [Theory]
        [InlineData("x:=1;y:=2;z:=x+y;", "Float(Abs(-(x+y+z)))", 6d)]
        [InlineData("x:=1;y:=2;Foo(x: Number): Number = Abs(x);", "Foo(-(y*y)+x)", 3d)]
        [InlineData("myvar:=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Bar(1) + myvar", 2d)]
        public void NamedFormulaEntryTest_ColonEqualRequired(string script, string expression, double expected)
        {
            var engine = new RecalcEngine(numberIsFloat: true);

            engine.AddUserDefinitions(script);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = (NumberValue)check.GetEvaluator().Eval();
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("T:=Type({a:Number,b:Decimal,c:Float});x:={a:1.0000000000000000000000001};", "x.a", 1)]
        [InlineData("T:=Type({a:Number,b:Decimal,c:Float});x:={c:2.0000000000000000000000001};", "x.c", 2)]
        [InlineData("T:=Type({a:Number,b:Decimal,c:Float});x:={a:1e-100};", "x.a", 1e-100)]
        [InlineData("T:=Type({a:Number,b:Decimal,c:Float});x:={c:2e-100};", "x.c", 2e-100)]
        public void UDTRecordTests(string script, string expression, double expected)
        {
            var engine = new RecalcEngine(numberIsFloat: true);

            engine.TryAddUserDefinitions(script, out var errors, engine.GetDefaultParserOptionsCopy());
            Assert.Empty(errors);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            // ToString for rounding for comparison
            var result = (NumberValue)check.GetEvaluator().Eval();
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("T:=Type({a:Number,b:Decimal,c:Float});x:={a:1.0000000000000000000000001};", "x.a", "1.0000000000000000000000001")]
        [InlineData("T:=Type({a:Number,b:Decimal,c:Float});x:={b:2.0000000000000000000000001};", "x.b", "2.0000000000000000000000001")]
        [InlineData("T:=Type({a:Number,b:Decimal,c:Float});x:={a:1e-100};", "x.a", "0")]
        [InlineData("T:=Type({a:Number,b:Decimal,c:Float});x:={b:2e-100};", "x.b", "0")]
        public void UDTRecordTestsDecimal(string script, string expression, string expected)
        {
            var engine = new RecalcEngine();

            engine.TryAddUserDefinitions(script, out var errors, engine.GetDefaultParserOptionsCopy());
            Assert.Empty(errors);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            // ToString for rounding for comparison
            var result = (DecimalValue)check.GetEvaluator().Eval();
            Assert.Equal(decimal.Parse(expected), result.Value);
        }

        [Theory]

        // without colon
        [InlineData("a=Max(2,1;4,2;-5,4);;b=Average(2,1;4,2;-5,4);;", "a+b", "4.5")]
        [InlineData("a=2,0000000000000000000001;;b=2,0000000000000000000001;;", "a+b", "4.0000000000000000000002")]

        // with colon
        [InlineData("a:=Max(2,1;4,2;-5,4);;b:=Average(2,1;4,2;-5,4);;", "a+b", "4.5")]
        [InlineData("a:=2,0000000000000000000001;;b:=2,0000000000000000000001;;", "a+b", "4.0000000000000000000002")]
        public void NamedFormulaEntryTestCulture(string script, string expression, string expected)
        {
            var engine = new RecalcEngine();
            var parserOptions = new ParserOptions() { NumberIsFloat = false, Culture = new CultureInfo("fr-fr"), AllowEqualOnlyNamedFormulas = true };

            engine.TryAddUserDefinitions(script, out var errors, parserOptions);
            Assert.Empty(errors);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = (DecimalValue)check.GetEvaluator().Eval();
            Assert.True(decimal.TryParse(expected, out var expectedDecimal));
            Assert.Equal(expectedDecimal, result.Value);
        }

        [Theory]
        [InlineData("a:=Max(2,1;4,2;-5,4);;b:=Average(2,1;4,2;-5,4);;", "a+b", "4.5")]
        [InlineData("a:=2,0000000000000000000001;;b:=2,0000000000000000000001;;", "a+b", "4.0000000000000000000002")]
        public void NamedFormulaEntryTestCulture_ColonEqualRequired(string script, string expression, string expected)
        {
            var engine = new RecalcEngine();
            var parserOptions = new ParserOptions() { NumberIsFloat = false, Culture = new CultureInfo("fr-fr") };

            engine.TryAddUserDefinitions(script, out var errors, parserOptions);
            Assert.Empty(errors);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = (DecimalValue)check.GetEvaluator().Eval();
            Assert.True(decimal.TryParse(expected, out var expectedDecimal));
            Assert.Equal(expectedDecimal, result.Value);
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
                Culture = CultureInfo.InvariantCulture,
                AllowEqualOnlyNamedFormulas = true
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.False(errors.Any());
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); };")]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(abc, { bcd: 1 }) };")]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(abc, { bcd: 1 }) }; num := 3;")]
        public void ValidUDFBodyTestColonEqual(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
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
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
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
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
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
                Culture = CultureInfo.InvariantCulture,
                AllowEqualOnlyNamedFormulas = true
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.True(errors.Any());
            Assert.Equal(udfCount, parseResult.UDFs.Count());
            Assert.Equal(validUdfCount, udfs.Count());
            Assert.Equal(nfCount, parseResult.NamedFormulas.Count());
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(,) ;}; num := 3;", 1, 0, 1)]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(,) ;};;;;;;;; num := 3;", 1, 0, 1)]
        public void InvalidUDFBodyTest2ColonEqual(string script, int udfCount, int validUdfCount, int nfCount)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
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
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.NotEmpty(errors);

            options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            parseResult = UserDefinitions.Parse(script, options);
            udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.Empty(errors);
        }

        [Fact]
        public void TestUdfsAndNfsAreMarkedAsAsync()
        {
            // Arrange
            var script = "test(): Text = Button1InScreen2.Text;Nf=Button1InScreen2.Text;Test2(): Void={Set(x, Button1InScreen2.Text);};";
            var parseResult = TexlParser.ParseUserDefinitionScript(script, new ParserOptions() { AllowsSideEffects = true });
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
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
            var script = "test(): Text = Button1InScreen2.Text;Nf:=Button1InScreen2.Text;Test2(): Void={Set(x, Button1InScreen2.Text);};";
            var parseResult = TexlParser.ParseUserDefinitionScript(script, new ParserOptions() { AllowsSideEffects = true });
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
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
            var builtInTypes = SymbolTable.NewDefaultTypes(RecalcEngine._builtInNamedTypesDictionary, numberTypeIs: FormulaType.Number);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), builtInTypes, out var errors);
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
            var engineWorks = new RecalcEngine(configWorks, numberIsFloat: true);

            engineWorks.AddUserDefinitions(script);

            var checkEvalWorks = engineWorks.Check(eval);
            Assert.True(checkEvalWorks.IsSuccess);

            var resultWorks = (NumberValue)checkEvalWorks.GetEvaluator().Eval();
            Assert.Equal(expected, resultWorks.Value);

            // harder way, using DefinitionCheckResult, that passes through a different UDF body path than above

            var configDCR = new PowerFxConfig(powerFxV1 ? Features.PowerFxV1 : Features.None);
            var engineDCR = new RecalcEngine(configDCR, numberIsFloat: true);

            var addDCR = engineDCR.AddUserDefinedFunction(script);
            var errorsDCR = addDCR.ApplyErrors();

            Assert.Empty(errorsDCR);

            var checkEvalDCR = engineDCR.Check(eval);
            Assert.True(checkEvalDCR.IsSuccess);

            var resultDCR = (NumberValue)checkEvalDCR.GetEvaluator().Eval();
            Assert.Equal(expected, resultDCR.Value);

            // harder way, using DefinitionCheckResult, inverted Features setting

            var configDCRNot = new PowerFxConfig(powerFxV1 ? Features.None : Features.PowerFxV1);
            var engineDCRNot = new RecalcEngine(configDCRNot, numberIsFloat: true);

            var addDCRNot = engineDCRNot.AddUserDefinedFunction(script);
            var errorsDCRNot = addDCRNot.ApplyErrors();

            Assert.NotEmpty(errorsDCRNot);
        }

        [Theory]

        // no colon
        [InlineData("a = 3", true)]
        [InlineData("a = 3\n", true)]
        [InlineData("a = 3;", true)]
        [InlineData("a = 3;;", false)]
        [InlineData("a = 3,", false)]
        [InlineData("a = 3.", true)] // decimal seperator
        [InlineData("a = 3:", false)]
        [InlineData("a = 3  b = 4", false)]
        [InlineData("a = 3\nb = 4", false)]
        [InlineData("a = 3\nb = 4\n", false)]
        [InlineData("a = 3. b = 4", false)]
        [InlineData("a = 3; b = 4", true)]
        [InlineData("a = 3; b = 4;", true)]
        [InlineData("a = 3, b = 4", false)]
        [InlineData("a = 3;; b = 4", false)]
        [InlineData("a = 3;; b = 4;;", false)]
        [InlineData("a = 3;; b = 4;", false)]

        // colon
        [InlineData("a := 3", true)]
        [InlineData("a := 3\n", true)]
        [InlineData("a := 3;", true)]
        [InlineData("a := 3;;", false)]
        [InlineData("a := 3,", false)]
        [InlineData("a := 3.", true)] // decimal seperator
        [InlineData("a := 3:", false)]
        [InlineData("a := 3  b := 4", false)]
        [InlineData("a := 3\nb := 4", false)]
        [InlineData("a := 3\nb := 4\n", false)]
        [InlineData("a := 3. b := 4", false)]
        [InlineData("a := 3; b := 4", true)]
        [InlineData("a := 3; b := 4;", true)]
        [InlineData("a := 3, b := 4", false)]
        [InlineData("a := 3;; b := 4", false)]
        [InlineData("a := 3;; b := 4;;", false)]
        [InlineData("a := 3;; b := 4;", false)]

        public void BadOtherNFSeperatorsDot(string expression, bool isValid)
        {
            var engine = new RecalcEngine();
            var parserOptions = new ParserOptions() { NumberIsFloat = false, Culture = CultureInfo.InvariantCulture, AllowEqualOnlyNamedFormulas = true };

            engine.TryAddUserDefinitions(expression, out var errors, parserOptions);

            if (isValid)
            {
                Assert.Empty(errors);
            }
            else
            {
                Assert.NotEmpty(errors);
            }
        }

        [Theory]

        // no colon
        [InlineData("a = 3", true)]
        [InlineData("a = 3\n", true)]
        [InlineData("a = 3;", false)]
        [InlineData("a = 3;;", true)]
        [InlineData("a = 3,", true)] // equivalent of 3. in invariant (decimal seperator)
        [InlineData("a = 3.", false)] 
        [InlineData("a = 3:", false)]
        [InlineData("a = 3  b = 4", false)]
        [InlineData("a = 3\nb = 4", false)]
        [InlineData("a = 3\nb = 4\n", false)]
        [InlineData("a = 3. b = 4", false)]
        [InlineData("a = 3; b = 4", false)]
        [InlineData("a = 3; b = 4;", false)]
        [InlineData("a = 3, b = 4", false)]
        [InlineData("a = 3;; b = 4", true)]
        [InlineData("a = 3;; b = 4;;", true)]
        [InlineData("a = 3;; b = 4;", false)]

        // colon
        [InlineData("a := 3", true)]
        [InlineData("a := 3\n", true)]
        [InlineData("a := 3;", false)]
        [InlineData("a := 3;;", true)]
        [InlineData("a := 3,", true)] // equivalent of 3. in invariant (decimal seperator)
        [InlineData("a := 3.", false)]
        [InlineData("a := 3:", false)]
        [InlineData("a := 3  b := 4", false)]
        [InlineData("a := 3\nb := 4", false)]
        [InlineData("a := 3\nb := 4\n", false)]
        [InlineData("a := 3. b := 4", false)]
        [InlineData("a := 3; b := 4", false)]
        [InlineData("a := 3; b := 4;", false)]
        [InlineData("a := 3, b := 4", false)]
        [InlineData("a := 3;; b := 4", true)]
        [InlineData("a := 3;; b := 4;;", true)]
        [InlineData("a := 3;; b := 4;", false)]
        public void BadOtherNFSeperatorsComma(string expression, bool isValid)
        {
            var engine = new RecalcEngine();
            var parserOptions = new ParserOptions() { NumberIsFloat = false, Culture = new CultureInfo("fr-fr"), AllowEqualOnlyNamedFormulas = true };

            engine.TryAddUserDefinitions(expression, out var errors, parserOptions);

            if (isValid)
            {
                Assert.Empty(errors);
            }
            else
            {
                Assert.NotEmpty(errors);
            }
        }

        [Theory]

        // This calculation will overflow a Decimal. If the NumberIsFloat config is not passed down properly from Engine -> ParserOptions -> BndingConfig,
        // then there will be an overflow error as the * operator will treat the Date coercion to number as Decimal instead of the proper Float.
        [InlineData("F():Float = Date(9999,1,1) * Date(9999,1,1) * Date(9999,1,1) * Date(9999,1,1) * Date(9999,1,1);", "F()", false, true)]
        [InlineData("F():Float = Date(9999,1,1) * Date(9999,1,1) * Date(9999,1,1) * Date(9999,1,1) * Date(9999,1,1);", "F()", true, false)]
        public void NumberIsFloatPassedToConfig(string script, string expression, bool numberIsFloat, bool expectOverflowError)
        {
            var engine = new RecalcEngine(numberIsFloat: numberIsFloat);

            // .SetText will pull NumberIsFloat from engine.ParserOptions, passing it on to BindingConfig
            engine.TryAddUserDefinitions(script, out var errors, engine.GetDefaultParserOptionsCopy());
            Assert.Empty(errors);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();
            Assert.True((expectOverflowError && result is ErrorValue errorValue) || (!expectOverflowError && result is NumberValue numberValue));
        }
    }
}
