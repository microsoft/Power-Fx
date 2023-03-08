// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Interpreter.Tests.PadTests;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class PadTests
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

            PadUntypedObject uo = new PadUntypedObject() { DataTable = dt };
            UntypedObjectValue uov = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), uo);

            PowerFxConfig config = new PowerFxConfig(new CultureInfo("en-US"), Features.All);
            config.AddFunction(new PadIndex_Indexer());
            config.AddFunction(new PadIndex_PropertyAccessor());

            RecalcEngine engine = new RecalcEngine(config);

            engine.UpdateVariable("padTable", uov);

            FormulaValue fv1 = engine.Eval(@"Value(PadIndex(PadIndex(padTable, 1), 1))");
            Assert.Equal(1d, fv1.ToObject());

            FormulaValue fv2 = engine.Eval(@"Text(PadIndex(PadIndex(padTable, 2), ""Column1""))");
            Assert.Equal("data3", fv2.ToObject());
        }

        internal class PadIndex_Indexer : BuiltinFunction, IAsyncTexlFunction
        {
            public override bool IsSelfContained => true;

            public override bool SupportsParamCoercion => false;

            public PadIndex_Indexer()
                : base("PadIndex", (string locale) => "PAD indexer function", FunctionCategories.Table, DType.UntypedObject, 0, 2, 2, DType.UntypedObject, DType.Number)
            {
            }

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield return new[] { (TexlStrings.StringGetter)((string locale) => "index") };
            }

            public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
            {
                var arg0 = (UntypedObjectValue)args[0];
                var arg1 = (NumberValue)args[1];

                var element = arg0.Impl;

                var len = element.GetArrayLength();
                var index1 = (int)arg1.Value;
                var index0 = index1 - 1; // 1-based index

                // Error pipeline already caught cases of too low. 
                if (index0 < len)
                {
                    var result = element[index0];

                    // Map null to blank
                    if (result == null || result.Type == FormulaType.Blank)
                    {
                        return Task.FromResult((FormulaValue)new BlankValue(IRContext.NotInSource(FormulaType.Blank)));
                    }

                    return Task.FromResult((FormulaValue)new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), result));
                }
                else
                {
                    return Task.FromResult((FormulaValue)CommonErrors.ArgumentOutOfRange(IRContext.NotInSource(FormulaType.UntypedObject)));
                }
            }
        }

        internal class PadIndex_PropertyAccessor : BuiltinFunction, IAsyncTexlFunction
        {
            public override bool IsSelfContained => true;

            public override bool SupportsParamCoercion => false;

            public PadIndex_PropertyAccessor()
                : base("PadIndex", (string locale) => "PAD property accessor function", FunctionCategories.Table, DType.UntypedObject, 0, 2, 2, DType.UntypedObject, DType.String)
            {
            }

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield return new[] { (TexlStrings.StringGetter)((string locale) => "property") };
            }

            public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
            {
                var arg0 = (UntypedObjectValue)args[0];
                var arg1 = (StringValue)args[1];
                
                var columnName = (string)arg1.Value;                

                if (arg0.Impl.TryGetProperty(columnName, out var result))
                {                    
                    if (result == null || result.Type == FormulaType.Blank)
                    {
                        return Task.FromResult((FormulaValue)new BlankValue(IRContext.NotInSource(FormulaType.Blank)));
                    }

                    return Task.FromResult((FormulaValue)new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), result));
                }
                else
                {
                    return Task.FromResult((FormulaValue)CommonErrors.ArgumentOutOfRange(IRContext.NotInSource(FormulaType.UntypedObject)));
                }
            }
        }

        public class PadUntypedObject : IUntypedObject
        {
            public DataTable DataTable;
            public DataRow CurrentRow = null;
            public bool IsTable = true;

            public IUntypedObject this[int index]
            {
                get
                {
                    if (this.IsTable)
                    {
                        PadUntypedObject rowUO = new PadUntypedObject()
                        {
                            DataTable = this.DataTable,
                            CurrentRow = DataTable.Rows[index],
                            IsTable = false
                        };

                        return rowUO;
                    }

                    var row = CurrentRow[index];
                    if (row is int i)
                    {
                        return new Int_UO(i);
                    }

                    return new String_UO(row.ToString());
                }
            }

            public FormulaType Type => ExternalType.ArrayType;

            public int GetArrayLength()
            {
                return 2;
            }

            public bool GetBoolean()
            {
                throw new NotImplementedException();
            }

            public double GetDouble()
            {
                throw new NotImplementedException();
            }

            public string[] GetPropertyNames()
            {
                throw new NotImplementedException();
            }

            public string GetString()
            {
                throw new NotImplementedException();
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                if (IsTable)
                {
                    throw new NotImplementedException();
                }

                string[] columns = DataTable.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName).ToArray();
                int idx = columns.FindIndex(c => string.Equals(c, value, StringComparison.OrdinalIgnoreCase));

                if (idx == -1)
                {
                    result = null;
                    return false;
                }

                var row = CurrentRow[idx];
                if (row is int i)
                {
                    result = new Int_UO(i);
                }
                else
                {
                    result = new String_UO(row.ToString());
                }

                return true;
            }
        }

        public class Int_UO : IUntypedObject
        {
            public double Value;

            public Int_UO(int i)
            {
                Value = i;
            }

            public IUntypedObject this[int index] => throw new NotImplementedException();

            public FormulaType Type => FormulaType.Number;

            public int GetArrayLength()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean()
            {
                throw new NotImplementedException();
            }

            public double GetDouble()
            {
                return Value;
            }

            public string[] GetPropertyNames()
            {
                throw new NotImplementedException();
            }

            public string GetString()
            {
                throw new NotImplementedException();
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                throw new NotImplementedException();
            }
        }

        public class String_UO : IUntypedObject
        {
            public string Value;

            public String_UO(string str)
            {
                Value = str;
            }

            public IUntypedObject this[int index] => throw new NotImplementedException();

            public FormulaType Type => FormulaType.String;

            public int GetArrayLength()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean()
            {
                throw new NotImplementedException();
            }

            public double GetDouble()
            {
                throw new NotImplementedException();
            }

            public string[] GetPropertyNames()
            {
                throw new NotImplementedException();
            }

            public string GetString()
            {
                return Value;
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                throw new NotImplementedException();
            }
        }
    }
}
