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
            var date = Value.Date;
            var timeSpan = Value.TimeOfDay;

            sb.Append($"DateTime({date.Year},{date.Month},{date.Day},{timeSpan.Hours},{timeSpan.Minutes},{timeSpan.Seconds},{timeSpan.Milliseconds})");
        }
    }
}
