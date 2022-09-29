// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
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

        public override string ToExpression()
        {
            if (Type is BlankType)
            {
                return Type.DefaultExpressionValue();
            }
            else
            {
                return $"If(false,{Type.DefaultExpressionValue()})";
            }
        }
    }
}
