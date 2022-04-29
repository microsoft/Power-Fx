// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshal a specific type of object to a record. 
    /// </summary>
    [DebuggerDisplay("ObjMarshal({Type})")]
    public class ObjectMarshaller : ITypeMarshaller
    {
        // Map fx field name to a function produces the formula value given the dotnet object.
        private readonly IReadOnlyDictionary<string, Func<object, FormulaValue>> _mapping;

        /// <inheritdoc/>
        FormulaType ITypeMarshaller.Type => Type;

        /// <summary>
        /// Strongly typed wrapper for Type. 
        /// </summary>
        public RecordType Type { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectMarshaller"/> class.
        /// </summary>
        /// <param name="type">The FormulaType that these objects product.</param>
        /// <param name="fieldMap">A mapping of fx field names to functions that produce that field. </param>
        public ObjectMarshaller(RecordType type, IReadOnlyDictionary<string, Func<object, FormulaValue>> fieldMap)
        {
            if (!(type is RecordType))
            {
                throw new ArgumentException($"type must be a record, not ${type}");
            }

            Type = type;
            _mapping = fieldMap;
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
