// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Data;
using Microsoft.PowerFx.Interpreter.Tests;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Tests
{
    // Marshal DataTable as a Table of Records, where each cell is UntypedObject.
    internal class DataTableMarshallerProvider : IDynamicTypeMarshaller
    {
        public bool TryMarshal(TypeMarshallerCache cache, object value, out FormulaValue result)
        {
            if (value is DataTable dataTable)
            {
                result = new DataTableValue(dataTable);
                return true;
            }

            result = null;
            return false;
        }

        // Wrap a System.Data.DataTable as a Power Fx TableValue
        private class DataTableValue : CollectionTableValue<DataRow>
        {
            public DataTableValue(DataTable dataTable)
                : base(ComputeType(dataTable), new DataTableWrapper(dataTable))
            {
            }

            // Type is a record matching the columnNames, and each field is type is IUntypedObject.
            // $$$ USe Columns.DataType to have stronger schema?
            public static RecordType ComputeType(DataTable dataTable)
            {
                var recordType = new RecordType();
                foreach (DataColumn column in dataTable.Columns)
                {
                    recordType = recordType.Add(column.ColumnName, FormulaType.UntypedObject);
                }

                return recordType;
            }

            protected override DValue<RecordValue> Marshal(DataRow item)
            {
                var record = new DataRowRecordValue(RecordType, item);

                return DValue<RecordValue>.Of(record);
            }
        }

        // Wrap an individual DataRow of the DataTable as a Power Fx RecordValue
        private class DataRowRecordValue : RecordValue
        {
            private readonly DataRow _row;

            public DataRowRecordValue(RecordType type, DataRow row)
                : base(type)
            {
                _row = row;
            }

            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                var value = _row[fieldName];
                result = WrapDotNetObjectAsUntypedValue(value);
                return true;
            }

            private static UntypedObjectValue WrapDotNetObjectAsUntypedValue(object item)
            {
                // Would be nice if this was easier...
                UntypedObjectValue objFx = FormulaValue.New(new ScenarioDotNetObjectWrapper.Wrapper(item));
                return objFx;
            }
        }

        // This class shouldn't be necessary, but DataTable is legacy and doesn't implement generic interfaces. 
        // Wrap and expose the generic interfaces that it ought to be implementing. 
        private class DataTableWrapper : IReadOnlyList<DataRow>
        {
            private readonly DataTable _dataTable;

            public int Count => _dataTable.Rows.Count;

            public DataRow this[int index] => _dataTable.Rows[index];

            public DataTableWrapper(DataTable dataTable)
            {
                _dataTable = dataTable;
            }

            public IEnumerator<DataRow> GetEnumerator()
            {
                // DataTable is legacy and doesn't implement generic interfaces. 
                foreach (DataRow row in _dataTable.Rows)
                {
                    yield return row;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _dataTable.Rows.GetEnumerator();
            }
        }
    }
}
