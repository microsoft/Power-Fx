// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// A binding error type, attached to syntax nodes whose type is incorrect.
    /// </summary>
    public sealed class BindingErrorType : FormulaType
    {
        internal BindingErrorType()
            : base(DType.Error)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVisitor visitor) => visitor.Visit(this);
    }
}
