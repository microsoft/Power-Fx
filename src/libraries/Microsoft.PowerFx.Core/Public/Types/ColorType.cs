// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class ColorType : FormulaType
    {
        internal ColorType()
            : base(DType.Color)
        {
        }

        public override void Visit(ITypeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
