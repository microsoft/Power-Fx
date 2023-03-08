// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class NumberValue : PrimitiveValue<double>
    {
        internal NumberValue(IRContext irContext, double value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Number);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append((Value == 0) ? "0" : Value.ToString());
        }
    }

    public class DecimalValue : PrimitiveValue<decimal>
    {
        internal DecimalValue(IRContext irContext, decimal value)
            : base(irContext, value)
        {
            bool x = irContext.ResultType == FormulaType.Decimal;
            if (!x)
            {
                x = true;
            }

            Contract.Assert(IRContext.ResultType == FormulaType.Decimal);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            decimal normalized = Value / 1.00000000000000000000000000000m;
            sb.Append(normalized.ToString());
        }
    }
}
