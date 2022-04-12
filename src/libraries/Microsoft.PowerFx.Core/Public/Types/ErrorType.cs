// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// An error type, attached to syntax nodes whose type is incorrect.
    /// </summary>
    public class ErrorType : FormulaType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorType"/> class.
        /// </summary>
        public ErrorType()
            : base(DType.Error)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVistor visitor) => visitor.Visit(this);
    }
}
