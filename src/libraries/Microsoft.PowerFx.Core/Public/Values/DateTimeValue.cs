// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Represents a Date and Time together, in the local time zone.
    /// </summary>
    [DebuggerDisplay("{ToObject().ToString()} ({Type}) {Value.Kind.ToString()}")]
    public class DateTimeValue : PrimitiveValue<DateTime>
    {
        // List of types that allowed to convert to DateTimeValue
        internal static readonly IReadOnlyList<FormulaType> AllowedListConvertToDateTime = new FormulaType[] { FormulaType.String, FormulaType.Number, FormulaType.Decimal, FormulaType.DateTime, FormulaType.Date };

        /// <summary>
        /// Direct use of <see cref="Value"/> is prohibited in favor of <see cref="GetConvertedValue(TimeZoneInfo)"/> method.
        /// Only use 'new' keyword here to catch accidental uses of base class method. 
        /// </summary>
        [Obsolete("Use Method" + nameof(GetConvertedValue) + " instead, with proper timezone information.")]
        public new DateTime Value => base.Value;

        /// <summary>
        /// Converts To UTC time if value is not utc and <paramref name="timeZoneInfo"/> is utc.
        /// Converts from UTC time if value is utc and <paramref name="timeZoneInfo"/> is non utc.
        /// else returns the  value/>/>.
        /// NOTE: if <paramref name="timeZoneInfo"/> is null, Local time zone is used.
        /// </summary>
        public DateTime GetConvertedValue(TimeZoneInfo timeZoneInfo)
        {
            return GetConvertedDateTimeValue(_value, timeZoneInfo);
        }

        internal static DateTime GetConvertedDateTimeValue(DateTime value, TimeZoneInfo timeZoneInfo)
        {
            // Ensure timeZoneInfo is not null; default to local time zone if it is.
            if (timeZoneInfo == null)
            {
                timeZoneInfo = TimeZoneInfo.Local;
            }

            DateTime result;

            if (value.Kind == DateTimeKind.Local)
            {
                // Convert from local time to the specified time zone.
                result = TimeZoneInfo.ConvertTime(value, TimeZoneInfo.Local, timeZoneInfo);
            }
            else if (value.Kind == DateTimeKind.Utc)
            {
                // Convert from UTC to the specified time zone.
                result = TimeZoneInfo.ConvertTimeFromUtc(value, timeZoneInfo);
            }
            else 
            {
                // DateTimeKind.Unspecified
                // Assume the unspecified DateTime is in the specified time zone.
                // If you need to convert it to another time zone, specify the source time zone.
                result = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            }

            return result;
        }

        internal DateTimeValue(IRContext irContext, DateTime value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.DateTime || IRContext.ResultType == FormulaType.DateTimeNoTimeZone);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append($"DateTime({_value.Year},{_value.Month},{_value.Day},{_value.Hour},{_value.Minute},{_value.Second},{_value.Millisecond})");
        }
    }
}
