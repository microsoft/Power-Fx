// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Types
{
    // Project the correct compile-time type onto the runtime value. 
    // Important for union / intersection types, such as Table() or If(). For example:
    //    First(Table({a:1},{b:2})), result is a record with both fields a and b. 
    internal class CompileTimeTypeWrapperRecordValue : RecordValue
    {
        private readonly RecordValue _inner;

        public static RecordValue AdjustType(BaseRecordType expectedType, RecordValue inner)
        {
            if (expectedType.Equals(inner.Type))
            {
                return inner;
            }

            return new CompileTimeTypeWrapperRecordValue(expectedType, inner);
        }

        private CompileTimeTypeWrapperRecordValue(BaseRecordType type, RecordValue inner)
            : base(type)
        {
            _inner = inner;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            // If the runtime value is missing a field of the given type, it will be Blank().
            result = _inner.GetField(fieldType, fieldName);
            return true;
        }

        public override object ToObject()
        {
            // Unwrap as inner object. Especially important when host is passing
            // in a custom object that it needs to retrieve.
            return _inner.ToObject();
        }
    }
}
