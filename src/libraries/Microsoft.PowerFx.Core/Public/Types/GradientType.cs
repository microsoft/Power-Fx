// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class GradientType : FormulaType
    {
        internal GradientType()
            : base(DType.Gradient)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Gradient";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            sb.Append("LinearGradient(RGBA(0, 0, 0, 0), RGBA(0, 0, 0, 0), 0)");
        }
    }
}
