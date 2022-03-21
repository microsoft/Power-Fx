// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx
{
    // Table based on a .net collection class. 
    // T is marhsalled via a ITypeMarshaller.
    internal class ObjectCollectionTableValue<T> : CollectionTableValue<T>
    {
        // Convert T --> RecordValue
        private readonly ITypeMarshaller _rowMarshaler;

        internal ObjectCollectionTableValue(IRContext irContext, IEnumerable<T> source, ITypeMarshaller rowMarshaler)
            : base(irContext, source)
        {
            _rowMarshaler = rowMarshaler;
        }

        protected override DValue<RecordValue> Marshal(T item)
        {
            var arg = _rowMarshaler.Marshal(item);
            var result = arg switch
            {
                RecordValue r => DValue<RecordValue>.Of(r),
                BlankValue b => DValue<RecordValue>.Of(b),
                _ => DValue<RecordValue>.Of((ErrorValue)arg),
            };
            return result;
        }
    }
}
