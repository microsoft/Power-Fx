// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshal a specific type of object to a record. 
    /// </summary>
    [DebuggerDisplay("ObjMarshal({Type})")]
    public class ObjectMarshaller : ITypeMarshaller
    {
        public delegate RecordType GetMaterializedType();

        public delegate Func<object, FormulaValue> GetFieldMapping(string powerFxFieldName);

        // Map fx field name to a function produces the formula value given the dotnet object.
        private readonly IReadOnlyDictionary<string, Func<object, FormulaValue>> _mapping;

        /// <inheritdoc/>
        FormulaType ITypeMarshaller.Type => Type;

        /// <summary>
        /// Strongly typed wrapper for Type. 
        /// </summary>
        public RecordType Type { get; }

        public ObjectMarshaller(GetMaterializedType getMaterializedType, GetFieldMapping getFieldMapping)
        {
            var lazyTypeProvider = new LazyTypeProvider(() => getMaterializedType()._type, LazyMarshalledTypeMetadata.Record);
            Type = new RecordType(lazyTypeProvider.ExpandedType);
            _mapping = new Dictionary<string, Func<object, FormulaValue>>();
        }

        /// <inheritdoc/>
        public FormulaValue Marshal(object source)
        {
            var value = new ObjectRecordValue(Type, source, this);
            return value;
        }

        // Get the value of the field. 
        // Return null on missing
        internal bool TryGetField(object source, string name, out FormulaValue fieldValue)
        {
            if (_mapping.TryGetValue(name, out var getter))
            {
                fieldValue = getter(source);
                return true;
            }

            fieldValue = null;
            return false;
        }

        internal IEnumerable<NamedValue> GetFields(object source)
        {
            foreach (var kv in _mapping)
            {
                var fieldName = kv.Key;
                var getter = kv.Value;

                var value = getter(source);
                yield return new NamedValue(fieldName, value);
            }
        }
    }
}
