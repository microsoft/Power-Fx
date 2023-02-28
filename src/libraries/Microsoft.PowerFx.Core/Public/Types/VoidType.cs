// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Types
{
    public sealed class VoidType : FormulaType
    {
        internal VoidType()
            : base(DType.Void)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVisitor visitor) => visitor.Visit(this);
    }
}
