// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class ColorValue : PrimitiveValue<Color>
    {
        internal ColorValue(IRContext irContext, Color value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Color);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append($"RGBA({Value.R},{Value.G},{Value.B},{Math.Round(Value.A / 255.0, 3)})");
        }
    }
}
