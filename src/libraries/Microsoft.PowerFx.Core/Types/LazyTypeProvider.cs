// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    internal sealed class LazyTypeProvider
    {
        public delegate bool FieldTypeGetter(string fieldName, out FormulaType fieldType);

        private readonly FieldTypeGetter _fieldTypeGetter;
        private readonly Dictionary<DName, DType> _expandedFields = new ();        
        private readonly IEnumerable<string> _fields;

        public ITypeIdentity Identity { get; }

        public LazyTypeProvider(ITypeIdentity identity, IEnumerable<string> fields, FieldTypeGetter fieldTypeGetter)
        {            
            Identity = identity;
            _fieldTypeGetter = fieldTypeGetter;
            _fields = fields;
        }

        internal bool TryGetFieldType(DName name, out DType type)
        {
            if (_expandedFields.TryGetValue(name, out type))
            {
                return true;
            }
            else if (_fieldTypeGetter(name.Value, out var fieldType))
            {
                type = fieldType.DType;
                _expandedFields.Add(name, type);
                return true;
            }

            return false;
        }

        // In general, this would only occur for imperative use cases like validating Patch/Collect/...
        // or operations that modify the type like AddColumns/DropColumns/...
        // Beyond that scenario, fully expanding a lazy type is inefficient and should be avoided
        internal DType GetExpandedType(bool isTable)
        {
            var fields = _fields.Select(field => 
                TryGetFieldType(new DName(field), out var type) ?
                    new TypedName(type, new DName(field)) :
                    throw new InvalidOperationException($"Fx type of field {field} not found"));

            return isTable ? DType.CreateTable(fields) : DType.CreateRecord(fields);
        }
    }
}
