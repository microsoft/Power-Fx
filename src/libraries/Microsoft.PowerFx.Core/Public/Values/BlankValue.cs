// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    [DebuggerDisplay("Blank() ({Type})")]
    public class BlankValue : FormulaValue
    {
        internal BlankValue(IRContext irContext)
            : base(irContext)
        {
        }

        public override object ToObject()
        {
            return null;
        }

        public override string ToString()
        {
            return $"Blank()";
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            if (settings.Compact)
            {
                sb.Append(new BlankType().DefaultExpressionValue());
                return;
            }

            if (Type is BlankType)
            {
                Type.DefaultExpressionValue(sb);
            }
            else
            {
                sb.Append($"If(false,");
                Type.DefaultExpressionValue(sb);
                sb.Append(")");
            }
        }
    }
}
