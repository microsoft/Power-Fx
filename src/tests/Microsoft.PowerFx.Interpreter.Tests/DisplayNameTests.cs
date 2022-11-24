// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Tests;
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

            var recordType = RecordType.Empty()
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

            var tv = TableValue.NewTable(recordType, rv1, rv2, rv3);

            var parameters = RecordValue.NewRecordFromFields(
                new NamedValue("myTable", tv));

            var result = engine.Eval("DropColumns(myTable, displayName)", parameters);
           
            var sb = new StringBuilder();
            result.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
            var output = sb.ToString();

            Assert.Equal("Table({logicalB:4},{logicalB:5},{logicalB:6})", output);

            var displayExpression = engine.GetDisplayExpression("DropColumns(myTable, displayName)", parameters.Type);
            var invariantExpression = engine.GetInvariantExpression("DropColumns(myTable, displayName)", parameters.Type);

            Assert.Equal("DropColumns(myTable, displayName)", displayExpression);
            Assert.Equal("DropColumns(myTable, logicalA)", invariantExpression);
        }
    }
}
