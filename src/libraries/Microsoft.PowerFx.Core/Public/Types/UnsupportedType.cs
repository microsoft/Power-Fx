// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Type not currently supported by the API.
    /// </summary>
    [DebuggerDisplay("Kind={_type.Kind} TypeFullName={_type.GetType().FullName}")]
    public class UnsupportedType : FormulaType
    {
        internal UnsupportedType(DType type)
            : base(type)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
