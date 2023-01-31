// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        [InlineData("EndsWith(hyperlinkVar,\".com\")")]
        [InlineData("Text(hyperlinkVar)")]
        [InlineData("hyperlinkVar & extraHyperlinkVar")]
        [InlineData("hyperlinkVar & extraHyperlinkVar & stringVar")]
        [InlineData("With({t1:Table({a:hyperlinkVar})},Patch(t1,First(t1),{a:stringVar}))")]
        public void TempTest(string expr)
        {
            var url1 = "https://www.msn.com";
            var url2 = "https://www.microsoft.com";

            var engine = new RecalcEngine(new PowerFxConfig());

            var hyperlinkVar = FormulaValue.NewUrl(url1);
            var extraHyperlinkVar = FormulaValue.NewUrl(url2);
            var stringVar = FormulaValue.New("lichess.org");

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.Config.SymbolTable.AddConstant("hyperlinkVar", hyperlinkVar);
            engine.Config.SymbolTable.AddConstant("extraHyperlinkVar", extraHyperlinkVar);
            engine.Config.SymbolTable.AddConstant("stringVar", stringVar);

            var result = engine.Eval(expr, options: new ParserOptions() { AllowsSideEffects = true });

            Assert.IsNotType<ErrorValue>(result);
        }
    }
}
