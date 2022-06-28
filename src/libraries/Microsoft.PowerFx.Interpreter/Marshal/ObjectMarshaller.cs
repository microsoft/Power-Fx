// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshal a specific type of object to a record. 
    /// </summary>
    [DebuggerDisplay("ObjMarshal({Type})")]
    public class ObjectMarshaller : ITypeMarshaller
    {
        public delegate FormulaValue FieldValueMarshaller(object source);

        public delegate (FormulaType fieldType, FieldValueMarshaller fieldValueMarshaller) FieldTypeAndValueMashallerGetter();

        // Map fx field name to a function produces the formula value given the dotnet object.
        private readonly Dictionary<DName, FieldValueMarshaller> _marshallerMapping;

        /// <inheritdoc/>
        FormulaType ITypeMarshaller.Type => Type;

        /// <summary>
        /// Strongly typed wrapper for Type. 
        /// </summary>
        public RecordType Type { get; }

        public ObjectMarshaller(Dictionary<DName, FieldTypeAndValueMashallerGetter> fieldGetters, Type fromType)
        {
            var fieldTypeGetters = fieldGetters.ToDictionary(kvp => kvp.Key, kvp => UseFieldType(kvp.Key, kvp.Value));
            var provider = new LazyTypeProvider(new LazyMarshalledTypeMetadata(fromType), fieldTypeGetters);
            Type = RecordType.FromLazyProvider(provider);
        }

        /// <inheritdoc/>
        public FormulaValue Marshal(object source)
        {
            var value = new ObjectRecordValue(Type, source, this);
            return value;
        }

        // Get the value of the field. 
        // Return null if the field was unused or doesn't exist.
        internal bool TryGetField(object source, string name, out FormulaValue fieldValue)
        {
            if (_marshallerMapping.TryGetValue(new DName(name), out var getter))
            {
                fieldValue = getter(source);
                return true;
            }

            fieldValue = null;
            return false;
        }

        // Helper Function, ensures that when a field is accessed via LazyTypeProvider, we also add it's
        // value marshalling function to the mapping.
        private LazyTypeProvider.FieldTypeGetter UseFieldType(DName fieldName, FieldTypeAndValueMashallerGetter fieldTypeAndValueMarshaller)
        {
            return () =>
            {
                var (fieldType, valueMarshaller) = fieldTypeAndValueMarshaller();
                _marshallerMapping[fieldName] = valueMarshaller;
                return fieldType;
            };
        }
    }
}
