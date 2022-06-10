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

        private readonly GetExpandedType _expansionFunc;
        private readonly Lazy<DType> _expandedType;

        public readonly ILazyTypeMetadata LazyTypeMetadata;
        public readonly DType Type;

        internal DType ExpandedType => _expandedType.Value;

        public LazyTypeProvider(GetExpandedType expansionFunc, ILazyTypeMetadata lazyTypeMetadata)
        {
            _expansionFunc = expansionFunc;
            LazyTypeMetadata = lazyTypeMetadata;

            _expandedType = new Lazy<DType>(ExpandType);
            Type = new DType(this, lazyTypeMetadata.IsTable);
        }

        private DType ExpandType()
        {
            return LazyTypeMetadata.IsTable ? _expansionFunc().ToTable() : _expansionFunc().ToRecord();
        }

        // $$ Could be used for lazy field retrieval?
        public bool TryGetFieldType(DName name, out DType type)
        {
            return ExpandedType.TryGetType(name, out type);
        }
    }
}
