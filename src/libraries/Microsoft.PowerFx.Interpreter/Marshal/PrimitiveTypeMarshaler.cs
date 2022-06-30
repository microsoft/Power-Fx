// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshaller for builtin primitives. 
    /// </summary>
    [DebuggerDisplay("PrimitiveMarshal({Type})")]
    public class PrimitiveTypeMarshaller : ITypeMarshaller
    {
        /// <inheritdoc/>
        public FormulaType Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveTypeMarshaller"/> class.
        /// </summary>
        /// <param name="fxType">The power fx type this marshals to.</param>
        public PrimitiveTypeMarshaller(FormulaType fxType)
        {
            Type = fxType;
        }

        /// <inheritdoc/>
        public FormulaValue Marshal(object value)
        {
            return PrimitiveValueConversions.Marshal(value, Type);
        }        
    }
}
