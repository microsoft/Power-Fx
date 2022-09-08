// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{    
    public class TexlTests : PowerFxTest
    {
        private const string ChurnDataFunctionName = "ChurnData";

        [Theory]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + 5", "d")]
        [InlineData("Date(2000,1,1) + Time(2000,1,1)", "d")]
        [InlineData("Time(2000,1,1) + Date(2000,1,1)", "d")]
        [InlineData("Time(2000,1,1) + 5", "T")]
        [InlineData("5 + DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("5 + Time(2000,1,1)", "T")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - DateTimeValue(\"1 Jan 2015\")", "n")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Date(2000,1,1)", "n")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - 5", "d")]
        [InlineData("Date(2000,1,1) - DateTimeValue(\"1 Jan 2015\")", "n")]
        [InlineData("Date(2000,1,1) - Date(1999,1,1)", "n")]
        [InlineData("Time(2,1,1) - Time(2,1,1)", "n")]
        [InlineData("5 - DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("5 - Time(2000,1,1)", "T")]
        [InlineData("-Date(2001,1,1)", "D")]
        [InlineData("-Time(2,1,1)", "T")]
        [InlineData("-DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("Date(2000,1,1) + 5", "d")]
        [InlineData("5 + Date(2000,1,1)", "d")]
        [InlineData("5 - Date(2000,1,1)", "d")]
        [InlineData("Time(20,1,1) + Time(19,1,1)", "T")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Time(20,1,1)", "d")]
        [InlineData("Time(20,1,1) + DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Time(20,1,1)", "d")]
        [InlineData("DateValue(\"1 Jan 2015\") - Time(20,1,1)", "d")]
        public void TexlDateOverloads(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out var type), script);
            Assert.True(type.IsValid, script);

            TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
        }

        [Theory]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Date(2000,1,1)")]
        [InlineData("Date(2000,1,1) + Date(1999,1,1)")]
        [InlineData("Date(2000,1,1) + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(20,1,1) - DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(20,1,1) - Date(2000,1,1)")]
        public void TexlDateOverloads_Negative(string script)
        {
            // TestBindingErrors(script, DType.Error);
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(script);
            
            Assert.Equal(DType.Error, result._binding.ResultType);            
            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:d]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:d]")]
        [InlineData("DateAdd(Table({a:Date(2000,1,1)}),[3])", "*[a:d]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Result:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), 2)", "d")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), 2, TimeUnit!Years)", "d")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), \"hello\")", "d")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), \"hello\", 3)", "d")]
        public void TexlDateAdd(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out var type));
            Assert.True(type.IsValid);

            TestSimpleBindingSuccess(script, type);
        }

        [Theory]
        
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:d]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:d]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Result:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],1)", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],[3])", "*[Value:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"),[1])", "*[Result:d]")]
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Result:n]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Result:n]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Result:n]")]
        public void TexlDateTableFunctions(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result._binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:d]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:d]")]
        [InlineData("DateAdd(Table({a:Date(2000,1,1)}),[3])", "*[Value:d]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],1)", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],[3])", "*[Value:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"),[1])", "*[Value:d]")]
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Value:n]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Value:n]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Value:n]")]
        public void TexlDateTableFunctions_ConsistentOneColumnTableResult(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(Features.ConsistentOneColumnTableResult));
            var result = engine.Check(expression);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result._binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd(Table({v:Date(2022,1,1),s:9},{v:Date(2022,2,2),s:25}), 2, TimeUnit.Days)")]
        [InlineData("DateAdd(DropColumns([Date(2022,1,1),Date(2022,2,2)],\"Value\"), 2, TimeUnit.Days)")]
        [InlineData("DateDiff(Table({v:Date(2022,1,1),s:9},{v:Date(2022,2,2),s:25}), Date(2022,12,12), TimeUnit.Days)")]
        [InlineData("DateDiff(DropColumns([Date(2022,1,1),Date(2022,2,2)],\"Value\"), Date(2022,2,2), TimeUnit.Days)")]
        public void TexlDateTableFunctions_Negative(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("Average(\"3\")")]
        [InlineData("Average(\"3\", 4)")]
        [InlineData("Average(true, 4)")]
        [InlineData("Average(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsAverageWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsAverageTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);
            TestSimpleBindingSuccess("Average(1, 2, A, B, C)", DType.Number, symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsChar()
        {
            TestSimpleBindingSuccess("Char(65)", DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Value:n]")));
            TestSimpleBindingSuccess("Char(T)", TestUtils.DT("*[Result:s]"), symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsConcatenate()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("myString", FormulaType.String);
            TestSimpleBindingSuccess(
                "Concatenate(\"abcdef\", myString)",
                DType.String,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "Concatenate(Table!B, \" ending\")",
                TestUtils.DT("*[Result:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Concatenate(\" Begining\", myString, \" simple\", \"\", \" ending\")",
                DType.String,
                symbol);

            symbol.RemoveVariable("Table");
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:s]")));
            TestSimpleBindingSuccess(
                "Concatenate(Table!B, \" ending\", Table!D)",
                TestUtils.DT("*[Result:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCount()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Count(Table)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "Count(Table!A)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCountA()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "CountA(Table)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:s]")));
            TestSimpleBindingSuccess(
                "CountA(Table2)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:s, B:n, C:b]")));
            TestSimpleBindingSuccess(
                "CountA(Table3!C)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCountIf()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));

            TestSimpleBindingSuccess(
                "CountIf(Table, A < 10)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "CountIf(Table, A < 10, A > 0, A <> 2)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCountRows()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:b, C:s]")));

            TestSimpleBindingSuccess(
                "CountRows(Table)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:b, C:s, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "CountRows(Table2)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "CountRows(First(Table2)!D)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFilter()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Filter(Table, A < 10)",
                TestUtils.DT("*[A:n]"),
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b]")));
            TestSimpleBindingSuccess(
                "Filter(Table2, A < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b]"),
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));
            TestSimpleBindingSuccess(
                "Filter(Table3, D!X < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]"),
                symbol);

            symbol.AddVariable("Table4", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "Filter(Table4, CountRows(D!X) < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]"),
                symbol);

            symbol.AddVariable("Table5", new TableType(TestUtils.DT("*[A:g]")));
            TestSimpleBindingSuccess(
                "Filter(Table5, A = GUID(\"43cb2147-c701-4981-b8ed-f0dd56e3fdde\"))",
                TestUtils.DT("*[A:g]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFilter_Negative()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:g]")));
            TestBindingErrors(
                "Filter(Table, A = \"43cb2147-c701-4981-b8ed-f0dd56e3fdde\")",
                TestUtils.DT("*[A:g]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFirst()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "First(Table)",
                TestUtils.DT("![A:n]"),
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:*[C:n]]")));
            TestSimpleBindingSuccess(
                "First(Table2)",
                TestUtils.DT("![A:n, B:*[C:n]]"),
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:n, B:*[C:*[D:*[E:![F:s, G:n]]]]]")));
            TestSimpleBindingSuccess(
                "First(Table3)",
                TestUtils.DT("![A:n, B:*[C:*[D:*[E:![F:s, G:n]]]]]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFirstN()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));

            TestSimpleBindingSuccess(
                "FirstN(Table)",
                TestUtils.DT("*[A:n]"),
                symbol);
            TestSimpleBindingSuccess(
                "FirstN(Table, 1234)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIf()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("B", new TableType(TestUtils.DT("*[X:n, Y:s, Z:b]")));
            symbol.AddVariable("C", new TableType(TestUtils.DT("*[Y:s, XX:n, Z:b]")));

            TestSimpleBindingSuccess(
                "If(A < 10, 1, 2)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, B, C)",
                TestUtils.DT("*[Y:s, Z:b]"),
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, 1, A < 20, 2, A < 30, 3)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, 1, A < 20, 2, A < 30, 3, 4)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, [[1,2,3],[3,2,1]], [[1,3,2],[3,2,3],[1,1,3]])",
                TestUtils.DT("*[Value:*[Value:n]]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIfWithArgumentCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            
            TestSimpleBindingSuccess(
                "If(A < 10, 1, \"2\")",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 1, \"one\", A < 2, 2, A < 3, true, false)",
                DType.String,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 1, true, A < 2, 2, A < 3, false, \"true\")",
                DType.Boolean,
                symbol);

            // Negative cases -- when args cannot be coerced.

            TestBindingErrors(
                "If(A < 10, 1, [1,2,3])",
                DType.Number,
                symbol);

            TestBindingErrors(
                "If(A < 10, 1, {Value: 2})",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsBlank()
        {
            TestSimpleBindingSuccess("IsBlank(\"foo\")", DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsBlank(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsBlank(Table)",
                DType.Boolean,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsBlankOrError()
        {
            TestSimpleBindingSuccess(
                "IsBlankOrError(\"foo\")",
                DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsBlankOrError(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsBlankOrError(Table)",
                DType.Boolean,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsNumeric()
        {
            TestSimpleBindingSuccess(
                "IsNumeric(\"12\")",
                DType.Boolean);

            TestSimpleBindingSuccess(
                "IsNumeric(12)",
                DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsNumeric(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsNumeric(Table)",
                DType.Boolean,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLast()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Last(Table)",
                TestUtils.DT("![A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLastN()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "LastN(Table)",
                TestUtils.DT("*[A:n]"),
                symbol);
            TestSimpleBindingSuccess(
                "LastN(Table, 1234)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLeft()
        {
            TestSimpleBindingSuccess(
                "Left(\"foo\", 3)",
                DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Count:n]")));

            TestSimpleBindingSuccess(
                "Left(T, 3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Left(T, T2)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Left(\"foo\", T2)",
                TestUtils.DT("*[Result:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLen()
        {
            TestSimpleBindingSuccess(
                "Len(\"foo\")",
                DType.Number);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "Len(T)",
                TestUtils.DT("*[Result:n]"),
                symbol);
        }

        [Theory]
        [InlineData("LookUp(T, A = 3, B)", "s", "*[A:n, B:s]")]
        [InlineData("LookUp(T, (A + 2) / C = 3, B & D)", "s", "*[A:n, B:s, C:n, D:s]")]
        [InlineData("LookUp(T, A = 3)", "![A:n, B:s]", "*[A:n, B:s]")]
        [InlineData("LookUp(T, (A + 2) / C = 3)", "![A:n, B:s, C:n, D:s]", "*[A:n, B:s, C:n, D:s]")]
        public void TexlFunctionTypeSemanticsLookUp(string script, string returnTypeString, string nameResolverTypeString)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT(nameResolverTypeString)));
            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(returnTypeString),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLower()
        {
            TestSimpleBindingSuccess(
                "Lower(\"FOO\")",
                DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "Lower(T)",
                TestUtils.DT("*[Name:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsMid()
        {
            TestSimpleBindingSuccess(
                "Mid(\"hello\", 2, 3)",
                DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Start:n]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Count:n]")));

            TestSimpleBindingSuccess(
                "Mid(T, 2, 3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Mid(T, T2, 3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Mid(T, 2, T3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Mid(T, T2, T3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Mid(\"hello\", T2, T3)",
                TestUtils.DT("*[Result:s]"),
                symbol);
        }

        [Theory]
        [InlineData("Max(1, 2, 3, 4)")]
        [InlineData("Max(1, A, 2, A)")]
        [InlineData("Max(Table, A)")]
        [InlineData("Min(1, 2, 3, 4)")]
        [InlineData("Min(1, A, 2, A)")]
        [InlineData("Min(Table, A)")]

        public void TexlFunctionTypeSemanticsMinMax(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("Max(\"3\")")]
        [InlineData("Max(\"3\", 4)")]
        [InlineData("Max(true, 4)")]
        [InlineData("Max(true, \"5\", 6)")]
        [InlineData("Min(\"3\")")]
        [InlineData("Min(\"3\", 4)")]
        [InlineData("Min(true, 4)")]
        [InlineData("Min(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsMinMaxWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Theory]
        [InlineData("Min(1, 2, A, B, C)")]
        [InlineData("Max(1, 2, A, B, C)")]
        public void TexlFunctionTypeSemanticsMinMaxTypedGlobalWithCoercion(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsNot()
        {
            TestSimpleBindingSuccess(
                "Not(true)",
                DType.Boolean);

            TestSimpleBindingSuccess(
                "Not(1)",
                DType.Boolean);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsNow()
        {
            TestSimpleBindingSuccess(
                "Now()",
                DType.DateTime);
        }

        [Theory]
        [InlineData("Or(true)")]
        [InlineData("Or(true, false, true)")]
        [InlineData("Or(1, 0)")]
        [InlineData("Or(1, And(0, 2))")]
        public void TexlFunctionTypeSemanticsOr(string script)
        {
            TestSimpleBindingSuccess(
                script,
                DType.Boolean);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRand()
        {
            TestSimpleBindingSuccess(
                "Rand()",
                DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRandBetween()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("B", FormulaType.Number);

            TestSimpleBindingSuccess(
                    "RandBetween(A, B)",
                    DType.Number,
                    symbol);
        }

        [Theory]
        [InlineData("Replace(\"hello\", 2, 3, \"X\")", "s")]
        [InlineData("Replace(T, 2, 3, \"X\")", "*[Name:s]")]
        [InlineData("Replace(T, T2, 3, \"X\")", "*[Name:s]")]
        [InlineData("Replace(T, 2, T3, \"X\")", "*[Name:s]")]
        [InlineData("Replace(T, T2, T3, \"X\")", "*[Name:s]")]
        [InlineData("Replace(T, T2, T3, TX)", "*[Name:s]")]
        [InlineData("Replace(\"hello\", T2, T3, TX)", "*[Result:s]")]
        public void TexlFunctionTypeSemanticsReplace(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Start:n]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Count:n]")));
            symbol.AddVariable("TX", new TableType(TestUtils.DT("*[Replacement:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsInt()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[A:n]")));

            TestSimpleBindingSuccess(
                "Int(1234)",
                DType.Number);

            TestSimpleBindingSuccess(
                "Int(T)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Theory]
        [InlineData("Int(\"3\")", "n", null)]
        [InlineData("Int(true)", "n", null)]
        [InlineData("Int(T)", "*[Booleans:n]", "*[Booleans:b]")]
        [InlineData("Int(T)", "*[Strings:n]", "*[Strings:s]")]
        [InlineData("Int([true, false, true])", "*[Value:n]", null)]
        [InlineData("Int([\"5\", \"6\"])", "*[Value:n]", null)]
        public void TexlFunctionTypeSemanticsIntWithCoercion(string script, string expectedType, string typedGlobal)
        {
            var symbol = new SymbolTable();
            
            if (typedGlobal != null)
            {
                symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));
                TestSimpleBindingSuccess(
                    script,
                    TestUtils.DT(expectedType),
                    symbol);
            }
            else
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
            }
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTruncOneParam()
        {
            TestSimpleBindingSuccess(
                "Trunc(1234)",
                DType.Number);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Trunc(T)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTruncTwoParams()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                "Trunc(1234.567, 4)",
                DType.Number);

            TestSimpleBindingSuccess(
                "Trunc(1234.567, T)",
                TestUtils.DT("*[Result:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "Trunc(X, T)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "Trunc(X, 4)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRound()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                "Round(1234.567, 4)",
                DType.Number);

            TestSimpleBindingSuccess(
                "Round(1234.567, T)",
                TestUtils.DT("*[Result:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "Round(X, T)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "Round(X, 4)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRoundUp()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                "RoundUp(1234.567, 4)",
                DType.Number);

            TestSimpleBindingSuccess(
                "RoundUp(1234.567, T)",
                TestUtils.DT("*[Result:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "RoundUp(X, T)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "RoundUp(X, 4)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRoundDown()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                "RoundDown(1234.567, 4)",
                DType.Number);

            TestSimpleBindingSuccess(
                "RoundDown(1234.567, T)",
                TestUtils.DT("*[Result:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "RoundDown(X, T)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "RoundDown(X, 4)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);
        }

        [Theory]
        [InlineData("Sort(Table, A)", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table, A, \"Ascending\")", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table, A, \"Descending\")", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table, B)", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table, C)", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table!A, A)", "*[A:n]")]
        [InlineData("Sort(Table, B & \"hello\")", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table!B, Left(B, 2))", "*[B:s]")]
        [InlineData("Sort(Table2, D!X & \"hello\")", "*[A:n, B:s, C:b, D:![X:n]]")]
        [InlineData("Sort(Table2, D!X + 2)", "*[A:n, B:s, C:b, D:![X:n]]")]
        public void TexlFunctionTypeSemanticsSort(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Sqrt(1234.567)", "n")]
        [InlineData("Sqrt(T)", "*[A:n]")]
        [InlineData("Sqrt(T2!Value)", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsSqrt(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[A:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Name:s, Price:n, Value:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Sqrt(\"3\")", "n", null)]
        [InlineData("Sqrt(true)", "n", null)]
        [InlineData("Sqrt(T)", "*[Booleans:n]", "*[Booleans:b]")]
        [InlineData("Sqrt(T)", "*[Strings:n]", "*[Strings:s]")]
        [InlineData("Sqrt([true, false, true])", "*[Value:n]", null)]
        [InlineData("Sqrt([\"5\", \"6\"])", "*[Value:n]", null)]
        public void TexlFunctionTypeSemanticsSqrtWithCoercion(string script, string expectedType, string typedGlobal)
        {
            if (typedGlobal != null)
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));

                TestSimpleBindingSuccess(
                    script, 
                    TestUtils.DT(expectedType), 
                    symbol);
            }
            else
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
            }
        }

        [Theory]
        [InlineData("Sum(1, 2, 3, 4)")]
        [InlineData("Sum(1, A, 2, A)")]
        [InlineData("Sum(Table, A)")]
        [InlineData("Sum(Table2, D!X)")]
        [InlineData("Sum(Table, A + CountA(D!X))")]
        public void TexlFunctionTypeSemanticsSum(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("Sum(\"3\")")]
        [InlineData("Sum(\"3\", 4)")]
        [InlineData("Sum(true, 4)")]
        [InlineData("Sum(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsSumWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsSumTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);

            TestSimpleBindingSuccess(
                "Sum(1, 2, A, B, C)",
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("StdevP(1, 2, 3, 4)")]
        [InlineData("StdevP(1, A, 2, A)")]
        [InlineData("StdevP(Table, A)")]
        [InlineData("StdevP(Table2, D!X)")]
        [InlineData("StdevP(Table, A + CountA(D!X))")]
        public void TexlFunctionTypeSemanticsStdevP(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("StdevP(\"3\")")]
        [InlineData("StdevP(\"3\", 4)")]
        [InlineData("StdevP(true, 4)")]
        [InlineData("StdevP(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsStdevPWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsStdevPTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);
            
            TestSimpleBindingSuccess(
                "StdevP(1, 2, A, B, C)",
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("VarP(1, 2, 3, 4)")]
        [InlineData("VarP(1, A, 2, A)")]
        [InlineData("VarP(Table, A)")]
        [InlineData("VarP(Table2, D!X)")]
        [InlineData("VarP(Table, A + CountA(D!X))")]
        public void TexlFunctionTypeSemanticsVarP(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("VarP(\"3\")")]
        [InlineData("VarP(\"3\", 4)")]
        [InlineData("VarP(true, 4)")]
        [InlineData("VarP(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsVarPWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsVarPTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);

            TestSimpleBindingSuccess(
                "VarP(1, 2, A, B, C)",
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("Text(123)")]
        [InlineData("Text(123, \"#.#\")")]
        [InlineData("Text(123, \"$000.000\")")]
        [InlineData("Text(123, \"dddd, mm/dd/yy, at hh:mm:ss am/pm\")")]
        [InlineData("Text(\"hello world\")")]
        [InlineData("Text(\"hello world\", \"mm/dd/yyyy\")")]
        public void TexlFunctionTypeSemanticsText(string script)
        {
            TestSimpleBindingSuccess(
                script,
                DType.String);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsText_Negative()
        {
            // Can't use both numeric formatting and date/time formatting within the same format string.
            TestBindingErrors(
                "Text(123, \"###.####    dddd, mm/dd/yy, at hh:mm:ss am/pm\")",
                DType.String,
                expectedErrorCount: 2);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsToday()
        {
            TestSimpleBindingSuccess("Today()", DType.Date);
        }

        [Theory]
        [InlineData("IsToday(Date(2000, 1, 1))")]
        [InlineData("IsToday(DateTimeValue(\"Today\"))")]
        [InlineData("IsToday(\"now\")")]
        [InlineData("IsToday(1)")]
        public void TexlFunctionTypeSemanticsIsToday(string script)
        {
            TestSimpleBindingSuccess(script, DType.Boolean);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsWeekNum()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("S", FormulaType.Date);
            symbol.AddVariable("R", FormulaType.Number);

            TestSimpleBindingSuccess(
                "WeekNum(S)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "WeekNum(S, R)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsISOWeekNum()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("S", FormulaType.Date);

            TestSimpleBindingSuccess(
                "ISOWeekNum(S)",
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("IsToday(true)")]
        [InlineData("IsToday(Time(1,2,3))")]
        public void TexlFunctionTypeSemanticsIsToday_Negative(string script)
        {
            TestBindingErrors(script, DType.Boolean);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTrim()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "Trim(\"hello\")",
                DType.String);

            TestSimpleBindingSuccess(
                "Trim(T)",
                TestUtils.DT("*[Name:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTrimEnds()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "TrimEnds(\" hello  \")",
                DType.String);

            TestSimpleBindingSuccess(
                "TrimEnds(T)",
                TestUtils.DT("*[Name:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsUpper()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "Upper(\"foo\")",
                DType.String);

            TestSimpleBindingSuccess(
                "Upper(T)",
                TestUtils.DT("*[Name:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsValue()
        {
            TestSimpleBindingSuccess(
                "Value(\"123.456\")",
                DType.Number);

            TestSimpleBindingSuccess(
                "Value(123.456)",
                DType.Number);
        }

        [Theory]
        [InlineData("a;")]
        [InlineData("a;b;c;d;e;")]
        [InlineData("a;a;a;a;")]
        [InlineData("If(A;B;,C;D;/*asdf*/,F;)")]
        [InlineData("ClearCollect(A,B);If(A, Set(A, B) /*asdf*/;);Set(A, !B);")]
        public void TexlTestParsingChainRuleEndingSemicolon(string inputText)
        {
            var result = TexlParser.ParseScript(inputText, flags: TexlParser.Flags.EnableExpressionChaining);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Root);
        }

        [Theory]
        [InlineData(";")]
        [InlineData("a;b;;")]
        public void TexlTestParsingChainRuleEndingSemicolon_negative(string inputText)
        {
            var result = TexlParser.ParseScript(inputText, flags: TexlParser.Flags.EnableExpressionChaining);
            Assert.True(result.HasError);
            Assert.NotNull(result.Root);
        }

        [Fact]
        public void TestTexlFunctionIsLambdaParamApi()
        {
            foreach (var func in BuiltinFunctionsCore.BuiltinFunctionsLibrary)
            {
                var maxArg = Math.Min(func.MaxArity, 1000);
                var foundLambdaParams = false;
                for (var i = 0; i < maxArg; i++)
                {
                    foundLambdaParams |= func.IsLambdaParam(i);
                }

                Assert.Equal(func.HasLambdas, foundLambdaParams);
            }
        }

        [Fact]
        public void TestTexlFunctionGetSignaturesApi()
        {
            foreach (var func in BuiltinFunctionsCore.BuiltinFunctionsLibrary)
            {
                IEnumerable<TexlStrings.StringGetter[]> signatures = func.GetSignatures();
                Assert.NotNull(signatures);
                Assert.True(func.MaxArity == 0 || signatures.Any(), "Failed on GetSignatures() for function " + func.Name);
            }
        }

        [Fact]
        public void TestWarningsOnEqualWithIncompatibleTypes()
        {
            TestBindingWarning("1 = \"hello\"", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("1 <> \"hello\"", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("true = 123", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("true <> 123", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("false = \"false\"", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("false <> \"false\"", DType.Boolean, expectedErrorCount: 1);
        }

        [Fact]
        public void TexlBindingLambdaAs()
        {
            TestSimpleBindingSuccess("Filter([1,2,3] As Input, Input.Value > 2)", TestUtils.DT("*[Value:n]"));
            TestSimpleBindingSuccess("Filter([1,2,3], ThisRecord.Value > 2)", TestUtils.DT("*[Value:n]"));
            TestSimpleBindingSuccess("Filter([1,2,3] As ThisRecord, ThisRecord.Value > 2)", TestUtils.DT("*[Value:n]"));
            TestSimpleBindingSuccess("Filter([[1,2,3],[2,3,4],[4,5,6]] As Outer, CountRows(Filter(Outer.Value As Inner, Inner.Value > 4)) > 1)", TestUtils.DT("*[Value:*[Value:n]]"));
            TestSimpleBindingSuccess("Filter(Filter([1,2,3] As Left, Left.Value > 1) As Right, Right.Value > 2)", TestUtils.DT("*[Value:n]"));
            TestSimpleBindingSuccess("ForAll(Filter([1,2,3] As Input, Input.Value > 2) As Outer, {Inner: Outer.Value})", TestUtils.DT("*[Inner:n]"));
            TestSimpleBindingSuccess("ForAll(Filter([1,2,3] As Input, Input.Value > 2), {Inner: ThisRecord.Value})", TestUtils.DT("*[Inner:n]"));

            // Required ident after rename
            TestBindingErrors("Filter([1,2,3] As Input, Value > 2)", TestUtils.DT("*[Value:n]"));

            // Required ident after rename
            TestBindingErrors("Filter([1,2,3] As Input, ThisRecord.Value > 2)", TestUtils.DT("*[Value:n]"));

            // As must be immediate child of call node
            TestBindingErrors("Filter([1,2,3] As First As Second, Value > 2)", TestUtils.DT("*[Value:n]"));

            // If As is used, field access must be qualified
            TestBindingErrors("ForAll(Filter([1,2,3] As Input, Input.Value > 2) As Outer, {Inner: Value})", TestUtils.DT("*[Inner:e]"));

            // If As is used, field access must be qualified
            TestBindingErrors("Filter([1,2,3] As ThisRecord, Value > 2)", TestUtils.DT("*[Value:n]"));

            // If As is used, field access must be qualified
            TestBindingErrors("Filter([1,2,3] As Renamed, IsBlank(Text(Renamed.Value)) = Value)", TestUtils.DT("*[Value:n]"));
        }

        [Theory]
        [InlineData("[]", "*[]")]
        [InlineData("[1, 2, 3]", "*[Value:n]")]
        [InlineData("[true, true, false, true]", "*[Value:b]")]
        [InlineData("[\"a\", \"b\", \"c\"]", "*[Value:s]")]
        [InlineData("[\"a\", 2, 3, true, false, 123, \"hello\"]", "*[Value:s]")]
        [InlineData("[1, 2, \"3\", \"1234.243\", 6564.254, Abs(-123.22)]", "*[Value:n]")]
        public void TexlBindingTables(string script, string expectedType)
        {
            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType));
        }

        [Fact]
        public void TexlBindingHeterogeneousTables()
        {
            TestSimpleBindingSuccess(
                "[{a:1}, {b:2}, {c:\"hello\"}, {a:3, c:\"goodbye\"}, {a:0, b:2}, {b:123, c:\"foo\"}]",
                TestUtils.DT("*[Value:![a:n, b:n, c:s]]"));

            TestSimpleBindingSuccess(
                "[" +
                "{ id: 0, nestedData: [{ nestedId: \"a\" }, { nestedId: \"b\" }, { nestedId: \"c\" }] }," +
                "{ id: 1, nestedData: [{ nestedId: \"d\" }, { nestedId: \"e\" }] }," +
                "{ id: 2, nestedData: [{ nestedId: \"f\" }, { nestedId: \"g\" }, { nestedId: \"h\" }] }," +
                "{ id: 3, nestedData: [] }," +
                "{ id: 4, nestedData: [{ nestedId: \"i\" }] }]",
                TestUtils.DT("*[Value:![id:n, nestedData:*[Value:![nestedId:s]]]]"));
        }

        [Fact]
        public void TexlBindingPurity()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Ratings", new TableType(TestUtils.DT("*[A:n]")));
            symbol.AddVariable("Movies", new TableType(TestUtils.DT("*[Ratings:n]")));

            // Basic literal and operator bindings
            TestBindingPurity("2", true);
            TestBindingPurity("\"String literal\"", true);
            TestBindingPurity("false", true);
            TestBindingPurity("false or true", true);
            TestBindingPurity("2 + 2 = 6", true);
            TestBindingPurity("\"str1\" & \"str2\"", true);
            TestBindingPurity("error cond - 2", true);
            TestBindingPurity("-2", true);

            // Introduce function calls that don't have any impurity
            TestBindingPurity("If(true, 2 + 2, 3 + 3)", true);
            TestBindingPurity("If(false, 18 * 9)", true);
            TestBindingPurity("If(If(true, true, false), 12, 5)", true);
            TestBindingPurity("If(false, -2, -3)", true);

            // Introduce impurity and test how it propagates through various types of nodes
            TestBindingPurity("Rand()", false);
            TestBindingPurity("Rand() + 3", false);
            TestBindingPurity("\"hi\" & Rand()", false);
            TestBindingPurity("-Rand()", false);
            TestBindingPurity("If(true, Rand(), 2)", false);
            TestBindingPurity("If(false, 2, If(false, 3, If(true, Rand())))", false);

            // Test using lambdas with impure expressions
            TestBindingPurity("Filter(Ratings, A < CountIf(Movies, Ratings > Rand()))", false, symbol);
            TestBindingPurity("CountIf(Filter(Movies, Ratings < Rand()), true)", false, symbol);
        }

        [Fact]
        public void TexlBindingDisambiguatedGlobals()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("foo", new TableType(TestUtils.DT("*[A:n, B:n, C:b]")));
            symbol.AddVariable("foo bar", new TableType(TestUtils.DT("*[A:n, B:n, C:b]")));

            TestSimpleBindingSuccess(
                "[@foo]",
                TestUtils.DT("*[A:n, B:n, C:b]"),
                symbol);
            TestSimpleBindingSuccess(
                "[@'foo bar']",
                TestUtils.DT("*[A:n, B:n, C:b]"),
                symbol);
        }

        [Fact]
        public void TestBindingErrorsOnDuplicateFieldNames()
        {
            TestBindingErrors("{A:1, A:2}", TestUtils.DT("![A:n]"));
            TestBindingErrors("{A:1, A:2, A:3}", TestUtils.DT("![A:n]"));
            TestBindingErrors("{A:1, B:true, A:2, B:3, A:3}", TestUtils.DT("![A:n, B:b]"));
            TestBindingErrors("{A:1, A:\"hello\"}", TestUtils.DT("![A:n]"));
            TestBindingErrors("{A:1, B:2, C:3, D:4, E:5, F:true, A:\"hello\"}", TestUtils.DT("![A:n, B:n, C:n, D:n, E:n, F:b]"));
        }

        [Theory]
        [InlineData("Find(\",\", \"LastName, FirstName\")", "n")]
        [InlineData("Find(\",\", \"LastName, FirstName\", 2)", "n")]
        [InlineData("Find(T, \"ll\")", "*[Result:n]")]
        [InlineData("Find(T, \"ll\", 2)", "*[Result:n]")]
        [InlineData("Find(T, \"ll\", T1)", "*[Result:n]")]
        [InlineData("Find(T, T2)", "*[Result:n]")]
        [InlineData("Find(T, T2, 2)", "*[Result:n]")]
        [InlineData("Find(T, T2, T1)", "*[Result:n]")]
        [InlineData("Find(\"ll\", T2)", "*[Result:n]")]
        [InlineData("Find(\"ll\", T2, 2)", "*[Result:n]")]
        [InlineData("Find(\"ll\", T2, T1)", "*[Result:n]")]
        [InlineData("Find(\"ll\", \"ll\", T1)", "*[Result:n]")]
        public void TexlFunctionTypeSemanticsFind(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[FindText:s]")));
            symbol.AddVariable("T1", new TableType(TestUtils.DT("*[StartIndex:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[WithinText:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("'test'", "'''test'''")]
        [InlineData("te st", "'te st'")]
        [InlineData("te'st", "'te''st'")]
        [InlineData("'te' st'", "'''te'' st'''")]
        [InlineData("te!st", "'te!st'")]
        [InlineData("te.st", "'te.st'")]
        [InlineData("_", "_")]
        [InlineData("_a", "_a")]
        [InlineData("12", "'12'")]
        [InlineData("1-2", "'1-2'")]
        [InlineData("@", "'@'")]
        public void TexlEscapeNameTest(string input, string expected)
        {
            Assert.Equal(expected, TexlLexer.EscapeName(input));
        }

        [Theory]
        [InlineData("DateValue(\"21 Jan 2014\")")]
        [InlineData("DateValue(\"25 agosto 2014\", \"it\")")]
        public void TexlFunctionTypeSemanticsDateValue(string script)
        {
            TestSimpleBindingSuccess(script, DType.Date);
        }

        [Theory]
        [InlineData("TimeValue(\"21 Jan 2014 9:50 AM\")")]
        [InlineData("TimeValue(\"25 agosto 2014 9:50 AM\", \"it\")")]
        public void TexlFunctionTypeSemanticsTimeValue(string script)
        {
            TestSimpleBindingSuccess(script, DType.Time);
        }

        [Theory]
        [InlineData("Table()", "*[]")]
        [InlineData("Table({})", "*[]")]
        [InlineData("Table({X:1})", "*[X:n]")]
        [InlineData("Table({X:\"hello\"})", "*[X:s]")]
        [InlineData("Table({X:true})", "*[X:b]")]
        [InlineData("Table({X:true}, {})", "*[X:b]")]
        [InlineData("Table({}, {X:true})", "*[X:b]")]
        [InlineData("Table({X:false}, {}, {X:true})", "*[X:b]")]
        [InlineData("Table({X:false}, {}, {X:true}, {})", "*[X:b]")]
        [InlineData("Table({X:false}, {}, {}, {}, {}, {X:true}, {})", "*[X:b]")]
        [InlineData("Table({X:false, Y:123}, {X:true})", "*[X:b, Y:n]")]
        [InlineData("Table({X:false, Y:123}, {X:true}, {})", "*[X:b, Y:n]")]
        [InlineData("Table({X:false, Y:123}, {Y:99.9}, {X:true}, {})", "*[X:b, Y:n]")]
        [InlineData("Table({X:false, Y:123}, {Y:7, X:true}, {Y:99.9}, {X:true}, {})", "*[X:b, Y:n]")]
        [InlineData("Table({Y:123}, {Y:7}, {Y:99.9}, {X:true})", "*[X:b, Y:n]")]
        [InlineData("Table({Y:123}, {Z:true}, {X:\"hello\"}, {W:{A:1, B:true, C:\"hello\"}})", "*[X:s, Y:n, Z:b, W:![A:n, B:b, C:s]]")]
        [InlineData("Table({Nested:Table({Nested:Table({X:1})})})", "*[Nested:*[Nested:*[X:n]]]")]
        public void TexlFunctionTypeSemanticsTable(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out DType type));
            Assert.True(type.IsValid);
            TestSimpleBindingSuccess(script, type);
        }

        [Theory]
        [InlineData("Table([])", "*[]")]
        [InlineData("Table(Table())", "*[]")]
        [InlineData("Table(Table(Table()))", "*[]")]
        [InlineData("Table(1, 2, 3)", "*[]")]
        [InlineData("Table(true, false)", "*[]")]
        [InlineData("Table(true, 2, \"hello\")", "*[]")]
        [InlineData("Table(\"hello\", \"world\")", "*[]")]
        [InlineData("Table({X:1}, [1, 2, 3])", "*[X:n]")]
        [InlineData("Table([true, false, true], {X:1}, {Y:2})", "*[X:n, Y:n]")]
        public void TexlFunctionTypeSemanticsTable_Negative(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out DType type));
            Assert.True(type.IsValid);
            TestBindingErrors(script, type);
        }

        [Theory]
        [InlineData("Concat([], \"\")", "s")]
        [InlineData("Concat([1, 2, 3], Text(Value))", "s")]
        [InlineData("Concat(Table({a:1, b:\"hello\"}, {b:\"world\"}), b)", "s")]
        [InlineData("Concat([1, 2, 3], Text(Value), \",\")", "s")]
        [InlineData("Concat([1, 2, 3], Text(Value), Text(Today()))", "s")]
        public void TexlFunctionTypeSemanticsConcat(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out DType type));
            Assert.True(type.IsValid);
            TestSimpleBindingSuccess(script, type);
        }

        [Theory]
        [InlineData("Concat([], 1)", "s")]
        [InlineData("Concat([1, 2, 3], Value)", "s")]
        [InlineData("Concat(Table({a:1, b:\"hello\"}, {b:\"world\"}), [\"hello\", \"world\"])", "s")]
        [InlineData("Concat([1, 2, 3], Value, Today())", "s")]
        [InlineData("Concat([1, 2, 3], Value, 1)", "s")]
        public void TexlFunctionTypeSemanticsConcat_Negative(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out DType type));
            Assert.True(type.IsValid);
            TestBindingErrors(script, type);
        }

        [Theory]
        [InlineData("Mod(2, 3)", "n")]
        [InlineData("Mod(2, -2)", "n")]
        [InlineData("Mod(-2, 2)", "n")]
        [InlineData("Mod(-2, -2)", "n")]
        [InlineData("Mod(T, 2)", "*[Result:n]")]
        [InlineData("Mod(T, T2)", "*[Result:n]")]
        [InlineData("Mod(3, T2)", "*[Result:n]")]
        public void TexlFunctionTypeSemanticsModOverloads(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Number:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Dividend:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Mod(\"3\", 2)", "n", null, null)]
        [InlineData("Mod(\"3\", true)", "n", null, null)]
        [InlineData("Mod(\"5\", T)", "*[Result:n]", "*[Booleans:b]", null)]
        [InlineData("Mod(T1, T2)", "*[Result:n]", "*[Booleans:b]", "*[Strings:s]")]
        [InlineData("Mod(T, false)", "*[Result:n]", "*[Strings:s]", null)]
        [InlineData("Mod([true, false, true], \"2\")", "*[Result:n]", null, null)]
        [InlineData("Mod(12.5656242, [\"5\", \"6\"])", "*[Result:n]", null, null)]
        public void TexlFunctionTypeSemanticsModOverloadsWithCoercion(string script, string expectedType, string typedGlobal1, string typedGlobal2)
        {
            if (typedGlobal1 != null)
            {
                if (typedGlobal2 != null)
                {
                    var symbol = new SymbolTable();
                    symbol.AddVariable("T1", new TableType(TestUtils.DT(typedGlobal1)));
                    symbol.AddVariable("T2", new TableType(TestUtils.DT(typedGlobal2)));
                    TestSimpleBindingSuccess(script, TestUtils.DT(expectedType), symbol);
                }
                else
                {
                    var symbol = new SymbolTable();
                    symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal1)));
                    TestSimpleBindingSuccess(script, TestUtils.DT(expectedType), symbol);
                }
            }
            else
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
            }
        }

        [Theory]
        [InlineData("Time(1,2,3)")]
        [InlineData("Time(1,2,3,4)")]
        [InlineData("Time(1,2, \"hello\")")]
        [InlineData("Time(1,2,3,\"hello\")")]
        public void TexlFunctionSemanticsTimeOverloads(string script)
        {
            TestSimpleBindingSuccess(script, DType.Time);
        }

        [Theory]
        [InlineData("Time(1)")]
        [InlineData("Time(1,2)")]
        public void TexlFunctionSemanticsTimeOverloadsNegative(string script)
        {
            TestBindingErrors(script, DType.Time);
        }

        [Theory(Timeout = 1000)]
        [InlineData(
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9", "n")]
        public void TexlExcessivelyLongButFlatRulesParseCorrectly(string script, string expectedType)
        {
            TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
        }

        [Theory]
        [InlineData("123", 1)]
        [InlineData("true", 1)]
        [InlineData("\"hello\"", 1)]
        [InlineData("ABC", 1)]
        [InlineData("A!B", 2)]
        [InlineData("A.B", 2)]
        [InlineData("A!B!C", 3)]
        [InlineData("A.B.C", 3)]
        [InlineData("A!B!C!D!E!A!B!C!D!E!A!B!C!D!E!A!B!C!D!E!A!B!C!D!E!A!B!C!D!E!A!B!C!D!E", 35)]
        [InlineData("A.B.C.D.E.A.B.C.D.E.A.B.C.D.E.A.B.C.D.E.A.B.C.D.E.A.B.C.D.E.A.B.C.D.E", 35)]
        [InlineData("First(T)", 3)]
        [InlineData("First(Last(T))", 5)]
        [InlineData("A(B(C(D(E(A(B(C(D(E(A(B(C(D(E(A(B(C(D(E(T))))))))))))))))))))", 41)]
        [InlineData("A(B(), C(), D(), E(), F(), G(), H(), I(), J())", 4)]
        [InlineData("1 + 2", 2)]
        [InlineData("1 + 2 + 3 + 4 + 5", 5)]
        [InlineData("(((1 + 2) + 3) + 4) + 5", 5)]
        [InlineData("1 + (2 + (3 + (4 + 5)))", 5)]
        [InlineData("1 + (2 + 3)", 3)]
        [InlineData("(1 + 2) + (3 + 4)", 3)]
        [InlineData("(1 + 2) * 8 + (3 + 4) * 9", 4)]
        [InlineData("!X", 2)]
        [InlineData("-X", 2)]
        [InlineData("-123", 2)]
        [InlineData("1 - 2", 3)]
        [InlineData("1 - X", 3)]
        [InlineData("X in Y", 2)]
        [InlineData("A; B; C; D; E", 2)]
        [InlineData("1 + 2; B; true || false; D; E", 3)]
        [InlineData("1 + (2 + 3); B; true || false; D; E", 4)]
        [InlineData("Collect(T, 1); Navigate(S1,\"\")", 4)]
        [InlineData("{}", 1)]
        [InlineData("{A:1, B:2}", 2)]
        [InlineData("{A:1, B:2, C:{X:1, Y:2}}", 3)]
        [InlineData("[]", 1)]
        [InlineData("[1, 2, 3, 4, 5]", 2)]
        [InlineData("[{A:1}]", 3)]
        [InlineData("{A:[1,2,3]}", 3)]
        [InlineData("[1 + 2, 3 + 4]", 3)]
        public void TexlTreeDepth(string script, int expectedDepth)
        {
            var result = TexlParser.ParseScript(script, flags: TexlParser.Flags.EnableExpressionChaining);
            var node = result.Root;
            Assert.NotNull(node);
            Assert.Equal(expectedDepth, node.Depth);
        }

        [Fact]
        public void TexlFunctionThatSupportsCoercedParams()
        {
            const TestUtils.FunctionFlags functionFlags = TestUtils.FunctionFlags.IsStrict | TestUtils.FunctionFlags.IsStateless | TestUtils.FunctionFlags.IsSelfContained | TestUtils.FunctionFlags.SupportsParamCoercion;

            var churnDataFunction = new TestUtils.MockFunction(ChurnDataFunctionName, string.Empty, FunctionCategories.Text, functionFlags, DType.Number, 0, 2, 2, DType.Number, DType.Number);
            var symbol = new SymbolTable();
            
            try
            {
                symbol.AddFunction(churnDataFunction);
                TestSimpleBindingSuccess("ChurnData(\"123\", true)", DType.Number, symbol);
            }
            finally
            {
                symbol.RemoveFunction(churnDataFunction);
            }
        }

        [Theory]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride(\"123\", true)", true)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride(123, true)", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([1,2,3], true)", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([\"1\",\"2\",\"3\"], \"2\")", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([\"1\",\"2\",\"3\"], true)", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([true,false,false], true)", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([\"1\",\"2\",\"3\"], 2)", false)]
        public void TexlFunctionThatSupportsSomeCoercedParams(string script, bool expectedErrors)
        {
            var mockFunction1 = new TestUtils.MockFunctionThatSupportsCoercedParamsWithOverride("MockFunctionThatSupportsCoercedParamsWithOverride", "_1", true, DType.Number, 0, 2, 2, DType.Number, DType.Number);
            var mockFunction2 = new TestUtils.MockFunctionThatSupportsCoercedParamsWithOverride("MockFunctionThatSupportsCoercedParamsWithOverride", "_2", true, DType.Number, 0, 2, 2, DType.EmptyTable, DType.Number)
            {
                CheckNumericTableOverload = true
            };
            var mockFunction3 = new TestUtils.MockFunctionThatSupportsCoercedParamsWithOverride("MockFunctionThatSupportsCoercedParamsWithOverride", "_3", false, DType.Number, 0, 2, 2, DType.EmptyTable, DType.String)
            {
                CheckStringTableOverload = true
            };
            var mockFunction4 = new TestUtils.MockFunctionThatSupportsCoercedParamsWithOverride("MockFunctionThatSupportsCoercedParamsWithOverride", "_4", false, DType.Number, 0, 2, 2, DType.EmptyTable, DType.Number)
            {
                CheckStringTableOverload = true
            };

            var symbol = new SymbolTable();

            try
            {
                symbol.AddFunction(mockFunction1);
                symbol.AddFunction(mockFunction2);
                symbol.AddFunction(mockFunction3);
                symbol.AddFunction(mockFunction4);

                if (expectedErrors)
                {
                    TestBindingErrors(script, DType.Number, symbol);
                }
                else
                {
                    TestSimpleBindingSuccess(script, DType.Number, symbol);
                }
            }
            finally
            {
                symbol.RemoveFunction(mockFunction1);
                symbol.RemoveFunction(mockFunction2);
                symbol.RemoveFunction(mockFunction3);
                symbol.RemoveFunction(mockFunction4);
            }
        }

        [Theory]
        [InlineData("MockFunction(\"123\", \"123\")", false)]
        [InlineData("MockFunction(true, true)", false)]
        [InlineData("MockFunction(\"123\", true)", false)]
        [InlineData("MockFunction(\"123\", 5)", false)]
        [InlineData("MockFunction([], [])", true)]
        public void TexlOverloadTakesPrecedenceOverCoercion(string script, bool expectedErrors)
        {
            var functionFlags = TestUtils.FunctionFlags.IsStrict | TestUtils.FunctionFlags.IsStateless | TestUtils.FunctionFlags.IsSelfContained;

            var mockFunction1 = new TestUtils.MockFunction("MockFunction", "_1", FunctionCategories.Text, functionFlags | TestUtils.FunctionFlags.SupportsParamCoercion, DType.Number, 0, 2, 2, DType.Number, DType.Number);
            var mockFunction2 = new TestUtils.MockFunction("MockFunction", "_2", FunctionCategories.Text, functionFlags, DType.Number, 0, 2, 2, DType.String, DType.String);
            var mockFunction3 = new TestUtils.MockFunction("MockFunction", "_3", FunctionCategories.Text, functionFlags | TestUtils.FunctionFlags.SupportsParamCoercion, DType.Number, 0, 2, 2, DType.String, DType.Number);
            
            var symbol = new SymbolTable();

            try
            {
                symbol.AddFunction(mockFunction1);
                symbol.AddFunction(mockFunction2);
                symbol.AddFunction(mockFunction3);

                if (expectedErrors)
                {
                    TestBindingErrors(script, DType.Number, symbol);
                }
                else
                {
                    TestSimpleBindingSuccess(script, DType.Number, symbol);
                }
            }
            finally
            {
                symbol.RemoveFunction(mockFunction1);
                symbol.RemoveFunction(mockFunction2);
                symbol.RemoveFunction(mockFunction3);
            }
        }

        [Fact]
        public void TestBlankFunction_Positive()
        {
            TestSimpleBindingSuccess("Blank()", TestUtils.DT("N"));
        }

        [Fact]
        public void TestBlankFunction_Negative()
        {
            TestBindingErrors("Blank(\"null\")", TestUtils.DT("N"));
        }

        [Theory]
        [InlineData("With({A: 1, B: \"test\"}, B & \" \" & A)", "![A:n, B:s]", "s")]
        [InlineData("With({table: [{name: \"first\"},{name: \"first\"}]}, ForAll(table, Value))", "![table:*[value:s]]", "*[name:s]")]
        [InlineData("With({date: Today()}, date)", "![date:D]", "D")]
        [InlineData("With({a: true, b: 5}, If(a, b+2, b-2))", "![a:b, b:n]", "n")]
        [InlineData("With({color: RGBA(255, 255, 255, 1)}, ThisRecord)", "", "![color:c]")]
        [InlineData("With({color: RGBA(255, 255, 255, 1)}, ThisRecord.color)", "![color:c]", "c")]
        [InlineData("With({color: RGBA(255, 255, 255, 1)} As Outer, Outer.color)", "![color:c]", "c")]
        [InlineData("With({r: 255, g: 255, b: 255, a: 1}, {color: RGBA(r, g, b, a)})", "![r:n, g:n, b:n, a:n]", "![color: c]")]
        public void TexlFunctionTypeSemanticsWith(string script, string typedGlobal, string expectedTypeString)
        {
            if (string.IsNullOrEmpty(typedGlobal))
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedTypeString));
            }
            else
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new KnownRecordType(TestUtils.DT(typedGlobal)));
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedTypeString), symbol);
            }
        }

        [Theory]
        [InlineData("With({table: DoesntExist}, 1)", "![table:e]", "n")]
        [InlineData("With(1)", "", "?")]
        public void TexlFunctionTypeSemanticsWith_Negative(string script, string typedGlobal, string expectedTypeString)
        {
            if (string.IsNullOrEmpty(typedGlobal))
            {
                TestBindingErrors(script, TestUtils.DT(expectedTypeString));
            }
            else
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new KnownRecordType(TestUtils.DT(typedGlobal)));
                TestBindingErrors(script, TestUtils.DT(expectedTypeString), symbol);
            }
        }

        [Theory]
        [InlineData("ForAll(T, \"A: \" & A)", "*[A:n]", "*[Value:s]")]
        [InlineData("ForAll(T, Rec.Item)", "*[Rec:![Item:o]]", "*[Value:o]")]
        [InlineData("ForAll(T, Rec)", "*[Rec:![Item:o]]", "*[Item:o]")]
        [InlineData("ForAll(T, {Num:A+5, Image:img})", "*[A:n, img:i, unused:s]", "*[Num:n, Image:i]")]
        [InlineData("ForAll(T, FirstN(table, 3))", "*[table:*[A:n]]", "*[Value:*[A:n]]")]
        [InlineData("ForAll(Table({Str:\"hello\", Num:5}), Num + 5)", "", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsForAll(string script, string typedGlobal, string expectedTypeString)
        {
            if (string.IsNullOrEmpty(typedGlobal))
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedTypeString));
            }
            else
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedTypeString), symbol);
            }
        }

        [Theory]
        [InlineData("ForAll(DoesntExist, 1)", "", "?")]
        [InlineData("ForAll(T, DoesntExist + 1)", "*[Exists:n]", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsForAll_Negative(string script, string typedGlobal, string expectedTypeString)
        {
            if (string.IsNullOrEmpty(typedGlobal))
            {
                TestBindingErrors(script, TestUtils.DT(expectedTypeString));
            }
            else
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));
                TestBindingErrors(script, TestUtils.DT(expectedTypeString), symbol);
            }
        }

        [Theory]
        [InlineData("Sum(T, 1)", "n")]
        [InlineData("Average(T, \"Item\")", "n")]
        [InlineData("Filter(T, true)", "*[Item:n]")]
        public void TestWarningOnLiteralPredicate(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Item:n]")));
            TestBindingWarning(
                script,
                TestUtils.DT(expectedType),
                expectedErrorCount: null,
                symbolTable: symbol);
        }

        [Theory]
        [InlineData("Log(2)")]
        [InlineData("Log(2, 7)")]
        [InlineData("Log(numvar)")]
        [InlineData("Log(numvar, 7)")]
        public void TexlFunctionTypeSemanticsLog(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("numvar", FormulaType.Number);

            TestSimpleBindingSuccess(
                script, 
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("Log(numvar, numtable)")]
        [InlineData("Log(numtable, numvar)")]
        [InlineData("Log(3, numtable)")]
        [InlineData("Log(numtable, 3)")]
        [InlineData("Log(numtable, numtable)")]
        public void TexlFunctionTypeSemanticsLog_T(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("numvar", FormulaType.Number);
            symbol.AddVariable("numtable", new TableType(TestUtils.DT("*[Num:n]")));

            TestSimpleBindingSuccess(
                script, 
                TestUtils.DT("*[Num:n]"),
                symbol);
        }

        [Theory]
        [InlineData("Log(errortable)")]
        [InlineData("Log(errortable, 3)")]
        [InlineData("Log(numbertable, errortable)")]
        public void TexlFunctionTypeSemanticsLog_T_Negative(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("errortable", new TableType(TestUtils.DT("*[Num:n, Other:s]")));
            symbol.AddVariable("numtable", new TableType(TestUtils.DT("*[Num:n]")));

            // Expected type is "n" because when we fail typechecking for all overloads, the binder picks the first one
            TestBindingErrors(
                script, 
                TestUtils.DT("n"),
                symbol);
        }

        [Theory]
        [InlineData("StartsWith(\"Hello\", \"He\")")]
        [InlineData("StartsWith(name, \"He\")")]
        [InlineData("StartsWith(\"Hello\", name)")]
        [InlineData("StartsWith(name, otherName)")]
        public void TexlFunctionTypeSemanticsStartsWith(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);
            symbol.AddVariable("otherName", FormulaType.String);

            TestSimpleBindingSuccess(
                script, 
                DType.Boolean,
                symbol);
        }

        [Theory]
        [InlineData("EndsWith(\"Hello\", \"lo\")")]
        [InlineData("EndsWith(name, \"lo\")")]
        [InlineData("EndsWith(\"Hello\", name)")]
        [InlineData("EndsWith(name, otherName)")]
        public void TexlFunctionTypeSemanticsEndsWith(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);
            symbol.AddVariable("otherName", FormulaType.String);

            TestSimpleBindingSuccess(
                script, 
                DType.Boolean,
                symbol);
        }

        [Theory]
        [InlineData("Coalesce(\"Hello\", \"He\")", "s")]
        [InlineData("Coalesce(1, Today())", "n")]
        [InlineData("Coalesce(Blank(), 1, Today())", "n")]
        [InlineData("Coalesce(name, url)", "s")]
        [InlineData("Coalesce(url, name)", "s")]
        [InlineData("Coalesce(url)", "h")]
        [InlineData("Coalesce(col)", "*[testname:s]")]
        [InlineData("Coalesce(Blank())", "N")]
        [InlineData("Coalesce(Blank(), Blank())", "N")]
        public void TexlFunctionTypeSemanticsCoalesce(string script, string typeSpec)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);
            symbol.AddVariable("url", FormulaType.Hyperlink);
            symbol.AddVariable("col", new TableType(TestUtils.DT("*[testname:s]")));

            TestSimpleBindingSuccess(
                script, 
                TestUtils.DT(typeSpec),
                symbol);
        }

        [Theory]
        [InlineData("Coalesce(\"Hello\", Color.Brown)", "s")]
        [InlineData("Coalesce(1, Color.Brown)", "n")]
        public void TexlFunctionTypeSemanticsCoalesce_Negative(string script, string typeSpec)
        {
            TestBindingErrors(script, TestUtils.DT(typeSpec));
        }

        [Theory]
        [InlineData("IsError(\"Hello\")", "b")]
        [InlineData("IsError(7)", "b")]
        [InlineData("IsError(1 / 0)", "b")]
        [InlineData("IsError(Sum(7, 42, 5))", "b")]
        [InlineData("IsError([1,2,3,4])", "b")]
        [InlineData("IsError({a:3, b:4})", "b")]
        public void TexlFunctionTypeSemanticsIsError(string script, string typeSpec)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);

            TestSimpleBindingSuccess(
                script, 
                TestUtils.DT(typeSpec),
                symbol);
        }

        [Theory]
        [InlineData("IsError(1, 2)", "b")]
        [InlineData("IsError()", "b")]
        public void TexlFunctionTypeSemanticsIsError_Negative(string script, string typeSpec)
        {
            TestBindingErrors(script, TestUtils.DT(typeSpec));
        }

        [Theory]
        [InlineData("Sequence(20)", "*[Value:n]")]
        [InlineData("Sequence(20, 30)", "*[Value:n]")]
        [InlineData("Sequence(20, 30, 10)", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsSequence(string script, string typeSpec)
        {
            TestSimpleBindingSuccess(script, TestUtils.DT(typeSpec));
        }

        [Theory]
        [InlineData("\"Hello World\" //Line Comment", "s")]
        [InlineData("6 //Line Comment \n * 4", "n")]
        [InlineData("\"Hello World\" /*Block Comment*/", "s")]
        [InlineData("/*Block Comment*/ \"Hello World\"", "s")]
        [InlineData("1 + 2 //Line Comment", "n")]
        [InlineData("1 + 2/*Block Comment*/", "n")]
        [InlineData("1 + /*Block Comment*/ 2", "n")]
        [InlineData("1 /*Block Comment*/ + 2", "n")]
        [InlineData("/*Block Comment*/ 1 + 2", "n")]
        public void TexlTestCommentingSemantics(string script, string typeSpec)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);
            var temp = TestUtils.DT("%s[X:\"hi world\"]");
            
            TestSimpleBindingSuccess(
                script, 
                TestUtils.DT(typeSpec),
                symbol);
        }

        private void TestBindingPurity(string script, bool isPure, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.NotNull(result._binding);
        
            Assert.Equal(isPure, result._binding.IsPure(result.Parse.Root));
        }

        private void TestBindingWarning(string script, DType expectedType, int? expectedErrorCount, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);
            
            Assert.Equal(expectedType, result._binding.ResultType);
            Assert.True(result._binding.ErrorContainer.HasErrors());
            if (expectedErrorCount != null)
            {
                Assert.Equal(expectedErrorCount, result._binding.ErrorContainer.GetErrors().Count());
            }

            Assert.True(result.IsSuccess);
        }

        private void TestBindingErrors(string script, DType expectedType, int expectedErrorCount, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.Equal(expectedType, result._binding.ResultType);
            Assert.Equal(expectedErrorCount, result._binding.ErrorContainer.GetErrors().Count());
            Assert.False(result.IsSuccess);
        }

        private void TestBindingErrors(string script, DType expectedType, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.Equal(expectedType, result._binding.ResultType);
            Assert.False(result.IsSuccess);
        }

        internal static void TestSimpleBindingSuccess(string script, DType expectedType, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = symbolTable
            };
            var engine = new Engine(config);
            var result = engine.Check(script);
            Assert.Equal(expectedType, result._binding.ResultType);
            Assert.True(result.IsSuccess);
        }
    }
}
