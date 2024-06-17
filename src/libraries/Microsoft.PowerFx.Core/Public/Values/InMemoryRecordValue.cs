// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    // Represent record backed by known list of values. 
    internal class InMemoryRecordValue : RecordValue
    {
        protected readonly IReadOnlyDictionary<string, FormulaValue> _fields;
        private readonly IDictionary<string, FormulaValue> _mutableFields;

        public InMemoryRecordValue(IRContext irContext, IEnumerable<NamedValue> fields)
          : this(irContext, ToDict(fields))
        {
        }

        public InMemoryRecordValue(IRContext irContext, params NamedValue[] fields)
          : this(irContext, ToDict(fields))
        {
        }

        public InMemoryRecordValue(IRContext irContext, IReadOnlyDictionary<string, FormulaValue> fields)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is RecordType);

            _fields = fields;
            _mutableFields = fields as IDictionary<string, FormulaValue>;

            if (_mutableFields.IsReadOnly)
            {
                _mutableFields = null;
            }
        }

        public InMemoryRecordValue(InMemoryRecordValue orig)
            : this(orig.IRContext, new Dictionary<string, FormulaValue>(orig._mutableFields))
        {
        }

        public override void ShallowCopyFieldInPlace(string fieldName)
        {
            if (_fields.TryGetValue(fieldName, out FormulaValue result))
            {
                _mutableFields[fieldName] = result.MaybeShallowCopy();
            }
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

        public override async Task<DValue<RecordValue>> UpdateFieldsAsync(RecordValue changeRecord, CancellationToken cancellationToken)
        {
            return await UpdateAllowedFieldsAsync(changeRecord, _fields, cancellationToken).ConfigureAwait(false);
        }

        public override async IAsyncEnumerable<NamedValue> GetOriginalFieldsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var field in OriginalFields)
            {
                yield return field;
            }
        }

        protected override IEnumerable<NamedValue> GetOriginalFieldsCore()
        {
            foreach (var field in _fields)
            {
                yield return new NamedValue(field.Key, field.Value);
            }
        }

        protected async Task<DValue<RecordValue>> UpdateAllowedFieldsAsync(RecordValue changeRecord, IEnumerable<KeyValuePair<string, FormulaValue>> allowedFields, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_mutableFields == null)
            {
                return await base.UpdateFieldsAsync(changeRecord, cancellationToken).ConfigureAwait(false);
            }

            await foreach (var field in changeRecord.GetOriginalFieldsAsync(cancellationToken).ConfigureAwait(false))
            {
                _mutableFields[field.Name] = field.Value;
            }

            var fields = new List<NamedValue>();

            foreach (var kvp in allowedFields)
            {
                fields.Add(new NamedValue(kvp.Key, _fields[kvp.Key]));
            }

            return DValue<RecordValue>.Of(NewRecordFromFields(fields));
        }
    }
}
