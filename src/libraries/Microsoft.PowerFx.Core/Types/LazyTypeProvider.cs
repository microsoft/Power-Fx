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
    /// <summary>
    /// Wrapper class, provides access to derived TryGetFieldType, as well as identity of lazy types via AggregateType.
    /// Also provides faciility to fully expand a level of the type when needed by operations such as Union/AddColumns/...
    /// </summary>
    internal sealed class LazyTypeProvider
    {
        private readonly Dictionary<DName, DType> _expandedFields = new ();
        public readonly AggregateType BackingFormulaType;

        internal IEnumerable<DName> FieldNames => BackingFormulaType.FieldNames.Select(field => new DName(field));

        internal string UserVisibleTypeName => BackingFormulaType.UserVisibleTypeName;

        public LazyTypeProvider(AggregateType type)
        {
            // Ensure we aren't trying to wrap a Known type as lazy. This would cause StackOverflows when calling Equals()
            Contracts.Assert(type is not KnownRecordType and not TableType);

            BackingFormulaType = type;
        }

        // Wrapper function around AggregateType.TryGetFieldType, provides caching
        // in case computing field types is non-trivial for derived AggregateTypes.
        internal bool TryGetFieldType(DName name, out DType type)
        {
            if (_expandedFields.TryGetValue(name, out type))
            {
                return true;
            }
            else if (BackingFormulaType.TryGetFieldType(name.Value, out var fieldType))
            {
                type = fieldType._type;
                _expandedFields.Add(name, type);
                return true;
            }

            return false;
        }

        // In general, this would only occur for imperative use cases like validating Patch/Collect/...
        // or operations that modify the type like Table()/AddColumns/DropColumns/...
        // Beyond that scenario, fully expanding a lazy type is inefficient and should be avoided
        internal DType GetExpandedType(bool isTable)
        {
            var fields = FieldNames.Select(field => 
                TryGetFieldType(field, out var type) ?
                    new TypedName(type, field) :
                    throw new InvalidOperationException($"Fx type of field {field} not found"));

            return isTable ? DType.CreateTable(fields) : DType.CreateRecord(fields);
        }

        public override bool Equals(object obj)
        {
            return BackingFormulaType.Equals(obj);
        }

        public override int GetHashCode()
        {
            return BackingFormulaType.GetHashCode();
        }
    }
}
