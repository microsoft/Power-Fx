// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ClearFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Fact]
        public async Task CheckArgsTestAsync()
        {
            var functionImpl = new ClearFunctionImpl();
            var function = new ClearFunction();
            var expressionError = new ExpressionError()
            {
                Kind = ErrorKind.ReadOnlyValue,
                Severity = ErrorSeverity.Critical,
                Message = "Something went wrong"
            };

            var faultyArs = new FormulaValue[]
            {
                FormulaValue.NewError(expressionError),
                FormulaValue.NewBlank()
            };

            foreach (var arg in faultyArs)
            {
                BasicServiceProvider serviceProvider = new BasicServiceProvider();
                serviceProvider.AddService(new FunctionExecutionContext(new FormulaValue[] { arg }));
                var result = await functionImpl.InvokeAsync(serviceProvider, CancellationToken.None).ConfigureAwait(false);

                if (arg is ErrorValue)
                {
                    Assert.IsType<ErrorValue>(result);
                }

                if (arg is BlankValue)
                {
                    Assert.IsType<BlankValue>(result);
                }
            }
        }

        [Fact]
        public void UpdateVariableTest()
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);

            config.SymbolTable.EnableMutationFunctions();

            var r1 = FormulaValue.NewRecordFromFields(new NamedValue("property", FormulaValue.New("x")));
            var r2 = FormulaValue.NewRecordFromFields(new NamedValue("property", FormulaValue.New("check")));

            var t1 = FormulaValue.NewTable(r1.Type, new List<RecordValue>() { r1, r2 });

            engine.UpdateVariable("t1", t1);

            var checkResult = engine.Check("Clear(t1)", options: _opts);
            Assert.True(checkResult.IsSuccess);

            var result = engine.Eval("Clear(t1)", options: _opts);
            var emptyTable = (TableValue)engine.GetValue("t1");

            Assert.Equal(0, emptyTable.Count());
        }
    }
}
