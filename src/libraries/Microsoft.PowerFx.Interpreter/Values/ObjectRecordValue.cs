// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Types
{    
    /// <summary>
    /// Represent a Record that's backed by a DotNet object. 
    /// Field access is lazy.
    /// </summary>
    public class ObjectRecordValue : RecordValue
    {
        /// <summary>
        /// The host object that this was originally created around. 
        /// </summary>
        public object Source { get; private set; }

        private readonly ObjectMarshaller _mapping;

        internal ObjectRecordValue(RecordType type, object source, ObjectMarshaller marshaller)
            : base(type)
        {
            Source = source;
            _mapping = marshaller;
        }

        public override IEnumerable<NamedValue> Fields
        {
            get
            {
                foreach (var name in _mapping.UsedFields)
                {
                    var fieldType = Type.GetFieldType(name);
                    var value = GetField(fieldType, name);
                    yield return new NamedValue(name, value);
                }
            }
        }

        /// <inheritdoc/>
        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            if (_mapping.TryGetField(Source, fieldName, out result))
            {
                return true;
            }

            // Retrieving the type might populate the mapping for this field
            fieldType = Type.MaybeGetFieldType(fieldName);
            return fieldType != null && _mapping.TryGetField(Source, fieldName, out result);
        }

        public override object ToObject()
        {
            return Source;
        }
    }
}
