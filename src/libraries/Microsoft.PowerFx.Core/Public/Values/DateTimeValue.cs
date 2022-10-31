// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Represents a Date and Time together, in the local time zone.
    /// </summary>
    public class DateTimeValue : PrimitiveValue<DateTime>
    {
        internal DateTimeValue(IRContext irContext, DateTime value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.DateTime);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {            
            if (settings.UseCompactRepresentation)
            {
                // DateTime(2022,10,23,12,34,56,999)
                sb.Append($"DateTime({Value.Year},{Value.Month},{Value.Day},{Value.Hour},{Value.Minute},{Value.Second},{Value.Millisecond})");
                return;
            }

            // DateTimeValue("2022-10-25T20:31:38.0594225Z")
            sb.Append($"DateTimeValue({CharacterUtils.ToPlainText(Value.ToString("o", CultureInfo.InvariantCulture))})");
        }
    }
}
