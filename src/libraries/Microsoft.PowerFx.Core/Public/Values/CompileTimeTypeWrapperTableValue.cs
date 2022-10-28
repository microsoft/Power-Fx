// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Types
{
    // Project the correct compile-time type onto the runtime value. 
    // Important for union / intersection types, such as Table() or If(). For example:
    //    First(Table({a:1},{b:2})), result is a record with both fields a and b. 
    internal class CompileTimeTypeWrapperTableValue : CollectionTableValue<DValue<RecordValue>>
    {
        public static TableValue AdjustType(TableType expectedType, TableValue inner)
        {
            if (expectedType.Equals(inner.Type))
            {
                return inner;
            }

            return new CompileTimeTypeWrapperTableValue(expectedType, inner);
        }

        private CompileTimeTypeWrapperTableValue(TableType type, TableValue inner)
            : base(type.ToRecord(), inner.Rows)
        {
        }

        protected override DValue<RecordValue> Marshal(DValue<RecordValue> record)
        {
            if (record.IsValue)
            {
                var compileTimeType = RecordType;
                var record2 = CompileTimeTypeWrapperRecordValue.AdjustType(compileTimeType, record.Value);
                return DValue<RecordValue>.Of(record2);
            }
            else
            {
                return record;
            }
        }
    }
}
