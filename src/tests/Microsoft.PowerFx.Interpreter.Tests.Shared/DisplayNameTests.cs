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
            var pfxConfig = new PowerFxConfig(new Features { SupportColumnNamesAsIdentifiers = true });
            var engine = new RecalcEngine(pfxConfig);

            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Number, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Number, displayName: "displayName2"));

            var rv1 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(1.0)),
                new NamedValue("logicalB", FormulaValue.New(4.0)));
            var rv2 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(2.0)),
                new NamedValue("logicalB", FormulaValue.New(5.0)));
            var rv3 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(3.0)),
                new NamedValue("logicalB", FormulaValue.New(6.0)));

            var tv = TableValue.NewTable(recordType, rv1, rv2, rv3);

            var parameters = RecordValue.NewRecordFromFields(
                new NamedValue("myTable", tv));

            var result = engine.Eval("DropColumns(myTable, displayName)", parameters);

            Assert.Equal("*[logicalB`displayName2:n]", result.Type.ToStringWithDisplayNames());

            var output = result.ToExpression();

            Assert.Equal("Table({logicalB:Float(4)},{logicalB:Float(5)},{logicalB:Float(6)})", output);

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
            var pfxConfig = new PowerFxConfig(new Features { SupportColumnNamesAsIdentifiers = true });
            var engine = new RecalcEngine(pfxConfig);

            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Number, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Number, displayName: "displayName2"));

            var rv1 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(1.0)),
                new NamedValue("logicalB", FormulaValue.New(4.0)));
            var rv2 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(2.0)),
                new NamedValue("logicalB", FormulaValue.New(5.0)));
            var rv3 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(3.0)),
                new NamedValue("logicalB", FormulaValue.New(6.0)));

            var tv = TableValue.NewTable(recordType, rv1, rv2, rv3);
            var parameters = RecordValue.NewRecordFromFields(new NamedValue("myTable", tv));
            var result = engine.Eval("AddColumns(myTable, newColumn, displayName * logicalB)", parameters);

            Assert.Equal("*[logicalA`displayName:n, logicalB`displayName2:n, newColumn:n]", result.Type.ToStringWithDisplayNames());

            var output = result.ToExpression();

            Assert.Equal("Table({logicalA:Float(1),logicalB:Float(4),newColumn:Float(4)},{logicalA:Float(2),logicalB:Float(5),newColumn:Float(10)},{logicalA:Float(3),logicalB:Float(6),newColumn:Float(18)})", output);

            var displayExpression = engine.GetDisplayExpression("AddColumns(myTable, newColumn, displayName * logicalB)", parameters.Type);
            var invariantExpression = engine.GetInvariantExpression("AddColumns(myTable, newColumn, displayName * logicalB)", parameters.Type);

            Assert.Equal("AddColumns(myTable, newColumn, displayName * displayName2)", displayExpression);
            Assert.Equal("AddColumns(myTable, newColumn, logicalA * logicalB)", invariantExpression);

            var resultD = engine.Eval(displayExpression, parameters);
            var resultI = engine.Eval(invariantExpression, parameters);

            Assert.Equal("*[logicalA`displayName:n, logicalB`displayName2:n, newColumn:n]", resultD.Type.ToStringWithDisplayNames());
            Assert.Equal("*[logicalA`displayName:n, logicalB`displayName2:n, newColumn:n]", resultI.Type.ToStringWithDisplayNames());
        }

        [Fact]
        public void DisplayNameTest_DropColumns_Decimal()
        {
            var pfxConfig = new PowerFxConfig(new Features { SupportColumnNamesAsIdentifiers = true });
            var engine = new RecalcEngine(pfxConfig);

            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Decimal, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Decimal, displayName: "displayName2"));

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

            Assert.Equal("*[logicalB`displayName2:w]", result.Type.ToStringWithDisplayNames());

            var output = result.ToExpression();

            Assert.Equal("Table({logicalB:Decimal(4)},{logicalB:Decimal(5)},{logicalB:Decimal(6)})", output);

            var displayExpression = engine.GetDisplayExpression("DropColumns(myTable, displayName)", parameters.Type);
            var invariantExpression = engine.GetInvariantExpression("DropColumns(myTable, displayName)", parameters.Type);

            Assert.Equal("DropColumns(myTable, displayName)", displayExpression);
            Assert.Equal("DropColumns(myTable, logicalA)", invariantExpression);

            var resultD = engine.Eval(displayExpression, parameters);
            var resultI = engine.Eval(invariantExpression, parameters);

            Assert.Equal("*[logicalB`displayName2:w]", resultD.Type.ToStringWithDisplayNames());
            Assert.Equal("*[logicalB`displayName2:w]", resultI.Type.ToStringWithDisplayNames());
        }

        [Fact]
        public void DisplayNameTest_AddColumns_Decimal()
        {
            var pfxConfig = new PowerFxConfig(new Features { SupportColumnNamesAsIdentifiers = true });
            var engine = new RecalcEngine(pfxConfig);

            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Decimal, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Decimal, displayName: "displayName2"));

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

            Assert.Equal("*[logicalA`displayName:w, logicalB`displayName2:w, newColumn:w]", result.Type.ToStringWithDisplayNames());

            var output = result.ToExpression();

            Assert.Equal("Table({logicalA:Decimal(1),logicalB:Decimal(4),newColumn:Decimal(4)},{logicalA:Decimal(2),logicalB:Decimal(5),newColumn:Decimal(10)},{logicalA:Decimal(3),logicalB:Decimal(6),newColumn:Decimal(18)})", output);

            var displayExpression = engine.GetDisplayExpression("AddColumns(myTable, newColumn, displayName * logicalB)", parameters.Type);
            var invariantExpression = engine.GetInvariantExpression("AddColumns(myTable, newColumn, displayName * logicalB)", parameters.Type);

            Assert.Equal("AddColumns(myTable, newColumn, displayName * displayName2)", displayExpression);
            Assert.Equal("AddColumns(myTable, newColumn, logicalA * logicalB)", invariantExpression);

            var resultD = engine.Eval(displayExpression, parameters);
            var resultI = engine.Eval(invariantExpression, parameters);

            Assert.Equal("*[logicalA`displayName:w, logicalB`displayName2:w, newColumn:w]", resultD.Type.ToStringWithDisplayNames());
            Assert.Equal("*[logicalA`displayName:w, logicalB`displayName2:w, newColumn:w]", resultI.Type.ToStringWithDisplayNames());
        }

        // ThisGroup cannt be used either as a column name or as a display name.
        [Theory]        
        [InlineData("Summarize(t1, logicalC, Sum(ThisGroup, logicalA) As Soma)")]
        [InlineData("Summarize(t1, 'ThisGroup', Sum(ThisGroup, logicalC) As Soma)")]
        public void SummarizeDisplayNamesTest(string expression)
        {
            var engine = new RecalcEngine();

            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Decimal, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Decimal, displayName: "displayName2"))
                .Add(new NamedFormulaType("logicalC", FormulaType.Decimal, displayName: "ThisGroup"));

            var rv1 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(1)),
                new NamedValue("logicalB", FormulaValue.New(4)),
                new NamedValue("logicalC", FormulaValue.New(4)));
            var rv2 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(2)),
                new NamedValue("logicalB", FormulaValue.New(5)),
                new NamedValue("logicalC", FormulaValue.New(5)));
            var rv3 = RecordValue.NewRecordFromFields(
                new NamedValue("logicalA", FormulaValue.New(3)),
                new NamedValue("logicalB", FormulaValue.New(6)),
                new NamedValue("logicalC", FormulaValue.New(6)));

            engine.UpdateVariable("t1", TableValue.NewTable(recordType, rv1, rv2, rv3));

            var check = engine.Check(expression);
            Assert.False(check.IsSuccess);
        }
    }
}
