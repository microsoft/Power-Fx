// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Special Type which can't be coerced or accepted by any types.
    /// </summary>
    public sealed class Void : FormulaType
    {
        internal Void()
            : base(DType.Void)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVisitor visitor) => visitor.Visit(this);
    }
}
