﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    public sealed class LazyTypeProvider
    {
        public delegate FormulaType FieldTypeGetter();

        private readonly ImmutableDictionary<DName, FieldTypeGetter> _fieldTypeGetters;
        private readonly Dictionary<DName, DType> _expandedFields = new Dictionary<DName, DType>();

        public readonly ILazyTypeMetadata LazyTypeMetadata;

        public LazyTypeProvider(ILazyTypeMetadata metadata, Dictionary<DName, FieldTypeGetter> fieldTypeGetters)
        {            
            LazyTypeMetadata = metadata;
            _fieldTypeGetters = fieldTypeGetters.ToImmutableDictionary();
        }

        private LazyTypeProvider(ILazyTypeMetadata metadata, ImmutableDictionary<DName, FieldTypeGetter> fieldTypeGetters)
        {            
            LazyTypeMetadata = metadata;
            _fieldTypeGetters = fieldTypeGetters;
        }

        internal bool TryGetFieldType(DName name, out DType type)
        {
            if (_expandedFields.TryGetValue(name, out type))
            {
                return true;
            }
            else if (_fieldTypeGetters.TryGetValue(name, out var getter))
            {
                type = getter()._type;
                _expandedFields.Add(name, type);
            }

            return false;
        }

        // Used for DType modifications without full expansion (e.g. as part of AddColumns)
        internal LazyTypeProvider AddField(DName name, DType type)
        {
            Contracts.Assert(!_fieldTypeGetters.ContainsKey(name));

            return new LazyTypeProvider(LazyTypeMetadata, _fieldTypeGetters.Add(name, () => FormulaType.Build(type)));
        }
        
        // Used for DType modifications without full expansion (e.g. as part of DropColumns/ShowColumns/...)
        internal LazyTypeProvider DropField(DName name)
        {
            return new LazyTypeProvider(LazyTypeMetadata, _fieldTypeGetters.Remove(name));
        }

        // Primarily used for DType Accepts to preserve Relationship/Attachments behavior. 
        // Specifically covers the `LazyType.Accepts(nonLazyType)` case.
        // In general, this would only occur for imperative use cases like validating Patch/Collect/...
        // Where the value being patched is non-lazy
        // This use case does not currently apply to marshalled types, and if that changes, we can revisit this behavior. 
        internal bool TryGetExpandedType(bool isTable, out DType expandedType)
        {
            expandedType = DType.Invalid;
            if (!LazyTypeMetadata.IsFullExpansionAllowed) 
            {
                return false;
            }

            var fields = _fieldTypeGetters.Select(kvp => new TypedName(kvp.Value()._type, kvp.Key));

            expandedType = isTable ? DType.CreateTable(fields) : DType.CreateRecord(fields);
            return true;
        }
    }
}
