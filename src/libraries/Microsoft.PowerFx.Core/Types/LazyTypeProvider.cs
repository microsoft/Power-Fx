// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    internal class LazyTypeProvider
    {
        internal delegate DType GetExpandedType();

        private readonly Lazy<DType> _expandedType;

        public readonly ILazyTypeMetadata LazyTypeMetadata;
        public readonly RecordType Type;

        internal DType ExpandedType => _expandedType.Value;

        public LazyTypeProvider(GetExpandedType expansionFunc, ILazyTypeMetadata lazyTypeMetadata)
        {
            _expandedType = new Lazy<DType>(() => expansionFunc());

            LazyTypeMetadata = lazyTypeMetadata;
            Type = new RecordType(new DType(this, lazyTypeMetadata.IsTable));
        }

        // $$ Could be used for lazy field retrieval?
        public bool TryGetFieldType(DName name, out DType type)
        {
            return ExpandedType.TryGetType(name, out type);
        }
    }
}
