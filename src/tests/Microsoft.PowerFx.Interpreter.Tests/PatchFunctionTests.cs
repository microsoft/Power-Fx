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
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class PatchFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Theory]
        [InlineData(typeof(PatchFunction))]
        [InlineData(typeof(PatchRecordFunction))]
        public async Task CheckArgsTestAsync(Type type)
        {
            var expressionError = new ExpressionError()
            {
                Kind = ErrorKind.ReadOnlyValue,
                Severity = ErrorSeverity.Critical,
                Message = "Something went wrong"
            };

            FormulaValue[] args = new[]
            {
                FormulaValue.NewError(expressionError)
            };

            var function = Activator.CreateInstance(type) as IAsyncTexlFunction;
            var result = await function.InvokeAsync(args, CancellationToken.None);

            Assert.IsType<ErrorValue>(result);
        }

        [Fact]
        public void UpdateVariableTest()
        {
            var config = new PowerFxConfig();
            var symbol = new SymbolTable();

            symbol.EnableMutationFunctions();
            config.SymbolTable = symbol;

            var engine = new RecalcEngine(config);

            RecordValue r1 = FormulaValue.NewRecordFromFields(new NamedValue("property", FormulaValue.New("x")));
            RecordValue r2 = FormulaValue.NewRecordFromFields(new NamedValue("property", FormulaValue.New("check")));

            engine.UpdateVariable("TestVar", r1);
            engine.UpdateVariable("r2", r2);

            var checkResult = engine.Check("Patch(TestVar,r2)", options: _opts);
            Assert.True(checkResult.IsSuccess);

            var result = engine.Eval("Patch(TestVar,r2)", options: _opts);
            engine.UpdateVariable("TestVar", result);
            Assert.Equal(r2.Dump(), engine.GetValue("TestVar").Dump());
        }
    }
}
