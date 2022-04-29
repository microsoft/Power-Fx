﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        internal ObjectRecordValue(RecordType type, object source, ObjectMarshaller marshaler) 
            : base(type)
        {
            Source = source;
            _mapping = marshaler;
        }
                
        /// <inheritdoc/>
        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            return _mapping.TryGetField(Source, fieldName, out result);            
        }

        public override object ToObject()
        {
            return Source;
        }
    }
}
