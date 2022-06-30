// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshalling provider to handle builtin primitive types. 
    /// </summary>
    public class PrimitiveMarshallerProvider : ITypeMarshallerProvider
    {      
        /// <inheritdoc/>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaller)
        {
            if (PrimitiveValueConversions.TryGetFormulaType(type, out var fxType))
            {
                marshaller = new PrimitiveTypeMarshaller(fxType);
                return true;
            }

            // Not supported
            marshaller = null;
            return false;
        }
    }
}
