// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    public class LazyTypeProvider
    {
        public delegate FormulaType FieldTypeGetter();

        private readonly IReadOnlyDictionary<DName, FieldTypeGetter> _fieldTypeGetters;
        private readonly Dictionary<DName, DType> _expandedFields = new Dictionary<DName, DType>();

        public readonly ILazyTypeMetadata LazyTypeMetadata;

        public LazyTypeProvider(ILazyTypeMetadata metadata, IReadOnlyDictionary<DName, FieldTypeGetter> fieldTypeGetters)
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
    }
}
