// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class BinderTests : PowerFxTest
    {
        [Theory]
        [InlineData("x")]
        [InlineData("x.Value")]
        [InlineData("x.Tbl")]
        [InlineData("First(x.Tbl)")]
        [InlineData("First(x.Tbl).Value2")]
        [InlineData("Last(x.Tbl).Value2")]
        [InlineData("Index(x.Tbl, 3).Value2")]
        [InlineData("x.Rec")]
        [InlineData("x.Rec.Value3")]
        [InlineData("tbl")]
        [InlineData("First(tbl)")]
        [InlineData("Last(tbl)")]
        [InlineData("Index(tbl, 3)")]
        [InlineData("MyDataSource")]
        public void TestMutableNodes(string expression)
        {
            var config = new PowerFxConfig();
            config.SymbolTable.AddVariable(
                "x",
                new KnownRecordType(TestUtils.DT("![Value:n,Tbl:*[Value2:n],Rec:![Value3:n,Value4:n]]")),
                mutable: true);
            config.SymbolTable.AddVariable(
                "tbl",
                TableType.Empty().Add("Value", FormulaType.Number),
                mutable: true);
            var schema = DType.CreateTable(
                new TypedName(DType.Guid, new DName("ID")),
                new TypedName(DType.Number, new DName("Value")));
            config.SymbolTable.AddEntity(new TestDataSource("MyDataSource", schema));

            var engine = new Engine(config);
            var checkResult = engine.Check(expression);
            Assert.True(checkResult.IsSuccess);
            var binding = checkResult.Binding;
            Assert.True(binding.IsMutable(binding.Top));
        }

        [Theory]
        [InlineData("nf")]
        [InlineData("const")]
        [InlineData("ShowColumns(FirstN(x.Tbl, 3), Value2)")]
        [InlineData("ShowColumns(LastN(x.Tbl, 3), Value2)")]
        [InlineData("Filter(tbl, Value > 10)")]
        [InlineData("FirstN(tbl, 2)")]
        [InlineData("LastN(tbl, 2)")]
        [InlineData("First(MyDataSource)")]
        public void TestImmutableNodes(string expression)
        {
            var config = new PowerFxConfig();
            config.SymbolTable.AddConstant("const", new NumberValue(IR.IRContext.NotInSource(FormulaType.Number), 2));
            config.SymbolTable.AddVariable("nf", new TableType(TestUtils.DT("*[Value:n]")), mutable: false);
            config.SymbolTable.AddVariable(
                "x",
                new KnownRecordType(TestUtils.DT("![Value:n,Tbl:*[Value2:n],Rec:![Value3:n,Value4:n]]")),
                mutable: true);
            config.SymbolTable.AddVariable(
                "tbl",
                TableType.Empty().Add("Value", FormulaType.Number),
                mutable: true);
            var schema = DType.CreateTable(
                new TypedName(DType.Guid, new DName("ID")),
                new TypedName(DType.Number, new DName("Value")));
            config.SymbolTable.AddEntity(new TestDataSource("MyDataSource", schema));

            var engine = new Engine(config);
            var checkResult = engine.Check(expression);
            Assert.True(checkResult.IsSuccess);
            var binding = checkResult.Binding;
            Assert.False(binding.IsMutable(binding.Top));
        }

        private class FakePatchFunction : BuiltinFunction
        {
            public FakePatchFunction()
                : base("Patch", (x) => "Patch", FunctionCategories.Table, DType.EmptyRecord, 0, 3, 3)
            {
            }

            public override bool IsSelfContained => false;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                return Enumerable.Empty<TexlStrings.StringGetter[]>();
            }
        }

        [Fact]
        public void TestImmutableNodesWithScopeVariable()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            config.SymbolTable.AddVariable("tableVar", new TableType(TestUtils.DT("*[Value:n]")), mutable: true);
            config.AddFunction(new FakePatchFunction());

            var checkResult = engine.Check("With({x:[1,2,3]},x)");
            Assert.True(checkResult.IsSuccess);
            var binding = checkResult.Binding;
            var withCallNode = binding.Top.AsCall();
            var xScopeVarNode = withCallNode.Args.ChildNodes[1];
            Assert.False(binding.IsMutable(xScopeVarNode));

            var parserOptions = new ParserOptions { AllowsSideEffects = true };
            checkResult = engine.Check("With({a:1},Patch(tableVar,{Value:a},{Value:a+1}))", parserOptions);
            Assert.True(checkResult.IsSuccess);
            binding = checkResult.Binding;
            withCallNode = binding.Top.AsCall();
            var patchCallNode = withCallNode.Args.ChildNodes[1].AsCall();
            var tableVarNode = patchCallNode.Args.ChildNodes[0];
            Assert.True(binding.IsMutable(tableVarNode));

            var valueANode = patchCallNode.Args.ChildNodes[1].AsRecord();
            var aNode = valueANode.ChildNodes[0];
            Assert.False(binding.IsMutable(aNode));
        }

        [Fact]
        public void TestHasErrorsInTreeCallNode()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            config.SymbolTable.AddVariable("tableVar", new TableType(TestUtils.DT("*[Value:n]")));

            // Function with wrong number of arguments, first error will be a token not a node error
            var checkResult = engine.Check("FirstN([1, 2, 3, 4, 5])");
            Assert.False(checkResult.IsSuccess);

            var binding = checkResult.Binding;

            Assert.True(binding.ErrorContainer.HasErrorsInTree(binding.Top));
        }

        public class SimpleExpressionEngine : Engine
        {
            public SimpleExpressionEngine()
                : base(new PowerFxConfig())
            { 
            }

            private protected override BindingConfig GetDefaultBindingConfig(ParserOptions options, RecordType ruleScope = null)
            {
                return new BindingConfig(allowsSideEffects: options.AllowsSideEffects, numberIsFloat: options.NumberIsFloat, enforceSimpleExpressions: true);
            }
        }

        [Theory]
        [InlineData("123")]
        [InlineData("\"abc\"")]
        [InlineData("Lower(\"abc\")")]
        [InlineData("Trim(\"abc\")")]
        [InlineData("Len(\"abc\")")]
        [InlineData("Text(Value(\"abc\"))")]
        [InlineData("If(true, 1, 2)")]
        [InlineData("If(1=2, Color.Red, Color.Blue)")]
        [InlineData("With({Value:1234}, Value > 1)")]
        [InlineData("With({Value:1234}, ThisRecord.Value > 1)")]

        // Boolean functions
        [InlineData("Boolean(1)")]
        [InlineData("Boolean(\"true\")")]
        [InlineData("And(true, false)")]
        [InlineData("Or(true, false)")]
        [InlineData("Not(true)")]

        // Char / UniChar
        [InlineData("Char(65)")]
        [InlineData("UniChar(65)")]

        // Concatenate and string interpolation
        [InlineData("Concatenate(\"Hello\", \" \", \"World\")")]
        [InlineData("\"Hello \" & \"World\"")]

        // DateTime functions
        [InlineData("Date(2023, 1, 1)")]
        [InlineData("Time(12, 30, 45)")]
        [InlineData("DateTime(2023, 1, 1, 12, 30, 45)")]
        [InlineData("Year(Date(2023, 1, 1))")]
        [InlineData("Month(Date(2023, 1, 1))")]
        [InlineData("Day(Date(2023, 1, 1))")]
        [InlineData("Hour(Time(12, 30, 45))")]
        [InlineData("Minute(Time(12, 30, 45))")]
        [InlineData("Second(Time(12, 30, 45))")]
        [InlineData("Weekday(Date(2023, 1, 1))")]
        [InlineData("WeekNum(Date(2023, 1, 1))")]
        [InlineData("DateValue(\"2023-01-01\")")]
        [InlineData("TimeValue(\"12:30:45\")")]
        [InlineData("DateTimeValue(\"2023-01-01T12:30:45\")")]

        // Hex conversion
        [InlineData("Dec2Hex(255)")]
        [InlineData("Hex2Dec(\"FF\")")]

        // Logical
        [InlineData("And(true, true, true)")]
        [InlineData("Or(false, false, true)")]

        // Math functions
        [InlineData("Pi()")]

        // String manipulation
        [InlineData("Replace(\"abcdef\", 1, 2, \"cd\")")]
        [InlineData("Substitute(\"abcabc\", \"a\", \"x\")")]

        // More complex simple expressions
        [InlineData("If(And(true, Not(false)), Concatenate(\"Hello\", \" \", \"World\"), \"Goodbye\")")]
        [InlineData("Text(DateValue(\"2023-01-01\"), \"yyyy-MM-dd\")")]
        [InlineData("Lower(Substitute(\"Hello World\", \" \", \"-\"))")]
        [InlineData("Switch(1, 1, \"One\", 2, \"Two\", \"Other\")")]
        public void TestExpressionSimpleConstraintValid(string expr)
        {
            var engine = new SimpleExpressionEngine();
            var checkResult = engine.Check(expr);
            Assert.True(checkResult.IsSuccess);
        }

        [Theory]
        [InlineData("IfError(1/0, 1)")] // Note, IfError is always Async (consider changing that/adding a sync version)
        [InlineData("Filter(tableVar, Value > 1)")]
        [InlineData("Filter(tableVar, ThisRecord.Value > 1)")]
        [InlineData("With(recordVar, Value > 1)")]
        [InlineData("With(recordVar, ThisRecord.Value > 1)")]
        [InlineData("AsType(ParseJson(\"Foo\"), Type(Text))")]
        [InlineData("Split(\"foo\", \"f\")")]
        public void TestExpressionSimpleConstraintError(string expr)
        {
            var engine = new SimpleExpressionEngine();
            engine.Config.SymbolTable.AddVariable("tableVar", new TableType(TestUtils.DT("*[Value:n]")));
            engine.Config.SymbolTable.AddVariable("recordVar", new KnownRecordType(TestUtils.DT("![Value:n]")));
            var checkResult = engine.Check(expr);
            Assert.False(checkResult.IsSuccess);
        }
    }
}
