// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class ColorType : FormulaType
    {
        internal ColorType()
            : base(DType.Color)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Color";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            sb.Append($"RGBA(0, 0, 0, 1)");
        }
    }
}
