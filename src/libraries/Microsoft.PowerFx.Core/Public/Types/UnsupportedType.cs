// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Type not currently supported by the API.
    /// </summary>
    public class UnsupportedType : FormulaType
    {
        internal UnsupportedType(DType type)
            : base(type)
        {
        }

        /// <inheritdoc />
        public override void Visit(ITypeVistor visitor)
        {
            visitor.Visit(this);
        }

        internal string Description => $"kind={_type.Kind} fullTypeName={_type.GetType().FullName}";
    }
}
