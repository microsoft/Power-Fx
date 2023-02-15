// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            if (Type.TryGetFieldType(fieldName, out _))
            {
                // Only return field which were specified via the expectedType (IE RecordType),
                // because inner record value may have more fields than the expected type.
                return _fields.TryGetValue(fieldName, out result);
            }

            result = default;
            return false;
        }

        public override async Task<DValue<RecordValue>> UpdateFieldsAsync(RecordValue changeRecord, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_mutableFields == null)
            {
                return await base.UpdateFieldsAsync(changeRecord, cancellationToken);
            }

            await UpdateExistingFields(changeRecord, cancellationToken);

            var fields = new List<NamedValue>();

            foreach (var kvp in _fields)
            {
                // Only add fields which were specified via the expectedType (IE RecordType),
                // because inner record value may have more fields than the expected type.
                if (Type.TryGetFieldType(kvp.Key, out _))
                {
                    fields.Add(new NamedValue(kvp.Key, kvp.Value));
                }
            }

            return DValue<RecordValue>.Of(NewRecordFromFields(fields));
        }
    }
}
