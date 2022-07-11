// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class ObjectRecordType : RecordType
    {
        public override IAggregateTypeIdentity Identity { get; }

        public override IEnumerable<string> FieldNames => _marshaller.FieldNames;

        private readonly ObjectMarshaller _marshaller;

        public ObjectRecordType(Type fromType, ObjectMarshaller marshaller)
            : base()
        {
            Identity = new MarshalledObjectIdentity(fromType);
            _marshaller = marshaller;
        }

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            return _marshaller.TryGetFieldType(name, out type);
        }
    }
}
