// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Threading;
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

            var symbols = new SymbolTable();
            symbols.EnableMutationFunctions();
            symbols.AddVariable("MyTable", tableType);

            var engine = new RecalcEngine(new PowerFxConfig());
            var checkResult = engine.Check("Patch(MyTable, { Currency: 1.2 }, { Currency: 1.5 })", new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbols);

            Assert.True(checkResult.IsSuccess, string.Join("\r\n", checkResult.Errors.Select(err => $"{err.Message}")));

            var runtimeConfig = new SymbolValues();
            runtimeConfig.Add("MyTable", TableValue.NewTable(tableType.ToRecord()));
            
            var evalResult = checkResult.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig).Result;
            Assert.IsNotType<ErrorValue>(evalResult);
        }
    }
}
