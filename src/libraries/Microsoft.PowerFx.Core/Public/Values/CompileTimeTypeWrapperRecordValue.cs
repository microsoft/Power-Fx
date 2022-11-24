// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    // Project the correct compile-time type onto the runtime value. 
    // Important for union / intersection types, such as Table() or If(). For example:
    //    First(Table({a:1},{b:2})), result is a record with both fields a and b. 
    internal class CompileTimeTypeWrapperRecordValue : InMemoryRecordValue
    {
        public static RecordValue AdjustType(RecordType expectedType, RecordValue inner)
        {
            if (expectedType.Equals(inner.Type))
            {
                return inner;
            }

            return new CompileTimeTypeWrapperRecordValue(expectedType, inner);
        }

        private CompileTimeTypeWrapperRecordValue(RecordType type, RecordValue inner)
            : base(IRContext.NotInSource(type), inner.Fields)
        {
        }
    }
}
