// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// This maintains the relatively simple notion of identity for a FormulaType marshalled from C#,
    /// which in this case is just the Type.
    /// </summary>
    [ThreadSafeImmutable]
    public class MarshalledObjectIdentity : ITypeIdentity
    {
        public Type FromType { get; }

        public MarshalledObjectIdentity(Type fromType)
        {
            FromType = fromType;
        }

        public override bool Equals(object obj)
        {
            if (obj is not MarshalledObjectIdentity otherMetadata)
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
