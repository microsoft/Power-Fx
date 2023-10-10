// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Represents a Date only, without a time component, in the local time zone.
    /// </summary>
    [DebuggerDisplay("{ToObject().ToString()} ({Type}) {Value.Kind.ToString()}")]
    public class DateValue : PrimitiveValue<DateTime>
    {
        /// <summary>
        /// Direct use of <see cref="Value"/> is prohibited in favor of <see cref="GetConvertedValue(TimeZoneInfo)"/> method.
        /// Only use 'new' keyword here to catch accidental uses of base class method. 
        /// </summary>
        [Obsolete("Use Method" + nameof(GetConvertedValue) + " instead, with proper timezone information.")]
        public new DateTime Value => base.Value;

        /// <summary>
        /// Converts To UTC time if value is not utc and <paramref name="timeZoneInfo"/> is utc.
        /// Converts from UTC time if  value is utc and <paramref name="timeZoneInfo"/> is non utc.
        /// else returns the  value/>/>.
        /// NOTE: if <paramref name="timeZoneInfo"/> is null, Local time zone is used.
        /// </summary>
        public DateTime GetConvertedValue(TimeZoneInfo timeZoneInfo)
        {
            return DateTimeValue.GetConvertedDateTimeValue(_value, timeZoneInfo);
        }

        public DateTime GetUtc(TimeZoneInfo timeZoneInfo)
        {
            DateTime dt = GetConvertedValue(timeZoneInfo);

            if (dt.Kind == DateTimeKind.Utc)
            {
                return dt;
            }

            return TimeZoneInfo.ConvertTimeToUtc(dt, timeZoneInfo);
        }

        internal DateValue(IRContext irContext, DateTime value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Date);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append($"Date({_value.Year},{_value.Month},{_value.Day})");
        }
    }
}
