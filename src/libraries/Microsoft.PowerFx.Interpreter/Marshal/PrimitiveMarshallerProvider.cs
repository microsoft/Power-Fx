// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshalling provider to handle builtin primitive types. 
    /// </summary>
    public class PrimitiveMarshallerProvider : ITypeMarshallerProvider
    {      
        /// <inheritdoc/>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, out ITypeMarshaller marshaler)
        {
            if (PrimitiveValueConversions.TryGetFormulaType(type, out var fxType))
            {
                marshaler = new PrimitiveTypeMarshaller(fxType);
                return true;
            }

            // Not supported
            marshaler = null;
            return false;
        }
    }
}
