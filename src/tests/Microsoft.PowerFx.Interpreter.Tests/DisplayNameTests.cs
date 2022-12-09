// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DisplayNameTests
    {
        [Fact]
        public void DisplayNameTest_DropColumns()
        {
            var pfxConfig = new PowerFxConfig(Features.SupportColumnNamesAsIdentifiers);
            var engine = new RecalcEngine(pfxConfig);

            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Number, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Number, displayName: "displayName2"));

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

            Assert.Equal("*[logicalB`displayName2:n]", result.Type.ToStringWithDisplayNames());

            var output = result.ToExpression();

            Assert.Equal("Table({logicalB:4},{logicalB:5},{logicalB:6})", output);

            var displayExpression = engine.GetDisplayExpression("DropColumns(myTable, displayName)", parameters.Type);
            var invariantExpression = engine.GetInvariantExpression("DropColumns(myTable, displayName)", parameters.Type);

            Assert.Equal("DropColumns(myTable, displayName)", displayExpression);
            Assert.Equal("DropColumns(myTable, logicalA)", invariantExpression);

            var resultD = engine.Eval(displayExpression, parameters);
            var resultI = engine.Eval(invariantExpression, parameters);

            Assert.Equal("*[logicalB`displayName2:n]", resultD.Type.ToStringWithDisplayNames());
            Assert.Equal("*[logicalB`displayName2:n]", resultI.Type.ToStringWithDisplayNames());
        }

        [Fact]
        public void DisplayNameTest_AddColumns()
        {
            var pfxConfig = new PowerFxConfig(Features.SupportColumnNamesAsIdentifiers);
            var engine = new RecalcEngine(pfxConfig);

            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Number, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Number, displayName: "displayName2"));

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
            var parameters = RecordValue.NewRecordFromFields(new NamedValue("myTable", tv));
            var result = engine.Eval("AddColumns(myTable, newColumn, displayName * logicalB)", parameters);

            Assert.Equal("*[logicalA`displayName:n, logicalB`displayName2:n, newColumn:n]", result.Type.ToStringWithDisplayNames());

            var output = result.ToExpression();

            Assert.Equal("Table({logicalA:1,logicalB:4,newColumn:4},{logicalA:2,logicalB:5,newColumn:10},{logicalA:3,logicalB:6,newColumn:18})", output);

            var displayExpression = engine.GetDisplayExpression("AddColumns(myTable, newColumn, displayName * logicalB)", parameters.Type);
            var invariantExpression = engine.GetInvariantExpression("AddColumns(myTable, newColumn, displayName * logicalB)", parameters.Type);

            Assert.Equal("AddColumns(myTable, newColumn, displayName * displayName2)", displayExpression);
            Assert.Equal("AddColumns(myTable, newColumn, logicalA * logicalB)", invariantExpression);

            var resultD = engine.Eval(displayExpression, parameters);
            var resultI = engine.Eval(invariantExpression, parameters);

            Assert.Equal("*[logicalA`displayName:n, logicalB`displayName2:n, newColumn:n]", resultD.Type.ToStringWithDisplayNames());
            Assert.Equal("*[logicalA`displayName:n, logicalB`displayName2:n, newColumn:n]", resultI.Type.ToStringWithDisplayNames());
        }
    }
}
