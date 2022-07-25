// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Internal type for non-lazy table types.
    /// May also be a wrapper around host-derived record types.
    /// </summary>
    internal sealed class KnownTableType : TableType
    {
        public override IEnumerable<string> FieldNames => _type.GetRootFieldNames().Select(name => name.Value);

        internal KnownTableType(DType type)
            : base(type)
        {
        }

        internal KnownTableType()
            : base(DType.EmptyTable)
        {
        }

        public override bool Equals(object other)
        {
            if (other is not KnownTableType otherTableType)
            {
                return false;
            }

            if (_type.IsLazyType && otherTableType._type.IsLazyType && _type.IsTable == otherTableType._type.IsTable)
            {
                return _type.LazyTypeProvider.BackingFormulaType.Equals(otherTableType._type.LazyTypeProvider.BackingFormulaType);
            }

            return _type.Equals(otherTableType._type);
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }
    }
}
