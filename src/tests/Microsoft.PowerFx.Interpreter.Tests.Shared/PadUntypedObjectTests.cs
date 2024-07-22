// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
            NotImplementedUntypedObject notImplementedUO = new NotImplementedUntypedObject(dt);

            UntypedObjectValue uov = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uo);
            UntypedObjectValue uovCell = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uoCell);
            UntypedObjectValue notImplementedValue = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), notImplementedUO);

            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("padTable", uov, new SymbolProperties() { CanMutate = true, CanSetMutate = true });
            engine.UpdateVariable("notImplementedUO", notImplementedValue, new SymbolProperties() { CanMutate = true, CanSetMutate = true });
            engine.UpdateVariable("padCell", uovCell);

            // Setting an untyped object (padCell).
            DecimalValue result = (DecimalValue)engine.Eval(@"Set(Index(padTable, 1).Id, padCell);Index(padTable, 1).Id+1", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.Equal(100m, result.ToObject());

            // Setting a strongly typed object (99).
            result = (DecimalValue)engine.Eval(@"Set(Index(padTable, 1).Id, 99);Index(padTable, 1).Id+1", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.Equal(100m, result.ToObject());

            // Property does not exist.
            ErrorValue errorValue = (ErrorValue)engine.Eval(@"Set(Index(padTable, 1).DoesNotExist, 99)", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.IsType<ErrorValue>(errorValue);
            Assert.Equal(ErrorKind.InvalidArgument, errorValue.Errors.First().Kind);

            // Type not supported.
            errorValue = (ErrorValue)engine.Eval(@"Set(Index(padTable, 1).Column2, GUID())", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.IsType<ErrorValue>(errorValue);
            Assert.Equal(ErrorKind.InvalidArgument, errorValue.Errors.First().Kind);

            // 'SetProperty' not implemented.
            errorValue = (ErrorValue)engine.Eval(@"Set(Index(notImplementedUO, 1).Id, 1)", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.IsType<ErrorValue>(errorValue);
            Assert.Equal(ErrorKind.NotSupported, errorValue.Errors.First().Kind);
        }
      
        [Fact]
        public void PadUntypedObjectReadIndexTest()
        {
            DataTable dt = new DataTable("someTable");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Column1", typeof(string));
            dt.Columns.Add("Column2", typeof(string));
            dt.Rows.Add(1, "data1", "data2");
            dt.Rows.Add(2, "data3", "data4");

            var uoTable = new PadUntypedObject(dt);
            var uoRow = new PadUntypedObject(dt.Rows[0]); // First row

            var uovTable = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uoTable);
            var uovRow = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uoRow);

            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("padTable", uovTable);
            engine.UpdateVariable("padRow", uovRow);

            // Index over row.
            UntypedObjectValue result = (UntypedObjectValue)engine.Eval("Index(padRow, 1)");
            Assert.Equal(1d, result.Impl.GetDouble());

            result = (UntypedObjectValue)engine.Eval("Index(padRow, 2)");
            Assert.Equal("data1", result.Impl.GetString());

            var resultDecimal = (DecimalValue)engine.Eval("Index(padRow, 1) + 99");
            Assert.Equal(100, resultDecimal.Value);

            // Nested Index calls.
            result = (UntypedObjectValue)engine.Eval("Index(Index(padTable, 1), 1)");
            Assert.Equal(1d, result.Impl.GetDouble());

            result = (UntypedObjectValue)engine.Eval("Index(Index(padTable, 2), 2)");
            Assert.Equal("data3", result.Impl.GetString());
        }
      
        [Fact]
        public void PadUntypedObject2MutationTest()
        {
            DataTable dt = new DataTable("someTable");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Column1", typeof(string));
            dt.Columns.Add("Column2", typeof(string));
            dt.Rows.Add(1, "data1", "data2");
            dt.Rows.Add(2, "data3", "data4");

            var uo = new PadUntypedObject2(dt);
            var uov = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uo);

            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("padTable", uov, new SymbolProperties() { CanMutate = true, CanSetMutate = true });

            // Setting an untyped object (padCell)
            DecimalValue result = (DecimalValue)engine.Eval(@"Set(Index(Index(padTable, 1), 1), 97);Index(Index(padTable, 1), 1) + 0", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.Equal(97m, result.ToObject());
        }
    }

    #region Support classes
    public class PadUntypedObject : UntypedObjectBase
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

        public override FormulaType Type => GetFormulaType();

        public override IUntypedObject this[int index] => Index(index);

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

        public override int GetArrayLength()
        {
            return (DataTable != null)
                 ? DataTable.Rows.Count
                 : (DataRow != null)
                 ? DataRow.Table.Columns.Count
                 : throw new NotImplementedException();
        }

        public override bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public override double GetDouble()
        {
            return Cell switch
            {
                int => (double)(int)Cell,
                double => (double)Cell,
                float => (double)(float)Cell,
                _ => throw new NotImplementedException()
            };
        }

        public override decimal GetDecimal()
        {
            throw new NotImplementedException();
        }

        public override string GetUntypedNumber()
        {
            throw new NotImplementedException();
        }

        public string[] GetPropertyNames()
        {
            throw new NotImplementedException();
        }

        public override string GetString()
        {
            return Cell.ToString();
        }

        public override bool TryGetProperty(string propertyName, out IUntypedObject result)
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

        public override bool TryGetPropertyNames(out IEnumerable<string> result)
        {
            result = null;
            return false;
        }

        public override void SetProperty(string propertyName, FormulaValue value)
        {
            if (DataTable != null)
            {
                throw new NotImplementedException();
            }

            if (!DataRow.Table.Columns.Contains(propertyName))
            {
                value = default;
                throw new CustomFunctionErrorException($"Property '{propertyName}' does not exist.", ErrorKind.InvalidArgument);
            }

            if (value is DecimalValue dv)
            {
                DataRow[propertyName] = dv.Value;
            }
            else if (value is GuidValue)
            {
                throw new CustomFunctionErrorException($"Type '{value.Type.ToString()}' is not supported.", ErrorKind.InvalidArgument);
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
                    throw new CustomFunctionErrorException($"Type '{DataRow[propertyName].GetType()}' is not supported.", ErrorKind.InvalidArgument);
                }
            }
        }
    }

    public class NotImplementedUntypedObject : UntypedObjectBase
    {
        public DataTable DataTable;
        public DataRow DataRow;
        public object Cell;

        public NotImplementedUntypedObject(DataTable dt)
        {
            DataTable = dt;
            DataRow = null;
            Cell = null;
        }

        public NotImplementedUntypedObject(DataRow dr)
        {
            DataTable = null;
            DataRow = dr;
            Cell = null;
        }

        public NotImplementedUntypedObject(object cell)
        {
            DataTable = null;
            DataRow = null;
            Cell = cell;
        }

        private IUntypedObject Index(int index)
        {
            return new NotImplementedUntypedObject(DataTable.Rows[index]);
        }

        public override FormulaType Type => GetFormulaType();

        public override IUntypedObject this[int index] => Index(index);

        private FormulaType GetFormulaType()
        {
            return ExternalType.ArrayAndObject;
        }

        public override int GetArrayLength()
        {
            return 1;
        }

        public override bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public override double GetDouble()
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal()
        {
            throw new NotImplementedException();
        }

        public override string GetUntypedNumber()
        {
            throw new NotImplementedException();
        }

        public string[] GetPropertyNames()
        {
            throw new NotImplementedException();
        }

        public override string GetString()
        {
            return Cell.ToString();
        }

        public override bool TryGetProperty(string propertyName, out IUntypedObject result)
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
            result = new NotImplementedUntypedObject(cell);
            return true;
        }

        public override bool TryGetPropertyNames(out IEnumerable<string> result)
        {
            result = null;
            return false;
        }
    }

    public class PadUntypedObject2 : UntypedObjectBase
    {
        public DataTable DataTable;
        public DataRow DataRow;
        private readonly int _index;

        public PadUntypedObject2(DataTable dt)
        {
            DataTable = dt;
            DataRow = null;
            _index = -1;
        }

        public PadUntypedObject2(DataRow dr)
        {
            DataTable = null;
            DataRow = dr;
            _index = -1;
        }

        public PadUntypedObject2(DataRow dr, int index)
        {
            DataTable = null;
            DataRow = dr;
            _index = index;
        }

        private IUntypedObject Index(int index)
        {
            return (DataTable != null)
                    ? new PadUntypedObject2(DataTable.Rows[index])
                    : (DataRow != null)
                    ? new PadUntypedObject2(DataRow, index)
                    : throw new NotImplementedException();
        }

        public override FormulaType Type => GetFormulaType();

        public override IUntypedObject this[int index] => Index(index);

        private FormulaType GetFormulaType()
        {
            return (_index > -1)
                ? DataRow[_index] switch
                {
                    int => FormulaType.Number,
                    double => FormulaType.Number,
                    string => FormulaType.String,
                    _ => throw new NotImplementedException()
                }

                // for Table and Row
                : ExternalType.ArrayAndObject;
        }

        public override int GetArrayLength()
        {
            return (DataTable != null)
                 ? DataTable.Rows.Count
                 : (DataRow != null)
                 ? DataRow.Table.Columns.Count
                 : throw new NotImplementedException();
        }

        public override bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public override double GetDouble()
        {
            return DataRow[_index] switch
            {
                int => (double)(int)DataRow[_index],
                double => (double)DataRow[_index],
                float => (double)(float)DataRow[_index],
                _ => throw new NotImplementedException()
            };
        }

        public override decimal GetDecimal()
        {
            throw new NotImplementedException();
        }

        public override string GetUntypedNumber()
        {
            throw new NotImplementedException();
        }

        public string[] GetPropertyNames()
        {
            throw new NotImplementedException();
        }

        public override string GetString()
        {
            return DataRow[_index].ToString();
        }

        public override bool TryGetProperty(string propertyName, out IUntypedObject result)
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

            var index = DataRow.Table.Columns.IndexOf(propertyName);

            var cell = DataRow[propertyName];
            result = new PadUntypedObject2(DataRow, index);
            return true;
        }

        public override bool TryGetPropertyNames(out IEnumerable<string> result)
        {
            result = null;
            return false;
        }

        public override void SetProperty(int index, FormulaValue value)
        {
            index = index - 1; // 0-based index

            if (value is DecimalValue dv)
            {
                DataRow[index] = dv.Value;
                return;
            }

            throw new CustomFunctionErrorException("Something went wrong.", ErrorKind.InvalidArgument);
        }
    }
    #endregion
}
