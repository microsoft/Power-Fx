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
        /// <summary>
        /// Direct use of <see cref="Value"/> is prohibited in favor of <see cref="GetConvertedValue(TimeZoneInfo)"/> method.
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
            return GetConvertedDateTimeValue(_value, timeZoneInfo);
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
                return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            }
            else if (value.Kind == DateTimeKind.Unspecified && timeZoneInfo.BaseUtcOffset == TimeSpan.Zero)
            {
                return TimeZoneInfo.ConvertTimeToUtc(value, timeZoneInfo);
            }
            else if (value.Kind == DateTimeKind.Utc && timeZoneInfo.BaseUtcOffset != TimeSpan.Zero)
            {
                return TimeZoneInfo.ConvertTime(value, timeZoneInfo);
            }

            return value;
        }

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
            sb.Append($"DateTime({_value.Year},{_value.Month},{_value.Day},{_value.Hour},{_value.Minute},{_value.Second},{_value.Millisecond})");
        }
    }
}
