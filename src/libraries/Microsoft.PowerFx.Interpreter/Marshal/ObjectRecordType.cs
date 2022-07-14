// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    /// <summary>
    /// Lazy FormulaType wrapper for marshalled C# objects.
    /// Uses the backing Type to provide identity for equality operations.
    /// </summary>
    internal class ObjectRecordType : RecordType
    {
        public override IEnumerable<string> FieldNames => _marshaller.FieldNames;

        private readonly ObjectMarshaller _marshaller;
        
        private readonly Type _fromType;

        public ObjectRecordType(Type fromType, ObjectMarshaller marshaller)
            : base()
        {
            _marshaller = marshaller;
            _fromType = fromType;
        }

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            return _marshaller.TryGetFieldType(name, out type);
        }

        public override bool Equals(object other)
        {
            return other is ObjectRecordType otherRecord && otherRecord._fromType == _fromType;
        }

        public override int GetHashCode()
        {
            return _fromType.GetHashCode();
        }
    }
}
