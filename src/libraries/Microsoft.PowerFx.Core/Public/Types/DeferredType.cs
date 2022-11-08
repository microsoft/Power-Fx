// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// An unknown type, attached to syntax nodes whose type cannot be determined.
    /// </summary>
    public sealed class DeferredType : FormulaType
    {
        internal DeferredType()
            : base(DType.Deferred)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVisitor visitor) => visitor.Visit(this);
    }
}
