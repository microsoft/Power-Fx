// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    [ThreadSafeImmutable]
    internal class LazyMarshalledTypeMetadata : ILazyTypeMetadata
    {
        public static readonly LazyMarshalledTypeMetadata Table = new LazyMarshalledTypeMetadata(true);
        public static readonly LazyMarshalledTypeMetadata Record = new LazyMarshalledTypeMetadata(false);

        public bool IsTable { get; }

        private LazyMarshalledTypeMetadata(bool isTable)
        {
            IsTable = isTable;
        }
    }
}
