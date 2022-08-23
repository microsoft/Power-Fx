// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class CollectFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Theory]
        [InlineData("Collect(t, {MyField1:2, MyField2:\"hello1\"});CountRows(t)", 1)]
        [InlineData("Collect(t, r1);Collect(t, r1);Collect(t, r1);CountRows(t)", 3)]
        [InlineData("Collect(t, r1);Collect(t, If(1>0, r1,Blank()));CountRows(t)", 2)]
        public async Task AppendCountTest(string script, int expected)
        {
            var symbol = new SymbolTable();
            var listT = new List<RecordValue>();

            symbol.EnableCollectFunction();

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("MyField1", FormulaValue.New(1)),
                new NamedValue("MyField2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(r1.Type, listT);

            symbol.AddConstant("t", t);
            symbol.AddConstant("r1", r1);

            var engine = new RecalcEngine();
            var resultCount = await engine.EvalAsync(script, CancellationToken.None, options: _opts, symbolTable: symbol);
            
            Assert.Equal(expected, ((NumberValue)resultCount).Value);
            Assert.Equal(expected, listT.Count);
        }

        [Theory]
        [InlineData("IsBlank(Collect(t, Blank()))", true)]
        [InlineData("IsError(Collect(t, If(1/0, r1)))", true)]
        public async Task ReturnTest(string script, bool expected)
        {
            var symbol = new SymbolTable();

            symbol.EnableCollectFunction();

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("MyField1", FormulaValue.New(1)),
                new NamedValue("MyField2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(RecordValue.Empty().Type, new List<RecordValue>());

            symbol.AddConstant("t", t);
            symbol.AddConstant("r1", r1);

            var engine = new RecalcEngine();
            var resultCount = await engine.EvalAsync(script, CancellationToken.None, options: _opts, symbolTable: symbol);

            Assert.Equal(expected, ((BooleanValue)resultCount).Value);
        }

        [Theory]
        [InlineData("Collect(t)")]
        [InlineData("Collect(t, r1, r1)")]
        [InlineData("Collect(t, r1, 1; 2; r1)")]
        public void AppendMultipleTest(string script)
        {
            var symbol = new SymbolTable();
            var listT = new List<RecordValue>();

            symbol.EnableCollectFunction();

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("MyField1", FormulaValue.New(1)),
                new NamedValue("MyField2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(r1.Type, listT);

            symbol.AddConstant("t", t);
            symbol.AddConstant("r1", r1);

            var engine = new RecalcEngine();
            var result = engine.Check(script, options: _opts, symbolTable: symbol);

            // Currently the Collect function does not handle multiple records
            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("Collect(t, \"x\")")]
        public void AppendTypeCheckingTest(string script)
        {
            var symbol = new SymbolTable();
            var listT = new List<RecordValue>();

            symbol.EnableCollectFunction();

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("MyField1", FormulaValue.New(1)),
                new NamedValue("MyField2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(r1.Type, listT);

            symbol.AddConstant("t", t);
            symbol.AddConstant("r1", r1);

            var engine = new RecalcEngine();
            var result = engine.Check(script, options: _opts, symbolTable: symbol);

            Assert.False(result.IsSuccess); 
        }

        [Fact]
        public void CollectNonBehaviorTest()
        {
            var symbol = new SymbolTable();
            var listT = new List<RecordValue>();

            symbol.EnableCollectFunction();

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("MyField1", FormulaValue.New(1)),
                new NamedValue("MyField2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(r1.Type, listT);

            symbol.AddConstant("t", t);
            symbol.AddConstant("r1", r1);

            // Calling the engine with out the AllowSideEffect flag
            var engine = new RecalcEngine();
            var result = engine.Check("Collect(t, r1)", symbolTable: symbol);

            Assert.False(result.IsSuccess);
        }
    }
}
