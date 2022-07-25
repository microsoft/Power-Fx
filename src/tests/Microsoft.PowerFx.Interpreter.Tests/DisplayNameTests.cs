// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DisplayNameTests
    {
        [Fact]
        public void DisplayNameTest1()
        {
            var pfxConfig = new PowerFxConfig(Features.SupportIdentifiers);
            var engine = new RecalcEngine(pfxConfig);

            var rt = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Number, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Number));

            var rv1 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(1)),
                new NamedValue("logicalB", FormulaValue.New(4)));
            var rv2 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(2)),
                new NamedValue("logicalB", FormulaValue.New(5)));
            var rv3 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(3)),
                new NamedValue("logicalB", FormulaValue.New(6)));

            var tv = TableValue.NewTable(rt, rv1, rv2, rv3);

            var parameters = RecordValue.NewRecordFromFields(
                new NamedValue("myTable", tv));

            var result = engine.Eval("DropColumns(myTable, displayName)", parameters);
            var output = TestRunner.TestToString(result);

            Assert.Equal("Table({logicalB:4},{logicalB:5},{logicalB:6})", output);
        }
    }
}
