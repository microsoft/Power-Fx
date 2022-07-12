// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class ObjectRecordType : BaseRecordType
    {
        private readonly ObjectMarshaller _marshaller;

        public ObjectRecordType(Type fromType, ObjectMarshaller marshaller)
            : base(new MarshalledObjectIdentity(fromType), marshaller.FieldNames)
        {
             _marshaller = marshaller;
        }

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            return _marshaller.TryGetFieldType(name, out type);
        }
    }
}
