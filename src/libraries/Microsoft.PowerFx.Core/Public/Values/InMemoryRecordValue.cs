// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    // Represent record backed by known list of values. 
    internal class InMemoryRecordValue : RecordValue
    {
        private readonly IReadOnlyDictionary<string, FormulaValue> _fields;

        public InMemoryRecordValue(IRContext irContext, IEnumerable<NamedValue> fields)
          : this(irContext, ToDict(fields))
        {
        }

        public InMemoryRecordValue(IRContext irContext, IReadOnlyDictionary<string, FormulaValue> fields)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is RecordType);
            var recordType = (RecordType)IRContext.ResultType;

            _fields = fields;
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
    }
}
