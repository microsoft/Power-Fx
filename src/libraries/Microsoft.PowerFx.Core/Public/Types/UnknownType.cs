// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// An unknown type, attached to syntax nodes whose type cannot be determined.
    /// </summary>
    public sealed class UnknownType : FormulaType
    {
        internal UnknownType()
            : base(DType.Unknown)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVistor visitor) => visitor.Visit(this);
    }
}
