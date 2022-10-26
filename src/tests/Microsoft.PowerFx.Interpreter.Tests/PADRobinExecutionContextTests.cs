// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Data;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Interpreter.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class PADRobinExecutionContextTests
    {
        // Eval against RobinExecutionContext
        [Fact]
        public void EvalAgainstRobinExecutionContextTest()
        {
            var engine = new RecalcEngine();

            using var table = CreateDataTable();

            var robinTable = new DataTableValue(table);
            var c = new RobinExecutionContext(robinTable);
            var result1 = engine.Eval("Index(robintable, 2).Scores", c); // 20
            Assert.Equal(20.0, result1.ToObject());
            Assert.Equal(1, robinTable.TryGetIndexNumberOfCalls);
            Assert.Equal(0, robinTable.MarshalNumberOfCalls);
        }

        // Eval against UpdateVariable
        [Fact]
        public void EvalAgainstUpdateVariableTest()
        {
            var engine = new RecalcEngine();

            using var table = CreateDataTable();

            var robinTable = new DataTableValue(table);
            engine.UpdateVariable("robintable", robinTable);

            var result1 = engine.Eval("Index(robintable, 2).Scores"); // 20
            Assert.Equal(20.0, result1.ToObject());
            Assert.Equal(1, robinTable.TryGetIndexNumberOfCalls);
            Assert.Equal(0, robinTable.MarshalNumberOfCalls);
        }

        // Create table with strong typing
        private DataTable CreateDataTable()
        {
            var table = new DataTable();

            table.Columns.Add("Scores", typeof(int));
            table.Columns.Add("Names", typeof(string));

            table.Rows.Add(10, "name1");
            table.Rows.Add(20, "name2");
            table.Rows.Add(30, "name3");

            return table;
        }

        private class RobinExecutionContext : RecordValue
        {
            private readonly FormulaValue _value;

            public RobinExecutionContext(FormulaValue value)
                : base(RecordType.Empty().Add("robintable", value.Type))
            {
                _value = value;
            }

            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                result = _value;

                return true;
            }
        }
    }
}
