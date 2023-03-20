// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
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

        public decimal Normalize(decimal value)
        {
            // remove trailing 0's (significant digits)
            return Value / 1.000000000000000000000000000000m;
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            decimal normalized = Normalize(Value);
            sb.Append(normalized.ToString(CultureInfo.InvariantCulture));
        }
    }
}
