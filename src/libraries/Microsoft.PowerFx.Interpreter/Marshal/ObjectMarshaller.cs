// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshal a specific type of object to a record. 
    /// </summary>
    [DebuggerDisplay("ObjMarshal({Type})")]
    public class ObjectMarshaller : ITypeMarshaller
    {
        /// <summary>
        /// Value Marshalling function for a field.
        /// </summary>
        public delegate FormulaValue FieldValueMarshaller(object source);

        /// <summary>
        /// Method that should return the Type and Value Marshalling function for a field.
        /// </summary>
        public delegate (FormulaType fieldType, FieldValueMarshaller fieldValueMarshaller) FieldTypeAndValueMarshallerGetter();

        private readonly Dictionary<string, FieldTypeAndValueMarshallerGetter> _fieldGetters; 

        /// <inheritdoc/>
        FormulaType ITypeMarshaller.Type => Type;

        /// <summary>
        /// Strongly typed wrapper for Type. 
        /// </summary>
        public RecordType Type { get; }

        public IEnumerable<string> FieldNames => _fieldGetters.Select(kvp => kvp.Key);

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectMarshaller"/> class.
        /// </summary>
        public ObjectMarshaller(Dictionary<string, FieldTypeAndValueMarshallerGetter> fieldGetters, Type fromType)
        {
            _fieldGetters = fieldGetters;
            Type = new ObjectRecordType(fromType, this);
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
            fieldValue = null;
            if (_fieldGetters.TryGetValue(name, out var getter))
            {
                fieldValue = GetMarshalledValue(source, name, getter);
                return true;
            }

            return false;
        }

        private static FormulaValue GetMarshalledValue(object source, string name, FieldTypeAndValueMarshallerGetter getter)
        {
            var (_, valueMarshaller) = getter();
            if (valueMarshaller == null) 
            {
                throw new InvalidOperationException($"Failed to retrieve value marshaller for registered field {name}");
            }

            return valueMarshaller(source);
        }

        internal bool TryGetFieldType(string name, out FormulaType type)
        {
            type = FormulaType.Blank;
            if (_fieldGetters.TryGetValue(name, out var getter))
            {
                (type, _) = getter();
                return true;
            }

            return false;
        }
    }
}
