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

        public DateTime GetUtc(TimeZoneInfo timeZoneInfo)
        {
            DateTime dt = GetConvertedValue(timeZoneInfo);

            if (dt.Kind == DateTimeKind.Utc)
            {
                return dt;
            }

            return TimeZoneInfo.ConvertTimeToUtc(dt, timeZoneInfo);
        }

        internal static DateTime GetConvertedDateTimeValue(DateTime value, TimeZoneInfo timeZoneInfo)
        {
            // If timeZoneInfo is null and stored value is in UTC, we don't want to convert.
            if (timeZoneInfo == null && value.Kind == DateTimeKind.Utc)
            {
                timeZoneInfo = TimeZoneInfo.Utc;
            }
            else if (timeZoneInfo == null)
            {
                timeZoneInfo = TimeZoneInfo.Local;
            }

            // Since we can't convert LocalKind time to UTC, if the time was of kind local just change kind.
            if (value.Kind == DateTimeKind.Local && timeZoneInfo.BaseUtcOffset == TimeSpan.Zero)
            {
                return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
            else if (value.Kind == DateTimeKind.Local && timeZoneInfo.BaseUtcOffset != TimeSpan.Zero)
            {
                // Suspicious - should probably be TimeZoneInfo.ConvertTimeToUtc(value, timeZoneInfo) as we want to convert to UTC...
                return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            }
            else if (value.Kind == DateTimeKind.Unspecified && timeZoneInfo.BaseUtcOffset == TimeSpan.Zero)
            {
                return TimeZoneInfo.ConvertTimeToUtc(value, timeZoneInfo);
            }
            else if (value.Kind == DateTimeKind.Utc && timeZoneInfo.BaseUtcOffset != TimeSpan.Zero)
            {
                // Should probably use ConvertTimeFromUtc instead
                return TimeZoneInfo.ConvertTime(value, timeZoneInfo);
            }

            return value;
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
