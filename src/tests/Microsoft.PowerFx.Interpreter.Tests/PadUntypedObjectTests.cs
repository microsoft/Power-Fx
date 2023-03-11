// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class PadUntypedObjectTests
    {
        [Fact]
        public void PadUntypedObjectTest()
        {
            DataTable dt = new DataTable("someTable");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Column1", typeof(string));
            dt.Columns.Add("Column2", typeof(string));
            dt.Rows.Add(1, "data1", "data2");
            dt.Rows.Add(2, "data3", "data4");

            PadTable uo = new PadTable(dt);
            UntypedObjectValue uov = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uo);

            PowerFxConfig config = new PowerFxConfig(new CultureInfo("en-US"), Features.All);
            RecalcEngine engine = new RecalcEngine(config);

            engine.UpdateVariable("padTable", uov);

            FormulaValue fv1 = engine.Eval(@"Value(Index(Index(padTable, 1), 1))");
            Assert.Equal(1d, fv1.ToObject());

            FormulaValue fv2 = engine.Eval(@"Text(Index(padTable, 2).Column1)");
            Assert.Equal("data3", fv2.ToObject());

            FormulaValue fv3 = engine.Eval(@"Index(padTable, 2).Column7"); // invalid column
            Assert.Equal(FormulaType.UntypedObject, fv3.Type);
            Assert.True(fv3 is BlankValue);
        }
    }

    public class PadTable : ISupportsArray
    {
        private readonly DataTable _table;

        public PadTable(DataTable dt)
        {
            _table = dt;
        }

        public IUntypedObject this[int index] => new PadRow(_table.Rows[index]);

        public int Length => _table.Rows.Count;

        public bool IsBlank()
        {
            return _table == null;
        }
    }

    public class PadRow : ISupportsArray, ISupportsProperties
    {
        private readonly DataRow _row;

        public PadRow(DataRow dataRow)
        {
            _row = dataRow;
        }

        public IUntypedObject this[int index] => new PadCell(_row[index]);

        public int Length => _row.Table.Columns.Count;

        public bool IsBlank()
        {
            return _row == null;
        }

        public bool TryGetProperty(string propertyName, out IUntypedObject result)
        {
            if (!_row.Table.Columns.Contains(propertyName))
            {
                result = default;
                return false;
            }

            object cell = _row[propertyName];
            result = new PadCell(cell);
            return true;
        }
    }

    public class PadCell : SupportsFxValue
    {
        public PadCell(object obj)
            : base(obj)
        {
        }
    }

    //public class PadUntypedObject : IUntypedObject
    //{
    //    public DataTable DataTable;
    //    public DataRow DataRow;
    //    public object Cell;

    //    public PadUntypedObject(DataTable dt)
    //    {
    //        DataTable = dt;
    //        DataRow = null;
    //        Cell = null;
    //    }

    //    public PadUntypedObject(DataRow dr)
    //    {
    //        DataTable = null;
    //        DataRow = dr;
    //        Cell = null;
    //    }

    //    public PadUntypedObject(object cell)
    //    {
    //        DataTable = null;
    //        DataRow = null;
    //        Cell = cell;
    //    }

    //    public IUntypedObject this[int index] => Index(index);

    //    private IUntypedObject Index(int index)
    //    {
    //        return (DataTable != null)
    //                ? new PadUntypedObject(DataTable.Rows[index])
    //                : (DataRow != null)
    //                ? new PadUntypedObject(DataRow[index])
    //                : throw new NotImplementedException();
    //    }

    //    public FormulaType Type => GetFormulaType();

    //    private FormulaType GetFormulaType()
    //    {
    //        return (Cell != null)
    //            ? Cell switch
    //            {
    //                int => FormulaType.Number,
    //                double => FormulaType.Number,
    //                string => FormulaType.String,
    //                _ => throw new NotImplementedException()
    //            }

    //            // for Table and Row
    //            : ExternalType.ArrayAndObject;
    //    }

    //    public int GetArrayLength()
    //    {
    //        return (DataTable != null)
    //             ? DataTable.Rows.Count
    //             : (DataRow != null)
    //             ? DataRow.Table.Columns.Count
    //             : throw new NotImplementedException();
    //    }

    //    public bool GetBoolean()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public double GetDouble()
    //    {
    //        return Cell switch
    //        {
    //            int => (double)(int)Cell,
    //            double => (double)Cell,
    //            float => (double)(float)Cell,
    //            _ => throw new NotImplementedException()
    //        };
    //    }

    //    public string[] GetPropertyNames()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public string GetString()
    //    {
    //        return Cell.ToString();
    //    }

    //    public bool TryGetProperty(string propertyName, out IUntypedObject result)
    //    {
    //        if (DataTable != null)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        if (!DataRow.Table.Columns.Contains(propertyName))
    //        {
    //            result = default;
    //            return false;
    //        }

    //        var cell = DataRow[propertyName];
    //        result = new PadUntypedObject(cell);
    //        return true;
    //    }
    //}
}
