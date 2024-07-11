// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.PowerFx.Core.IR;
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

            PadUntypedObject uo = new PadUntypedObject(dt);
            UntypedObjectValue uov = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uo);

            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            engine.UpdateVariable("padTable", uov);

            FormulaValue fv1 = engine.Eval(@"Float(Index(Index(padTable, 1), 1))");
            Assert.Equal(1d, fv1.ToObject());

            FormulaValue fv2 = engine.Eval(@"Text(Index(padTable, 2).Column1)");
            Assert.Equal("data3", fv2.ToObject());

            FormulaValue fv3 = engine.Eval(@"Index(padTable, 2).Column7"); // invalid column
            Assert.Equal(FormulaType.UntypedObject, fv3.Type);
            Assert.True(fv3 is BlankValue);
        }

        [Fact]
        public void PadUntypedObjectMutationTest()
        {
            DataTable dt = new DataTable("someTable");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Column1", typeof(string));
            dt.Columns.Add("Column2", typeof(string));
            dt.Rows.Add(1, "data1", "data2");
            dt.Rows.Add(2, "data3", "data4");

            PadUntypedObject uo = new PadUntypedObject(dt);
            PadUntypedObject uoCell = new PadUntypedObject(99);

            PadUntypedObjectValue uov = new PadUntypedObjectValue(uo);
            PadUntypedObjectValue uovCell = new PadUntypedObjectValue(uoCell);

            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("padTable", uov, new SymbolProperties() { CanMutate = true, CanSetMutate = true });
            engine.UpdateVariable("padCell", uovCell);

            // Setting an untyped object (padCell)
            DecimalValue result = (DecimalValue)engine.Eval(@"Set(Index(padTable, 1).Id, padCell);Index(padTable, 1).Id+1", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.Equal(100m, result.ToObject());

            // Setting a strongly typed object (99)
            result = (DecimalValue)engine.Eval(@"Set(Index(padTable, 1).Id, 99);Index(padTable, 1).Id+1", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.Equal(100m, result.ToObject());
        }
    }

    public class PadUntypedObjectValue : UntypedObjectValue
    {
        private readonly PadUntypedObject _padImpl;

        public PadUntypedObjectValue(PadUntypedObject impl)
            : base(IRContext.NotInSource(FormulaType.UntypedObject), impl)
        {
            _padImpl = impl;
        }

        internal override UntypedObjectValue CreateNew(IRContext irContext, IUntypedObject impl)
        {
            return new PadUntypedObjectValue((PadUntypedObject)impl);
        }

        public override bool TrySetProperty(string propertyName, FormulaValue value)
        {
            if (_padImpl.DataTable != null)
            {
                throw new NotImplementedException();
            }

            if (!_padImpl.DataRow.Table.Columns.Contains(propertyName))
            {
                value = default;
                return false;
            }

            if (value is DecimalValue dv)
            {
                _padImpl.DataRow[propertyName] = dv.Value;
            }
            else if (value is UntypedObjectValue uov)
            {
                if (_padImpl.DataRow[propertyName].GetType() == typeof(string))
                {
                    _padImpl.DataRow[propertyName] = uov.Impl.GetString();
                }
                else if (_padImpl.DataRow[propertyName].GetType() == typeof(int))
                {
                    _padImpl.DataRow[propertyName] = uov.Impl.GetDouble();
                }
                else if (_padImpl.DataRow[propertyName].GetType() == typeof(bool))
                {
                    _padImpl.DataRow[propertyName] = uov.Impl.GetBoolean();
                }
                else if (_padImpl.DataRow[propertyName].GetType() == typeof(decimal))
                {
                    _padImpl.DataRow[propertyName] = uov.Impl.GetDecimal();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class PadUntypedObject : IUntypedObject
    {
        public DataTable DataTable;
        public DataRow DataRow;
        public object Cell;

        public PadUntypedObject(DataTable dt)
        {
            DataTable = dt;
            DataRow = null;
            Cell = null;
        }

        public PadUntypedObject(DataRow dr)
        {
            DataTable = null;
            DataRow = dr;
            Cell = null;
        }

        public PadUntypedObject(object cell)
        {
            DataTable = null;
            DataRow = null;
            Cell = cell;
        }

        private IUntypedObject Index(int index)
        {
            return (DataTable != null)
                    ? new PadUntypedObject(DataTable.Rows[index])
                    : (DataRow != null)
                    ? new PadUntypedObject(DataRow[index])
                    : throw new NotImplementedException();
        }

        public FormulaType Type => GetFormulaType();

        public IUntypedObject this[int index] => Index(index);

        private FormulaType GetFormulaType()
        {
            return (Cell != null)
                ? Cell switch
                {
                    int => FormulaType.Number,
                    double => FormulaType.Number,
                    string => FormulaType.String,
                    _ => throw new NotImplementedException()
                }

                // for Table and Row
                : ExternalType.ArrayAndObject;
        }

        public int GetArrayLength()
        {
            return (DataTable != null)
                 ? DataTable.Rows.Count
                 : (DataRow != null)
                 ? DataRow.Table.Columns.Count
                 : throw new NotImplementedException();
        }

        public bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            return Cell switch
            {
                int => (double)(int)Cell,
                double => (double)Cell,
                float => (double)(float)Cell,
                _ => throw new NotImplementedException()
            };
        }

        public decimal GetDecimal()
        {
            throw new NotImplementedException();
        }

        public string GetUntypedNumber()
        {
            throw new NotImplementedException();
        }

        public string[] GetPropertyNames()
        {
            throw new NotImplementedException();
        }

        public string GetString()
        {
            return Cell.ToString();
        }

        public bool TryGetProperty(string propertyName, out IUntypedObject result)
        {
            if (DataTable != null)
            {
                throw new NotImplementedException();
            }

            if (!DataRow.Table.Columns.Contains(propertyName))
            {
                result = default;
                return false;
            }

            var cell = DataRow[propertyName];
            result = new PadUntypedObject(cell);
            return true;
        }

        public bool TryGetPropertyNames(out IEnumerable<string> result)
        {
            result = null;
            return false;
        }

        public bool TrySetProperty(string propertyName, FormulaValue value)
        {
            if (DataTable != null)
            {
                throw new NotImplementedException();
            }

            if (!DataRow.Table.Columns.Contains(propertyName))
            {
                value = default;
                return false;
            }

            if (value is DecimalValue dv)
            {
                DataRow[propertyName] = dv.Value;
            }
            else if (value is UntypedObjectValue uov)
            {
                if (DataRow[propertyName].GetType() == typeof(string))
                {
                    DataRow[propertyName] = uov.Impl.GetString();
                }
                else if (DataRow[propertyName].GetType() == typeof(int))
                {
                    DataRow[propertyName] = uov.Impl.GetDouble();
                }
                else if (DataRow[propertyName].GetType() == typeof(bool))
                {
                    DataRow[propertyName] = uov.Impl.GetBoolean();
                }
                else if (DataRow[propertyName].GetType() == typeof(decimal))
                {
                    DataRow[propertyName] = uov.Impl.GetDecimal();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
