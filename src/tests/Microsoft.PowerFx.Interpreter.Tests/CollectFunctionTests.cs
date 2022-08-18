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
        private static readonly TypeMarshallerCache _cache = new TypeMarshallerCache();

        [Fact]
        public void AppendRecord()
        {
            var config = new PowerFxConfig();

            config.AddFunction(new CollectFunction());

            var engine = new RecalcEngine(config);

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("MyField1", FormulaValue.New(1)),
                new NamedValue("MyField2", FormulaValue.New("Hello Earth!!!")));

            RecordValue r2 = FormulaValue.NewRecordFromFields(
                new NamedValue("MyField1", FormulaValue.New(2)),
                new NamedValue("MyField2", FormulaValue.New("Hello Mars!!!")));

            var list = new List<RecordValue>();

            var t = (RecordsOnlyTableValue)FormulaValue.NewTable(r1.Type, list);

            engine.UpdateVariable("list", t);
            engine.UpdateVariable("r1", r1);

            engine.Eval("Collect(list, r1)"); // [1]

            t.Append(r2); // [2]

            Assert.Equal(2, list.Count);

            var resultCount = (NumberValue)engine.Eval("CountRows(list)");
            Assert.Equal(2, resultCount.Value);
        }
    }

    internal class TestRow
    {
        public double MyField1 { get; set; }

        public string MyField2 { get; set; }
    }
}
