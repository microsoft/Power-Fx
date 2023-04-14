// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class NumberValue : PrimitiveValue<double>
    {
        // List of types that allowed to convert to NumberValue
        internal static readonly IReadOnlyList<FormulaType> AllowedListConvertToNumber = new FormulaType[] { FormulaType.String, FormulaType.Number, FormulaType.DateTime, FormulaType.Date, FormulaType.Boolean, FormulaType.Decimal };

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
            if (!settings.UseCompactRepresentation)
            {
                sb.Append("Float(");
            }

            sb.Append((Value == 0) ? "0" : Value.ToString(CultureInfo.InvariantCulture));

            if (!settings.UseCompactRepresentation)
            {
                sb.Append(")");
            }
        }
    }
}
