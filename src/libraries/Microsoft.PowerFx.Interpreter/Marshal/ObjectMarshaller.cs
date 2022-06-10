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
        public delegate (RecordType fxType, IReadOnlyDictionary<string, Func<object, FormulaValue>>) GetMaterializedTypeAndMapping();

        private IReadOnlyDictionary<string, Func<object, FormulaValue>> _mapping;
        private readonly GetMaterializedTypeAndMapping _materializeFunc;

        // Map of fx field name to a function produces the formula value given the dotnet object.
        private IReadOnlyDictionary<string, Func<object, FormulaValue>> MaterializedMapping
        {
            get
            {
                if (_mapping == null)
                {       
                    _materializeFunc();
                }

                return _mapping;
            }
        }

        /// <inheritdoc/>
        FormulaType ITypeMarshaller.Type => Type;

        /// <summary>
        /// Strongly typed wrapper for Type. 
        /// </summary>
        public RecordType Type { get; }

        public ObjectMarshaller(GetMaterializedTypeAndMapping materializeFunc)
        {
            _materializeFunc = materializeFunc;
            Type = new LazyTypeProvider(MaterializeTypeAndMapping, LazyMarshalledTypeMetadata.Record).Type;
        }

        /// <inheritdoc/>
        public FormulaValue Marshal(object source)
        {
            var value = new ObjectRecordValue(Type, source, this);
            return value;
        }

        private DType MaterializeTypeAndMapping()
        {
            var (type, mapping) = _materializeFunc();
            _mapping = mapping;
            return type._type;
        }

        // Get the value of the field. 
        // Return null on missing
        internal bool TryGetField(object source, string name, out FormulaValue fieldValue)
        {
            if (MaterializedMapping.TryGetValue(name, out var getter))
            {
                fieldValue = getter(source);
                return true;
            }

            fieldValue = null;
            return false;
        }

        internal IEnumerable<NamedValue> GetFields(object source)
        {
            foreach (var kv in MaterializedMapping)
            {
                var fieldName = kv.Key;
                var getter = kv.Value;

                var value = getter(source);
                yield return new NamedValue(fieldName, value);
            }
        }
    }
}
