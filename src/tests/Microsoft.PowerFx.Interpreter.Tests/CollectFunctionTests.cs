// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class CollectFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Theory]
        [InlineData("Collect(t, r1)", 1)]
        [InlineData("Collect(t, r1);Collect(t, r1);Collect(t, r1)", 3)]
        [InlineData("Collect(t, r1);Collect(t, Blank())", 1)]
        [InlineData("Collect(t, r1);Collect(t, {})", 2)]
        public async Task AppendCountTest(string script, int expected)
        {
            var engine = new RecalcEngine();
            var symbol = engine.Config.SymbolTable;

            var listT = new List<RecordValue>();

            engine.Config.EnableMutationFunctions();

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("Field1", FormulaValue.New(1)),
                new NamedValue("Field2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(r1.Type, listT);

            engine.UpdateVariable("t", t);
            symbol.AddConstant("r1", r1);

            var result = await engine.EvalAsync(script, CancellationToken.None, options: _opts, symbolTable: symbol).ConfigureAwait(false);
            var resultCount = await engine.EvalAsync("t", CancellationToken.None, options: _opts, symbolTable: symbol).ConfigureAwait(false);

            Assert.Equal(expected, ((TableValue)resultCount).Count());
        }
    }
}
