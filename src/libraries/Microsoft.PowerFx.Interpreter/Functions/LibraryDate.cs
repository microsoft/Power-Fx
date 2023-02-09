// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static System.TimeZoneInfo;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static async ValueTask<FormulaValue> Today(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            var date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo).Date;
            return new DateValue(irContext, date);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-now-today-istoday
        public static FormulaValue IsToday(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            var arg0 = runner.GetNormalizedDateTime(args[0]);

            var now = DateTime.UtcNow;

            if (timeZoneInfo.BaseUtcOffset != TimeSpan.Zero)
            {
                now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
            }

            var same = (arg0.Year == now.Year) && (arg0.Month == now.Month) && (arg0.Day == now.Day);
            return new BooleanValue(irContext, same);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/show-text-dates-times
        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-dateadd-datediff
        public static FormulaValue DateAdd(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;

            DateTime datetime = runner.GetNormalizedDateTimeAllowTimeValue(args[0]);

            NumberValue delta;
            string timeUnit;

            if (args[1] is NumberValue number)
            {
                delta = number;
                timeUnit = ((StringValue)args[2]).Value.ToLowerInvariant();
            }
            else if (args[1] is TimeValue time)
            {
                delta = NumberValue.New(time.Value.TotalMilliseconds);
                timeUnit = "milliseconds";
            }
            else
            {
                throw new NotImplementedException();
            }

            var useUtcConversion = NeedToConvertToUtc(runner, datetime, timeUnit);

            if (useUtcConversion)
            {
                datetime = TimeZoneInfo.ConvertTimeToUtc(datetime, timeZoneInfo);
            }

            try
            {
                DateTime newDate;
                var deltaValue = delta.Value;
                switch (timeUnit)
                {
                    case "milliseconds":
                        newDate = datetime.AddMilliseconds(deltaValue);
                        break;
                    case "seconds":
                        newDate = datetime.AddSeconds(deltaValue);
                        break;
                    case "minutes":
                        newDate = datetime.AddMinutes(deltaValue);
                        break;
                    case "hours":
                        newDate = datetime.AddHours(deltaValue);
                        break;
                    case "days":
                        newDate = datetime.AddDays(deltaValue);
                        break;
                    case "months":
                        newDate = datetime.AddMonths((int)deltaValue);
                        break;
                    case "quarters":
                        newDate = datetime.AddMonths(((int)deltaValue) * 3);
                        break;
                    case "years":
                        newDate = datetime.AddYears((int)deltaValue);
                        break;
                    default:
                        return GetInvalidUnitError(irContext, "DateAdd");
                }

                if (useUtcConversion)
                {
                    newDate = TimeZoneInfo.ConvertTimeFromUtc(newDate, timeZoneInfo);
                }

                newDate = MakeValidDateTime(runner, newDate, timeZoneInfo);

                if (irContext.ResultType._type.Kind == Core.Types.DKind.Date)
                {
                    return new DateValue(irContext, newDate);
                }
                else if (irContext.ResultType._type.Kind == Core.Types.DKind.Time)
                {
                    return new TimeValue(irContext, newDate.Subtract(_epoch));
                }
                else
                {
                    return new DateTimeValue(irContext, newDate);
                }
            }
            catch
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }
        }

        private static DateTime MakeValidDateTime(EvalVisitor runner, DateTime dateTime, TimeZoneInfo timeZoneInfo)
        {
            return MakeValidDateTime(runner.TimeZoneInfo, dateTime);
        }

        private static DateTime MakeValidDateTime(TimeZoneInfo timeZoneInfo, DateTime dateTime)
        {
            if (dateTime.IsValid(timeZoneInfo))
            {
                return dateTime;
            }

            // If the date is invalid, we want to return the next valid date/time
            return GetNextValidDate(dateTime, timeZoneInfo);
        }

        private static DateTime GetNextValidDate(DateTime invalidDate, TimeZoneInfo timeZoneInfo)
        {
            // Determine which adjustment rule applies to the current date
            var adjr = timeZoneInfo.GetAdjustmentRules().FirstOrDefault(ar => ar.DateStart <= invalidDate && invalidDate < ar.DateEnd);

            if (adjr == null)
            {
                // We cannot correct the invalid date, let's default to what we have
                return invalidDate;
            }

            // As the datetime is invalid, we are necessarily in the invalid range of the DST
            // We will take the beginning of the range and apply the necessary DST offset to get the new time
            var validDate = AdjustMilliseconds(TransitionTimeToDateTime(invalidDate.Year, adjr.DaylightTransitionStart) + adjr.DaylightDelta);

            return validDate;
        }

        private static DateTime AdjustMilliseconds(DateTime datetime)
        {
            // Adjustment rules are usually 1ms off and we need to readjust the date/time to a whole second
            // otherwise we'd show 2:59:59 or 11:59:59 times when displaying the result (could potentially be on the wrong date)
            if (datetime.Millisecond > 995)
            {
                return datetime.AddMilliseconds(1000 - datetime.Millisecond);
            }

            return datetime;
        }

        // From https://referencesource.microsoft.com/#mscorlib/system/timezoneinfo.cs        
        // TransitionTimeToDateTime        
        // Helper function that converts a year and TransitionTime into a DateTime        
        private static DateTime TransitionTimeToDateTime(int year, TransitionTime transitionTime)
        {
            DateTime value;
            DateTime timeOfDay = transitionTime.TimeOfDay;

            if (transitionTime.IsFixedDateRule)
            {
                // create a DateTime from the passed in year and the properties on the transitionTime

                // if the day is out of range for the month then use the last day of the month
                var day = DateTime.DaysInMonth(year, transitionTime.Month);

                value = new DateTime(year, transitionTime.Month, (day < transitionTime.Day) ? day : transitionTime.Day, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
            }
            else
            {
                if (transitionTime.Week <= 4)
                {
                    // Get the (transitionTime.Week)th Sunday.                 
                    value = new DateTime(year, transitionTime.Month, 1, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

                    var dayOfWeek = (int)value.DayOfWeek;
                    var delta = (int)transitionTime.DayOfWeek - dayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }

                    delta += 7 * (transitionTime.Week - 1);

                    if (delta > 0)
                    {
                        value = value.AddDays(delta);
                    }
                }
                else
                {
                    // If TransitionWeek is greater than 4, we will get the last week.                    
                    var daysInMonth = DateTime.DaysInMonth(year, transitionTime.Month);
                    value = new DateTime(year, transitionTime.Month, daysInMonth, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

                    // This is the day of week for the last day of the month.
                    var dayOfWeek = (int)value.DayOfWeek;
                    var delta = dayOfWeek - (int)transitionTime.DayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }

                    if (delta > 0)
                    {
                        value = value.AddDays(-delta);
                    }
                }
            }

            return value;
        }

        public static FormulaValue DateDiff(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime start = runner.GetNormalizedDateTimeAllowTimeValue(args[0]);

            DateTime end = runner.GetNormalizedDateTimeAllowTimeValue(args[1]);

            Contract.Assert(start.Kind == end.Kind);

            var timeUnit = ((StringValue)args[2]).Value.ToLowerInvariant();

            // When converting to months, quarters or years, we don't use the time difference
            // and applying changes to map time to UTC could lead to weird results depending on the local time zone
            // as we could even change of year if the date we have is 1st of Jan and we are in a UTC+N time zone (N positive)
            switch (timeUnit)
            {
                case "months":
                    double months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
                    return new NumberValue(irContext, months);
                case "quarters":
                    // decrementing months by 1 so that we can have months 1-3 (Jan-Mar) on quarter 0, months 4-6 (Apr-Jun) on quarter 1, and so on
                    var quarters = ((end.Year - start.Year) * 4) + Math.Floor((end.Month - 1) / 3.0) - Math.Floor((start.Month - 1) / 3.0);
                    return new NumberValue(irContext, quarters);
                case "years":
                    double years = end.Year - start.Year;
                    return new NumberValue(irContext, years);
            }

            // This takes care of DST differences
            // e.g. https://www.timeanddate.com/time/change/usa/seattle?year=2023
            // start = DateTime(2023, 3, 12, 0, 0, 0) is in UTC-8
            // end = DateTime(2023, 3, 12, 3, 0, 0) is in UTC-7
            // startUTCOffset - endUTCOffset = -1
            // so adding that utcOffset difference to the end(instead of converting both to UTC) will adjust the subtraction for DST
            // while preserving hours, since cases having minutes offset can potentially change the hour.
            var utcOffset = timeZoneInfo.GetUtcOffset(start) - timeZoneInfo.GetUtcOffset(end);
            end += utcOffset;

            // The function DateDiff only returns a whole number of the units being subtracted, and the precision is given in the unit specified.
            switch (timeUnit)
            {
                case "milliseconds":
                    var milliseconds = Math.Floor((end - start).TotalMilliseconds);
                    return new NumberValue(irContext, milliseconds);
                case "seconds":
                    start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second);
                    end = new DateTime(end.Year, end.Month, end.Day, end.Hour, end.Minute, end.Second);
                    var seconds = Math.Floor((end - start).TotalSeconds);
                    return new NumberValue(irContext, seconds);
                case "minutes":
                    start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0);
                    end = new DateTime(end.Year, end.Month, end.Day, end.Hour, end.Minute, 0);
                    var minutes = Math.Floor((end - start).TotalMinutes);
                    return new NumberValue(irContext, minutes);
                case "hours":
                    start = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
                    end = new DateTime(end.Year, end.Month, end.Day, end.Hour, 0, 0);
                    var hours = Math.Floor((end - start).TotalHours);
                    return new NumberValue(irContext, hours);
                case "days":
                    start = new DateTime(start.Year, start.Month, start.Day, 0, 0, 0);
                    end = new DateTime(end.Year, end.Month, end.Day, 0, 0, 0);
                    var days = Math.Floor((end - start).TotalDays);
                    return new NumberValue(irContext, days);
                default:
                    return GetInvalidUnitError(irContext, "DateDiff");
            }
        }

        internal static bool NeedToConvertToUtc(EvalVisitor runner, DateTime datetime, string unit)
        {
            // If datetime is already UTC, no need to apply any conversion
            // For DateAdd in Days or bigger units, we want to preserve the time
            return datetime.Kind != DateTimeKind.Utc &&
                   IsSubdayTimeUnit(unit);
        }

        private static bool IsSubdayTimeUnit(string unit)
        {
            return unit is "milliseconds" or "seconds" or "minutes" or "hours";
        }

        private static ErrorValue GetInvalidUnitError(IRContext irContext, string functionName)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = $"The third argument to the {functionName} function is invalid",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-datetime-parts
        public static FormulaValue Year(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            if (args[0] is BlankValue)
            {
                // TODO: Standardize the number 0 - year 1900 logic
                return new NumberValue(irContext, 1900);
            }

            var arg0 = runner.GetNormalizedDateTime(args[0]);

            var x = arg0.Year;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Day(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 0);
            }

            var arg0 = runner.GetNormalizedDateTime(args[0]);

            var x = arg0.Day;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Month(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 1);
            }

            var arg0 = runner.GetNormalizedDateTime(args[0]);

            var x = arg0.Month;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Hour(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 0);
            }

            var arg0 = runner.GetNormalizedTimeSpan(args[0]);

            var x = arg0.Hours;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Minute(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 0);
            }

            var arg0 = runner.GetNormalizedTimeSpan(args[0]);

            var x = arg0.Minutes;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Second(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 0);
            }

            var arg0 = runner.GetNormalizedTimeSpan(args[0]);

            var x = arg0.Seconds;
            return new NumberValue(irContext, x);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-date-time
        // Date(Year,Month,Day)
        public static FormulaValue Date(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, NumberValue[] args)
        {
            // $$$ fix impl
            var year = (int)args[0].Value;
            var month = (int)args[1].Value;
            var day = (int)args[2].Value;

            return DateImpl(runner, context, irContext, year, month, day);
        }

        private static FormulaValue DateImpl(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, int year, int month, int day)
        {
            // The final date is built up this way to allow for inputs which overflow,
            // such as: Date(2000, 25, 69) -> 3/10/2002
            try
            {
                var timeZoneInfo = runner.TimeZoneInfo;

                var dateTimeKind = runner.DateTimeKind;

                var datetime = new DateTime(year, 1, 1, 0, 0, 0, dateTimeKind)
                    .AddMonths(month - 1)
                    .AddDays(day - 1);

                datetime = MakeValidDateTime(runner, datetime, runner.TimeZoneInfo);

                return new DateValue(irContext, datetime);
            }
            catch (ArgumentOutOfRangeException)
            {
                return CommonErrors.InvalidDateTimeError(irContext);
            }
        }

        public static FormulaValue Time(IRContext irContext, NumberValue[] args)
        {
            var hour = (int)args[0].Value;
            var minute = (int)args[1].Value;
            var second = (int)args[2].Value;
            var millisecond = (int)args[3].Value;

            return TimeImpl(irContext, hour, minute, second, millisecond);
        }

        private static FormulaValue TimeImpl(IRContext irContext, int hour, int minute, int second, int millisecond)
        {
            // The final time is built up this way to allow for inputs which overflow,
            // such as: Time(10, 70, 360) -> 11:16 AM
            var result = new TimeSpan(hour, 0, 0)
                .Add(new TimeSpan(0, minute, 0))
                .Add(new TimeSpan(0, 0, second))
                .Add(TimeSpan.FromMilliseconds(millisecond));

            if (result.TotalDays >= 1)
            {
                result = result.Subtract(TimeSpan.FromDays((int)result.TotalDays));
            }

            return new TimeValue(irContext, result);
        }

        public static FormulaValue DateTimeFunction(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, NumberValue[] args)
        {
            var year = (int)args[0].Value;
            var month = (int)args[1].Value;
            var day = (int)args[2].Value;
            var hour = (int)args[3].Value;
            var minute = (int)args[4].Value;
            var second = (int)args[5].Value;
            var millisecond = (int)args[6].Value;

            try
            {
                var timeZoneInfo = runner.TimeZoneInfo;
                var dateTimeKind = runner.DateTimeKind;

                var dateTime = new DateTime(year, 1, 1, 0, 0, 0, dateTimeKind)
                    .AddMonths(month - 1)
                    .AddDays(day - 1)
                    .AddHours(hour)
                    .AddMinutes(minute)
                    .AddSeconds(second)
                    .AddMilliseconds(millisecond);

                dateTime = MakeValidDateTime(runner, dateTime, timeZoneInfo);

                return new DateTimeValue(irContext, dateTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                return CommonErrors.InvalidDateTimeError(irContext);
            }
        }

        private static async ValueTask<FormulaValue> Now(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var tzInfo = runner.TimeZoneInfo;

            var datetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
            return new DateTimeValue(irContext, datetime);
        }

        private static FormulaValue DateParse(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
        {
            var str = args[0].Value;
            if (str == string.Empty)
            {
                return new BlankValue(irContext);
            }

            if (DateTime.TryParse(str, runner.CultureInfo, DateTimeStyles.AdjustToUniversal, out var result))
            {
                var tzi = runner.TimeZoneInfo;

                result = DateTimeValue.GetConvertedDateTimeValue(result, tzi);

                return new DateValue(irContext, result.Date);
            }
            else
            {
                return CommonErrors.InvalidDateTimeParsingError(irContext);
            }
        }

        private static bool TryGetCulture(string name, out CultureInfo value)
        {
            try
            {
                value = new CultureInfo(name);
                return true;
            }
            catch (CultureNotFoundException)
            {
                value = null;
                return false;
            }
        }

        public static bool TryDateTimeParse(FormattingInfo formatInfo, IRContext irContext, StringValue value, out DateTimeValue result)
        {
            result = null;

            if (DateTime.TryParse(value.Value, formatInfo.CultureInfo, DateTimeStyles.AdjustToUniversal, out var dateTime))
            {
                dateTime = DateTimeValue.GetConvertedDateTimeValue(dateTime, formatInfo.TimeZoneInfo);
                result = new DateTimeValue(irContext, dateTime);
            }

            return result != null;
        }

        public static FormulaValue DateTimeParse(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
        {
            return DateTimeParse(CreateFormattingInfo(runner), irContext, args);
        }

        public static FormulaValue DateTimeParse(FormattingInfo formatInfo, IRContext irContext, StringValue[] args)
        {
            // culture will have Cultural info in-case one was passed in argument else it will have the default one.
            CultureInfo culture = formatInfo.CultureInfo;
            if (args.Length > 1)
            {
                var languageCode = args[1].Value;

                if (!TryGetCulture(languageCode, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, languageCode);
                }

                formatInfo.CultureInfo = culture;
            }

            if (args[0].Value == string.Empty)
            {
                return new BlankValue(irContext);
            }

            if (TryDateTimeParse(formatInfo, irContext, args[0], out var result))
            {
                return result;
            }
            else
            {
                return CommonErrors.InvalidDateTimeParsingError(irContext);
            }
        }

        public static FormulaValue TimeParse(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
        {
            var str = args[0].Value;

            // culture will have Cultural info in-case one was passed in argument else it will have the default one.
            CultureInfo culture = runner.CultureInfo;
            if (args.Length > 1)
            {
                var languageCode = args[1].Value;
                if (!TryGetCulture(languageCode, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, languageCode);
                }
            }

            if (TimeSpan.TryParse(str, runner.CultureInfo, out var result))
            {
                return new TimeValue(irContext, result);
            }
            else
            {
                return CommonErrors.InvalidDateTimeParsingError(irContext);
            }
        }

        // Returns the number of minutes between UTC and either local or defined time zone
        public static FormulaValue TimeZoneOffset(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var tzInfo = runner.TimeZoneInfo;

            if (args.Length == 0)
            {
                var tzOffsetDays = tzInfo.GetUtcOffset(DateTime.Now).TotalDays;
                return new NumberValue(irContext, tzOffsetDays * -1440);
            }

            switch (args[0])
            {
                case DateTimeValue dtv:
                    return new NumberValue(irContext, tzInfo.GetUtcOffset(dtv.GetConvertedValue(tzInfo)).TotalDays * -1440);
                case DateValue dv:
                    return new NumberValue(irContext, tzInfo.GetUtcOffset(dv.GetConvertedValue(tzInfo)).TotalDays * -1440);
                default:
                    return CommonErrors.InvalidDateTimeError(irContext);
            }
        }
    }
}
