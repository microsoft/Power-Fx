// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal partial class Library
    {
        public static FormulaValue Today(IRContext irContext, FormulaValue[] args)
        {
            // $$$ timezone?
            var date = DateTime.Today;

            return new DateValue(irContext, date);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-now-today-istoday
        public static FormulaValue IsToday(IRContext irContext, FormulaValue[] args)
        {
            DateTime arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value;
                    break;
                case DateValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var now = DateTime.Today;
            var same = (arg0.Year == now.Year) && (arg0.Month == now.Month) && (arg0.Day == now.Day);
            return new BooleanValue(irContext, same);
        }

        // When not specified, default time zone is the local one.
        private static TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/show-text-dates-times
        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-dateadd-datediff
        public static FormulaValue DateAdd(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.GetService<TimeZoneInfo>() ?? LocalTimeZone;

            DateTime datetime;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    datetime = dtv.Value;
                    break;
                case DateValue dv:
                    datetime = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var delta = (NumberValue)args[1];
            var units = (StringValue)args[2];

            datetime = TimeZoneInfo.ConvertTimeToUtc(datetime, timeZoneInfo);

            try
            {
                DateTime newDate;
                switch (units.Value.ToLower())
                {
                    case "milliseconds":
                        newDate = datetime.AddMilliseconds(delta.Value);
                        break;
                    case "seconds":
                        newDate = datetime.AddSeconds(delta.Value);
                        break;
                    case "minutes":
                        newDate = datetime.AddMinutes(delta.Value);
                        break;
                    case "hours":
                        newDate = datetime.AddHours(delta.Value);
                        break;
                    case "days":
                        newDate = datetime.AddDays(delta.Value);
                        break;
                    case "months":
                        newDate = datetime.AddMonths((int)delta.Value);
                        break;
                    case "quarters":
                        newDate = datetime.AddMonths((int)delta.Value * 3);
                        break;
                    case "years":
                        newDate = datetime.AddYears((int)delta.Value);
                        break;
                    default:
                        // TODO: Task 10723372: Implement Unit Functionality in DateAdd, DateDiff Functions
                        return CommonErrors.NotYetImplementedError(irContext, "DateAdd Only supports Days for the unit field");
                }

                newDate = TimeZoneInfo.ConvertTimeFromUtc(newDate, timeZoneInfo);

                if (args[0] is DateTimeValue)
                {
                    return new DateTimeValue(irContext, newDate);
                }
                else
                {
                    return new DateValue(irContext, newDate.Date);
                }
            }
            catch
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }
        }

        public static FormulaValue DateDiff(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            DateTime start;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    start = dtv.Value;
                    break;
                case DateValue dv:
                    start = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            DateTime end;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    end = dtv.Value;
                    break;
                case DateValue dv:
                    end = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var units = (StringValue)args[2];

            // When converting to months, quarters or years, we don't use the time difference
            // and applying changes to map time to UTC could lead to weird results depending on the local time zone
            // as we could even change of year if the date we have is 1st of Jan and we are in a UTC+N time zone (N positive)
            switch (units.Value.ToLower())
            {
                case "months":
                    double months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
                    return new NumberValue(irContext, months);
                case "quarters":
                    var quarters = ((end.Year - start.Year) * 4) + Math.Floor(end.Month / 3.0) - Math.Floor(start.Month / 3.0);
                    return new NumberValue(irContext, quarters);
                case "years":
                    double years = end.Year - start.Year;
                    return new NumberValue(irContext, years);
            }

            // Convert to UTC to be accurate (apply DST if needed)
            var timeZoneInfo = runner.GetService<TimeZoneInfo>() ?? LocalTimeZone;

            start = TimeZoneInfo.ConvertTimeToUtc(start, timeZoneInfo);
            end = TimeZoneInfo.ConvertTimeToUtc(end, timeZoneInfo);

            var diff = end - start;

            // The function DateDiff only returns a whole number of the units being subtracted, and the precision is given in the unit specified.
            switch (units.Value.ToLower())
            {
                case "milliseconds":
                    var milliseconds = Math.Floor(diff.TotalMilliseconds);
                    return new NumberValue(irContext, milliseconds);
                case "seconds":
                    var seconds = Math.Floor(diff.TotalSeconds);
                    return new NumberValue(irContext, seconds);
                case "minutes":
                    var minutes = Math.Floor(diff.TotalMinutes);
                    return new NumberValue(irContext, minutes);
                case "hours":
                    var hours = Math.Floor(diff.TotalHours);
                    return new NumberValue(irContext, hours);
                case "days":
                    var days = Math.Floor(diff.TotalDays);
                    return new NumberValue(irContext, days);
                default:
                    // TODO: Task 10723372: Implement Unit Functionality in DateAdd, DateDiff Functions
                    return CommonErrors.NotYetImplementedError(irContext, "DateDiff Only supports Days for the unit field");
            }
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-datetime-parts
        public static FormulaValue Year(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                // TODO: Standardize the number 0 - year 1900 logic
                return new NumberValue(irContext, 1900);
            }

            DateTime arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value;
                    break;
                case DateValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var x = arg0.Year;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Day(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 0);
            }

            DateTime arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value;
                    break;
                case DateValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var x = arg0.Day;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Month(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 1);
            }

            DateTime arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value;
                    break;
                case DateValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var x = arg0.Month;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Hour(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 0);
            }

            TimeSpan arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value.TimeOfDay;
                    break;
                case TimeValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var x = arg0.Hours;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Minute(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 0);
            }

            TimeSpan arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value.TimeOfDay;
                    break;
                case TimeValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var x = arg0.Minutes;
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Second(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new NumberValue(irContext, 0);
            }

            TimeSpan arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value.TimeOfDay;
                    break;
                case TimeValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var x = arg0.Seconds;
            return new NumberValue(irContext, x);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-date-time
        // Date(Year,Month,Day)
        public static FormulaValue Date(IRContext irContext, NumberValue[] args)
        {
            // $$$ fix impl
            var year = (int)args[0].Value;
            var month = (int)args[1].Value;
            var day = (int)args[2].Value;

            return DateImpl(irContext, year, month, day);
        }

        private static FormulaValue DateImpl(IRContext irContext, int year, int month, int day)
        {
            // The final date is built up this way to allow for inputs which overflow,
            // such as: Date(2000, 25, 69) -> 3/10/2002
            var result = new DateTime(year, 1, 1)
                .AddMonths(month - 1)
                .AddDays(day - 1);

            return new DateValue(irContext, result);
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

            return new TimeValue(irContext, result);
        }

        public static FormulaValue DateTimeFunction(IRContext irContext, NumberValue[] args)
        {
            var year = (int)args[0].Value;
            var month = (int)args[1].Value;
            var day = (int)args[2].Value;
            var date = DateImpl(IRContext.NotInSource(FormulaType.Date), year, month, day);

            var hour = (int)args[3].Value;
            var minute = (int)args[4].Value;
            var second = (int)args[5].Value;
            var millisecond = (int)args[6].Value;
            var time = TimeImpl(IRContext.NotInSource(FormulaType.Time), hour, minute, second, millisecond);

            var result = AddDateAndTime(irContext, new[] { date, time });

            return result;
        }

        private static FormulaValue Now(IRContext irContext, FormulaValue[] args)
        {
            return new DateTimeValue(irContext, DateTime.Now);
        }

        private static FormulaValue DateParse(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
        {
            var str = args[0].Value;
            if (DateTime.TryParse(str, runner.CultureInfo, DateTimeStyles.None, out var result))
            {
                return new DateValue(irContext, result.Date);
            }
            else
            {
                return CommonErrors.InvalidDateTimeError(irContext);
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

        public static FormulaValue DateTimeParse(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
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

            if (DateTime.TryParse(str, culture, DateTimeStyles.None, out var result))
            {
                if (runner.TryGetService<TimeZoneInfo>(out var tzi) && result.Kind == DateTimeKind.Local)
                {
                    result = TimeZoneInfo.ConvertTime(result, TimeZoneInfo.Local, tzi);
                }

                return new DateTimeValue(irContext, result);
            }
            else
            {
                return CommonErrors.InvalidDateTimeError(irContext);
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
                return CommonErrors.InvalidDateTimeError(irContext);
            }
        }

        // Returns the number of minutes between UTC and either local or defined time zone
        public static FormulaValue TimeZoneOffset(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var tzInfo = runner.GetService<TimeZoneInfo>() ?? TimeZoneInfo.Local;

            if (args.Length == 0)
            {
                var tzOffsetDays = tzInfo.GetUtcOffset(DateTime.Now).TotalDays;
                return new NumberValue(irContext, tzOffsetDays * -1440);
            }

            switch (args[0])
            {
                case DateTimeValue dtv:
                    return new NumberValue(irContext, tzInfo.GetUtcOffset(dtv.Value).TotalDays * -1440);
                case DateValue dv:
                    return new NumberValue(irContext, tzInfo.GetUtcOffset(dv.Value).TotalDays * -1440);
                default:
                    return CommonErrors.InvalidDateTimeError(irContext);
            }
        }
    }
}
