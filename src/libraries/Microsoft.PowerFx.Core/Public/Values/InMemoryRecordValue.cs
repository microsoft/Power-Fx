// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    // Represent record backed by known list of values. 
    internal class InMemoryRecordValue : RecordValue
    {
        private readonly IReadOnlyDictionary<string, FormulaValue> _fields;
        private readonly IDictionary<string, FormulaValue> _fieldsDict;

        public InMemoryRecordValue(IRContext irContext, IEnumerable<NamedValue> fields)
          : this(irContext, ToDict(fields))
        {
        }

        public InMemoryRecordValue(IRContext irContext, IReadOnlyDictionary<string, FormulaValue> fields)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is RecordType);

            _fields = fields;
            _fieldsDict = fields as IDictionary<string, FormulaValue>;
        }

        private static IReadOnlyDictionary<string, FormulaValue> ToDict(IEnumerable<NamedValue> fields)
        {
            var dict = new Dictionary<string, FormulaValue>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                dict[field.Name] = field.Value;
            }

            return dict;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            return _fields.TryGetValue(fieldName, out result);
        }

        public override FormulaValue UpdateFields(IEnumerable<KeyValuePair<string, FormulaValue>> record)
        {
            if (_fieldsDict == null || _fieldsDict.IsReadOnly)
            {
                return base.UpdateFields(record);
            }

            var fields = new List<NamedValue>();

            foreach (var field in record)
            {
                var fieldName = field.Key;

                // Throws if field is missing
                Type.GetFieldType(fieldName);

                _fieldsDict[fieldName] = field.Value;

                fields.Add(new NamedValue(field.Key, field.Value));
            }

            return NewRecordFromFields(fields);
        }
    }
}
