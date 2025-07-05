// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class DecimalValue : PrimitiveValue<decimal>
    {
        // List of types that allowed to convert to DecimalValue
        internal static readonly IReadOnlyList<FormulaType> AllowedListConvertToDecimal = new FormulaType[] { FormulaType.String, FormulaType.Number, FormulaType.Decimal, FormulaType.DateTime, FormulaType.Date, FormulaType.Boolean };
        internal UnitInfo UnitInfo;

        internal DecimalValue(IRContext irContext, decimal value, UnitInfo unitInfo = null)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Decimal);
            UnitInfo = unitInfo;
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public decimal Normalize()
        {
            // Decimal math retains significant digits. For example 1.23 + 2.77 results in 4.00 when displayed.
            // To retain consistency with floating point math as seen with Float values and Excel,
            // we "normalize" the number to remove trailing zeros.  Note that the 1.000... needs to have enough
            // trailing zeros to cover at least the full range of decimal numbers (at least 30 digits).
            return Value / 1.000000000000000000000000000000m;
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            if (!settings.UseCompactRepresentation)
            {
                sb.Append("Decimal(");
            }

            decimal normalized = Normalize();
            sb.Append(normalized.ToString(CultureInfo.InvariantCulture));
            if (UnitInfo != null)
            {
                sb.Append(" ");
                sb.Append(UnitInfo.ToUnitsString(_value != 1));
            }

            if (!settings.UseCompactRepresentation)
            {
                sb.Append(")");
            }
        }
    }
}
