// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// An unknown type, attached to syntax nodes whose type cannot be determined.
    /// </summary>
    public class UnknownType : FormulaType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownType"/> class.
        /// </summary>
        public UnknownType()
            : base(DType.Unknown)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVistor visitor) => visitor.Visit(this);
    }
}
