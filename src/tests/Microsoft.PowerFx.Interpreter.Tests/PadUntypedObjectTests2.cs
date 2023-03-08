// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class PadUntypedObjectTests2
    {
        public class PadTests2
        {
            [Fact]
            public void PadUntypedObjectTest2()
            {
                DataTable dt = new DataTable("someTable");
                dt.Columns.Add("Id", typeof(int));
                dt.Columns.Add("Column1", typeof(string));
                dt.Columns.Add("Column2", typeof(string));
                dt.Rows.Add(1, "data1", "data2");
                dt.Rows.Add(2, "data3", "data4");

                PadUntypedObject uo = new PadUntypedObject(dt);
                UntypedObjectValue uov = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uo);

                PowerFxConfig config = new PowerFxConfig(new CultureInfo("en-US"), Features.All);
                RecalcEngine engine = new RecalcEngine(config);

                engine.UpdateVariable("padTable", uov);

                FormulaValue fv1 = engine.Eval(@"Value(Index(Index(padTable, 1), 1))");
                Assert.Equal(1d, fv1.ToObject());

                FormulaValue fv2 = engine.Eval(@"Text(Index(padTable, 2).Column1)");
                Assert.Equal("data3", fv2.ToObject());
            }

            public class PadUntypedObject : UntypedObjectBase
            {
                public DataTable DataTable;
                public DataRow DataRow;
                public object Cell;

                public PadUntypedObject(DataTable dt)
                    : base(UntypedObjectCapabilities.SupportsArray)
                {
                    DataTable = dt;
                    DataRow = null;
                    Cell = null;
                }

                public PadUntypedObject(DataRow dr)
                    : base(UntypedObjectCapabilities.SupportsArray | UntypedObjectCapabilities.SupportsProperties)
                {
                    DataTable = null;
                    DataRow = dr;
                    Cell = null;
                }

                public PadUntypedObject(object obj)
                    : base(GetCapabilities(obj))
                {
                    DataTable = null;
                    DataRow = null;
                    Cell = obj;
                }

                public override bool IsBlank()
                {
                    return DataTable == null && DataRow == null && Cell == null;
                }

                private static UntypedObjectCapabilities GetCapabilities(object obj)
                {
                    return obj switch
                    {
                        int => UntypedObjectCapabilities.SupportsDouble,
                        double => UntypedObjectCapabilities.SupportsDouble,
                        string => UntypedObjectCapabilities.SupportsString,
                        bool => UntypedObjectCapabilities.SupportsBoolean,
                        _ => throw new NotImplementedException($"Unknown type {obj.GetType().Name}")
                    };
                }

                public override FormulaType Type =>
                    DataTable == null && DataRow == null
                        ? Cell switch
                        {
                            int => FormulaType.Number,
                            double => FormulaType.Number,
                            string => FormulaType.String,
                            bool => FormulaType.Boolean,
                            _ => throw new NotImplementedException()
                        }
                        : throw new NotImplementedException();

                public override int ArrayLength()
                {
                    return DataTable != null ? DataTable.Rows.Count
                           : DataRow != null ? DataRow.Table.Columns.Count
                           : throw new NotImplementedException();
                }

                public override bool AsBoolean()
                {
                    return (bool)Cell;
                }

                public override double AsDouble()
                {
                    return Cell switch
                    {
                        double => (double)Cell,
                        int => (double)(int)Cell,
                        _ => (double)Cell
                    };
                }

                public override string AsString()
                {
                    return (string)Cell;
                }

                public override UntypedObjectBase GetProperty(string propertyName)
                {
                    return new PadUntypedObject(DataRow[propertyName]);
                }

                public override UntypedObjectBase IndexOf(int index)
                {
                    return DataTable != null
                        ? new PadUntypedObject(DataTable.Rows[index])
                        : new PadUntypedObject(DataRow[index]);
                }

                public override string[] PropertyNames()
                {
                    return DataRow.Table.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName).ToArray();
                }
            }
        }
    }
}
