﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Data;
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
            var robinScope = new RobinExecutionContext(robinTable);
            var result1 = engine.Eval("Index(robintable, 2).Scores", robinScope); // 20
            Assert.Equal(20m, result1.ToObject());
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
            engine._symbolValues.Add("robintable", robinTable);

            var result1 = engine.Eval("Index(robintable, 2).Scores"); // 20
            Assert.Equal(20m, result1.ToObject());
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
