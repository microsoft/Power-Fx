// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    [ThreadSafeImmutable]
    internal class LazyMarshalledTypeMetadata : ILazyTypeMetadata
    {
        public Type FromType { get; }

        public LazyMarshalledTypeMetadata(Type fromType)
        {
            FromType = fromType;
        }

        public override bool Equals(object obj)
        {
            if (obj is not LazyMarshalledTypeMetadata otherMetadata)
            {
                return false;
            }

            return otherMetadata.FromType.Equals(FromType);
        }

        public override int GetHashCode()
        {
            return FromType.GetHashCode();
        } 
    }
}
