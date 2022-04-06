// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Values
{
    internal class InMemoryRecordValue : RecordValue
    {
        private readonly Dictionary<string, FormulaValue> _fields = new Dictionary<string, FormulaValue>();

        public override IEnumerable<NamedValue> Fields =>
            from field in _fields select new NamedValue(field);

        public InMemoryRecordValue(IRContext irContext, IEnumerable<NamedValue> fields)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is RecordType);
            var recordType = (RecordType)IRContext.ResultType;
            var fieldDictionary = recordType.GetNames().ToDictionary(v => v.Name);
            foreach (var field in fields)
            {
                _fields[field.Name] = PropagateFieldType(field.Value, fieldDictionary[new DName(field.Name)].Type);
            }
        }

        internal override FormulaValue GetField(IRContext irContext, string name)
        {
            if (_fields.TryGetValue(name, out var value))
            {
                return value;
            }

            return new BlankValue(irContext);
        }

        private FormulaValue PropagateFieldType(FormulaValue fieldValue, FormulaType fieldType)
        {
            if (fieldValue is RecordValue recordValue)
            {
                return new InMemoryRecordValue(IRContext.NotInSource(fieldType), recordValue.Fields);
            }

            if (fieldValue is TableValue tableValue)
            {
                return new InMemoryTableValue(IRContext.NotInSource(fieldType), tableValue.Rows);
            }

            return fieldValue;
        }
    }
}
