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
    /// Internal type for non-lazy Record types.
    /// May also be a wrapper around host-derived table types.
    /// </summary>
    internal sealed class KnownRecordType : RecordType
    {
        public override IEnumerable<string> FieldNames => _type.GetRootFieldNames().Select(name => name.Value);

        internal KnownRecordType(DType type)
            : base(type)
        {
        }

        internal KnownRecordType()
            : base(DType.EmptyRecord)
        {
        }

        public override bool Equals(object other)
        {
            if (other is not KnownRecordType otherRecordType)
            {
                return false;
            }

            if (_type.IsLazyType && otherRecordType._type.IsLazyType && _type.IsRecord == otherRecordType._type.IsRecord)
            {
                return _type.LazyTypeProvider.BackingFormulaType.Equals(otherRecordType._type.LazyTypeProvider.BackingFormulaType);
            }

            return _type.Equals(otherRecordType._type);
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }
    }
}
