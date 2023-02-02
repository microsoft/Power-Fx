// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class IRTests
    {
        [Fact]
        public void ValidateNoRecordToRecordAggregateCoercion()
        {
            var tableType = TableType.Empty().Add(new NamedFormulaType(new TypedName(DType.Currency, new DName("Currency"))));

            var symbols = new SymbolTable { DebugName = "ST1 " };
            symbols.EnableMutationFunctions();
            var slot = symbols.AddVariable("MyTable", tableType);

            var engine = new RecalcEngine(new PowerFxConfig());
            var checkResult = engine.Check("Patch(MyTable, { Currency: 1.2 }, { Currency: 1.5 })", new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbols);

            checkResult.ThrowOnErrors();

            var runtimeConfig = new SymbolValues(symbols) { DebugName = "SV1" };
            runtimeConfig.Set(slot, TableValue.NewTable(tableType.ToRecord()));

            var evalResult = checkResult.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig).Result;
            Assert.IsNotType<ErrorValue>(evalResult);

            var ir = IRTranslator.Translate(checkResult.Binding).ToString();
            Assert.DoesNotContain("AggregateCoercionNode", ir);
        }

        [Theory]

        [InlineData("With({t1:Table({a:stringVar})},Patch(t1,First(t1),{a:integerVar}))")]
        [InlineData("With({t1:Table({a:5})},Patch(t1,First(t1),{a:datetimeVar}))")]
        public void RecordToRecordAggregateCoercionTest(string expr)
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            var stringVar = FormulaValue.New("lichess.org");
            var integerVar = FormulaValue.New(1);
            var datetimeVar = FormulaValue.New(DateTime.Now);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.Config.SymbolTable.AddConstant("stringVar", stringVar);
            engine.Config.SymbolTable.AddConstant("integerVar", integerVar);
            engine.Config.SymbolTable.AddConstant("datetimeVar", datetimeVar);

            var result = engine.Eval(expr, options: new ParserOptions() { AllowsSideEffects = true });

            Assert.IsNotType<ErrorValue>(result);
        }

        [Theory]
        [InlineData("stringVar & hyperlinkVar", "somethinghttps://www.bing.com")]
        [InlineData("stringVar & hyperlinkVar & 1", "somethinghttps://www.bing.com1")]
        [InlineData("Upper(hyperlinkVar)", "HTTPS://WWW.BING.COM")]
        [InlineData("Text(hyperlinkVar)", "https://www.bing.com")]
        [InlineData("Proper(hyperlinkVar)", "Https://Www.Bing.Com")]
        [InlineData("With({t1:Table({a:stringVar})},Patch(t1,First(t1),{a:hyperlinkVar});First(t1).a)", "https://www.bing.com")]
        [InlineData("With({t1:Table({a:hyperlinkVar})},Patch(t1,First(t1),{a:stringVar});First(t1).a)", "something")]
        public void HyperlinkTextCoercionTest(string expr, string expected)
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            var stringVar = FormulaValue.New("something");
            var integerVar = FormulaValue.New(1);
            var hyperlinkVar = FormulaValue.NewUrl("https://www.bing.com");

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.Config.SymbolTable.AddConstant("stringVar", stringVar);
            engine.Config.SymbolTable.AddConstant("hyperlinkVar", hyperlinkVar);

            var result = engine.Eval(expr, options: new ParserOptions() { AllowsSideEffects = true });

            Assert.IsNotType<ErrorValue>(result);

            if (result is HyperlinkValue hValue)
            {
                Assert.Equal(expected, hValue.Value);
            }
            else if (result is StringValue sValue)
            {
                Assert.Equal(expected, sValue.Value);
            }            
        }
    }
}
