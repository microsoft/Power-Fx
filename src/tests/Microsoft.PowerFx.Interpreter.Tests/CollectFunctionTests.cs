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
        [InlineData("Collect(t, r1)", 1)]
        [InlineData("Collect(t, r1);Collect(t, r1);Collect(t, r1)", 3)]
        [InlineData("Collect(t, r1);Collect(t, Blank())", 1)]
        [InlineData("Collect(t, r1);Collect(t, {})", 2)]
        public async Task AppendCountTest(string script, int expected)
        {
            var symbol = new SymbolTable();
            var listT = new List<RecordValue>();

            symbol.EnableMutationFunctions();

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("Field1", FormulaValue.New(1)),
                new NamedValue("Field2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(r1.Type, listT);

            symbol.AddConstant("t", t);
            symbol.AddConstant("r1", r1);

            var engine = new RecalcEngine();
            var resultCount = await engine.EvalAsync(script, CancellationToken.None, options: _opts, symbolTable: symbol);

            Assert.Equal(expected, listT.Count);
        }
    }
}
