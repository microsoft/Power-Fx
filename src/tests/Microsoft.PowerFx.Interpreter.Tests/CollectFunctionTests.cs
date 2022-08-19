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
        public void AppendCountTest(string script, int expected)
        {
            var config = new PowerFxConfig();

            config.EnableCollectFunction();
            config.EnableSetFunction();

            var engine = new RecalcEngine(config);

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("MyField1", FormulaValue.New(1)),
                new NamedValue("MyField2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(r1.Type, new List<RecordValue>());

            engine.UpdateVariable("t", t);
            engine.UpdateVariable("r1", r1);

            engine.Check(script, options: _opts);

            var resultCount = (NumberValue)engine.Eval(script, options: _opts);
            
            Assert.Equal(expected, resultCount.Value);
        }
    }
}
