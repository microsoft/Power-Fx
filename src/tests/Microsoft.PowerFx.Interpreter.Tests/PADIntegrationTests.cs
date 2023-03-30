// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Data;
using Microsoft.PowerFx.Interpreter.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class TableMarshalTypeTests
    {
        [Fact]
        public void DataTableCheckTest()
        {
            var engine = new RecalcEngine();

            // FormulaType
            // Is this a valid usage of the UnknownType?
            var fType = TableType.Empty()
                .Add(new NamedFormulaType("Column1", FormulaType.Unknown))
                .Add(new NamedFormulaType("Column2", FormulaType.Unknown))
                .Add(new NamedFormulaType("Column3", FormulaType.Unknown));

            var executionScope = RecordType.Empty()
                .Add("RobinTable", fType);

            var validationResult = engine.Check("Index(RobinTable, 1).Column1", executionScope);
            Assert.Equal(FormulaType.Unknown, validationResult.ReturnType);
        }

        // Table of heterogenous cells. 
        private DataTable CreateObjectDataTable()
        {
            var table = new DataTable();

            table.Columns.Add(string.Empty, typeof(object));
            table.Columns.Add(string.Empty, typeof(object));
            table.Columns.Add(string.Empty, typeof(object));

            var row1 = new object[] { 101, "str1b", true };
            var row2 = new object[] { 201, "str202", "str203" };
            var row3 = new object[] { 301, 302, 303 };

            table.Rows.Add(row1);
            table.Rows.Add(row2);
            table.Rows.Add(row3);

            return table;
        }

        [Fact]
        public void DataTableEvalTest()
        {
            var engine = new RecalcEngine();

            using var table = CreateObjectDataTable();

            var cache = new TypeMarshallerCache()
                .WithDynamicMarshallers(new DataTableMarshallerProvider());

            var robinTable = cache.Marshal(table);

            engine.UpdateVariable("robintable", robinTable);

            var opts = new ParserOptions() { NumberIsFloat = true };

            var result1 = engine.Eval("Value(Index(robintable, 1).Column1)", options: opts); // 101
            var result2 = engine.Eval("Text(Index(robintable, 2).Column2)", options: opts); // "str202"

            Assert.Equal(101d, result1.ToObject());
            Assert.Equal("str202", result2.ToObject());

            var result3 = engine.Eval("Sum(robintable, Value(ThisRecord.Column1))");
            Assert.Equal(101d + 201 + 301, result3.ToObject());
        }

        // Create table with strong typing
        private DataTable CreateDataTable()
        {
            var table = new DataTable();

            table.Columns.Add("Scores", typeof(double));
            table.Columns.Add("Names", typeof(string));

            table.Rows.Add(10, "name1");
            table.Rows.Add(20, "name2");
            table.Rows.Add(30, "name3");

            return table;
        }

        // Eval against a strongly typed data table 
        // Strong typing means we don't need extra Value()/Text() functions. 
        [Fact]
        public void DataTableEvalTest2()
        {
            var engine = new RecalcEngine();

            using var table = CreateDataTable();

            var cache = new TypeMarshallerCache()
                .WithDynamicMarshallers(new DataTableMarshallerProvider());

            var robinTable = cache.Marshal(table);

            engine.UpdateVariable("robintable", robinTable);

            var opts = new ParserOptions() { NumberIsFloat = true };

            var result1 = engine.Eval("Index(robintable, 2).Scores", options: opts); // 20
            var result2 = engine.Eval("Index(robintable, 3).Names", options: opts); // "name3"

            Assert.Equal(20d, result1.ToObject());
            Assert.Equal("name3", result2.ToObject());

            var result3 = engine.Eval("Sum(robintable, ThisRecord.Scores)");
            Assert.Equal(60d, result3.ToObject());

            // Access field not on the table 
            var result4 = engine.Eval(
                @"
                    First(
                        Table(
                            First(robintable), 
                            { Other : 5}
                         )).Other",
                options: opts);

            Assert.IsType<BlankValue>(result4);

            var symbol = new SymbolTable();
            var opt = new ParserOptions() { AllowsSideEffects = true, NumberIsFloat = true };

            symbol.EnableMutationFunctions();

            engine.Config.SymbolTable = symbol;

            Assert.Equal(3, table.Rows.Count);

            var result5 = engine.Eval("Remove(robintable, {Names:\"name2\"});robintable", options: opt);
            Assert.Equal("Table({Names:\"name1\",Scores:10},{Names:\"name3\",Scores:30})", ((DataTableValue)result5).Dump());

            // Is table object affected?
            Assert.Equal(2, table.Rows.Count);

            var result6 = engine.Eval("Collect(robintable, {Scores:10,Names:\"name100\"});robintable", options: opt);
            Assert.Equal("Table({Names:\"name1\",Scores:10},{Names:\"name3\",Scores:30},{Names:\"name100\",Scores:10})", ((DataTableValue)result6).Dump());

            var result7 = engine.Eval("Patch(robintable, First(robintable),{Names:\"new-name\"});robintable", options: opt);
            Assert.Equal("Table({Names:\"new-name\",Scores:10},{Names:\"name3\",Scores:30},{Names:\"name100\",Scores:10})", ((DataTableValue)result7).Dump());

            var result8 = engine.Eval("Remove(robintable, {Scores:10}, \"All\");robintable", options: opt);
            Assert.Equal("Table({Names:\"name3\",Scores:30})", ((DataTableValue)result8).Dump());

            Assert.Equal(1, table.Rows.Count);
        }

        [Fact]
        public void ListCheckTest()
        {
            var engine = new RecalcEngine();

            //FormulaType
            // Is this a valid representation of a List<object> type to a FormulaType?
            var fType = TableType.Empty()
                .Add(new NamedFormulaType("Value", FormulaType.Unknown));

            var executionScope = RecordType.Empty()
                .Add("RobinList", fType);

            var validationResult = engine.Check("Index(RobinList, 1).Value", executionScope);

            Assert.Equal(FormulaType.Unknown, validationResult.ReturnType);
        }

        [Fact]
        public void ListHeterogenousArray()
        {
            var engine = new RecalcEngine();

            // Marshalling will fail.
            var list = new List<object> { 1, "string", true };

            var cache = new TypeMarshallerCache()
                .NewPrepend(new ObjectListMarshallerProvider());

            var robinList = cache.Marshal(list);

            engine.UpdateVariable("robinList", robinList);

            var result1 = engine.Eval("Value(Index(robinList, 1).Value)");
            var result2 = engine.Eval("Text(Index(robinList, 2).Value)");

            Assert.Equal(1m, result1.ToObject());
            Assert.Equal("string", result2.ToObject());
        }
    }
}
