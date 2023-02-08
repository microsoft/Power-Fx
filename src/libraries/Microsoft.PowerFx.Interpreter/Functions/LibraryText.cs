// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    // Due to .Net static ctor initialization, must place in a separate class from Library. 
    internal static class LibraryFlags
    {
        public static readonly RegexOptions RegExFlags = RegexOptions.Compiled | RegexOptions.CultureInvariant;
    }

    internal static partial class Library
    {
        private static readonly RegexOptions RegExFlags = LibraryFlags.RegExFlags;

        private static readonly Regex _ampmReplaceRegex = new Regex("[aA][mM]\\/[pP][mM]", RegExFlags);
        private static readonly Regex _apReplaceRegex = new Regex("[aA]\\/[pP]", RegExFlags);
        private static readonly Regex _minutesBeforeSecondsRegex = new Regex("[mM][^dDyYhH]+[sS]", RegExFlags);
        private static readonly Regex _minutesAfterHoursRegex = new Regex("[hH][^dDyYmM]+[mM]", RegExFlags);
        private static readonly Regex _minutesRegex = new Regex("[mM]", RegExFlags);
        private static readonly Regex _internalStringRegex = new Regex("([\"][^\"]*[\"])", RegExFlags);
        private static readonly Regex _daysDetokenizeRegex = new Regex("[\u0004][\u0004][\u0004][\u0004]+", RegExFlags);
        private static readonly Regex _monthsDetokenizeRegex = new Regex("[\u0003][\u0003][\u0003][\u0003]+", RegExFlags);
        private static readonly Regex _yearsDetokenizeRegex = new Regex("[\u0005][\u0005][\u0005]+", RegExFlags);
        private static readonly Regex _years2DetokenizeRegex = new Regex("[\u0005]+", RegExFlags);
        private static readonly Regex _hoursDetokenizeRegex = new Regex("[\u0006][\u0006]+", RegExFlags);
        private static readonly Regex _minutesDetokenizeRegex = new Regex("[\u000A][\u000A]+", RegExFlags);
        private static readonly Regex _secondsDetokenizeRegex = new Regex("[\u0008][\u0008]+", RegExFlags);
        private static readonly Regex _milisecondsDetokenizeRegex = new Regex("[\u000e]+", RegExFlags);

        // Char is used for PA string escaping 
        public static FormulaValue Char(IRContext irContext, NumberValue[] args)
        {
            var arg0 = args[0];

            if (arg0.Value < 1 || arg0.Value >= 256)
            {
                return CommonErrors.InvalidCharValue(irContext);
            }

            var str = new string((char)arg0.Value, 1);
            return new StringValue(irContext, str);
        }

        public static async ValueTask<FormulaValue> Concat(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {// Streaming 
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];
            var separator = args.Length > 2 ? ((StringValue)args[2]).Value : string.Empty;

            var sb = new StringBuilder();
            var first = true;

            foreach (var row in arg0.Rows)
            {
                if (row.IsValue)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(separator);
                    }

                    var childContext = context.SymbolContext.WithScopeValues(row.Value);

                    var result = await arg1.EvalInRowScopeAsync(context.NewScope(childContext));

                    string str;
                    if (result is ErrorValue ev)
                    {
                        return ev;
                    }
                    else if (result is BlankValue)
                    {
                        str = string.Empty;
                    }
                    else
                    {
                        str = ((StringValue)result).Value;
                    }

                    sb.Append(str);
                }
            }

            return new StringValue(irContext, sb.ToString());
        }

        // Scalar
        // Operator & maps to this function call.
        public static FormulaValue Concatenate(IRContext irContext, StringValue[] args)
        {
            var sb = new StringBuilder();

            foreach (var arg in args)
            {
                sb.Append(arg.Value);
            }

            return new StringValue(irContext, sb.ToString());
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static FormulaValue Value(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return Value(CreateFormattingInfo(runner), irContext, args);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static FormulaValue Value(FormattingInfo formatInfo, IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is StringValue sv)
            {
                if (string.IsNullOrEmpty(sv.Value))
                {
                    return new BlankValue(irContext);
                }
            }

            // culture will have Cultural info in case one was passed in argument else it will have the default one.
            var culture = formatInfo.CultureInfo;
            if (args.Length > 1)
            {
                if (args[1] is StringValue cultureArg && !TryGetCulture(cultureArg.Value, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, cultureArg.Value);
                }

                formatInfo.CultureInfo = culture;
            }

            NumberValue result = Value(formatInfo, irContext, args[0]);

            return result == null ? CommonErrors.ArgumentOutOfRange(irContext) : result;
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static NumberValue Value(FormattingInfo formatInfo, IRContext irContext, FormulaValue value)
        {
            switch (value)
            {
                case NumberValue n:
                    return n;
                case BooleanValue b:
                    return BooleanToNumber(irContext, b);
                case DateValue dv:
                    return DateToNumber(formatInfo, irContext, dv);
                case DateTimeValue dtv:
                    return DateTimeToNumber(formatInfo, irContext, dtv);
                case StringValue sv:
                    var (val, err) = ConvertToNumber(sv.Value, formatInfo.CultureInfo);

                    if (err == ConvertionStatus.Ok)
                    {
                        return new NumberValue(irContext, val);
                    }

                    break;
            }

            return null;
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-text
        public static FormulaValue Text(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {            
            return Text(CreateFormattingInfo(runner), irContext, args);
        }

        public static FormulaValue Text(FormattingInfo formatInfo, IRContext irContext, FormulaValue[] args)
        {
            const int formatSize = 100;
            string formatString = null;

            if (args.Length > 1 && args[1] is StringValue fs)
            {
                formatString = fs.Value;
            }

            var culture = formatInfo.CultureInfo;
            if (args.Length > 2 && args[2] is StringValue languageCode)
            {
                if (!TryGetCulture(languageCode.Value, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, languageCode.Value);
                }

                formatInfo.CultureInfo = culture;
            }

            // We limit the format string size
            if (formatString != null && formatString.Length > formatSize)
            {
                var customErrorMessage = StringResources.Get(TexlStrings.ErrTextFormatTooLarge, culture.Name);
                return CommonErrors.GenericInvalidArgument(irContext, string.Format(customErrorMessage, formatSize));
            }

            if (formatString != null && !TextFormatUtils.IsValidFormatArg(formatString, out bool hasDateTimeFmt, out bool hasNumberFmt))
            {
                var customErrorMessage = StringResources.Get(TexlStrings.ErrIncorrectFormat_Func, culture.Name);
                return CommonErrors.GenericInvalidArgument(irContext, string.Format(customErrorMessage, "Text"));
            }

            var result = Text(formatInfo, irContext, args[0], formatString);

            return result == null ? CommonErrors.GenericInvalidArgument(irContext, StringResources.Get(TexlStrings.ErrTextInvalidFormat, culture.Name)) : result;
        }

        public static StringValue Text(FormattingInfo formatInfo, IRContext irContext, FormulaValue value, string formatString = null)
        {
            var timeZoneInfo = formatInfo.TimeZoneInfo;
            var culture = formatInfo.CultureInfo;
            var hasDateTimeFmt = false;
            var hasNumberFmt = false;
            string resultString = null;

            if (formatString != null && !TextFormatUtils.IsValidFormatArg(formatString, out hasDateTimeFmt, out hasNumberFmt))
            {
                return null;
            }

            switch (value)
            {
                case StringValue sv:
                    return sv;
                case NumberValue num:
                    if (formatString != null && hasDateTimeFmt)
                    {
                        // It's a number, formatted as date/time. Let's convert it to a date/time value first
                        var newDateTime = Library.NumberToDateTime(formatInfo, IRContext.NotInSource(FormulaType.DateTime), num);
                        return ExpandDateTimeExcelFormatSpecifiersToStringValue(irContext, formatString, "g", newDateTime.GetConvertedValue(timeZoneInfo), culture, formatInfo.CancellationToken);
                    }
                    else
                    {
                        resultString = num.Value.ToString(formatString ?? "g", culture);
                    }

                    break;                
                case DateTimeValue dateTimeValue:
                    if (formatString != null && hasNumberFmt)
                    {
                        // It's a datetime, formatted as number. Let's convert it to a number value first
                        var newNumber = Library.DateTimeToNumber(formatInfo, IRContext.NotInSource(FormulaType.Number), dateTimeValue);
                        resultString = newNumber.Value.ToString(formatString, culture);
                    }
                    else
                    {
                        return ExpandDateTimeExcelFormatSpecifiersToStringValue(irContext, formatString, "g", dateTimeValue.GetConvertedValue(timeZoneInfo), culture, formatInfo.CancellationToken);
                    }

                    break;
                case DateValue dateValue:
                    if (formatString != null && hasNumberFmt)
                    {
                        NumberValue newDateNumber = Library.DateToNumber(formatInfo, IRContext.NotInSource(FormulaType.Number), dateValue) as NumberValue;
                        resultString = newDateNumber.Value.ToString(formatString, culture);
                    }
                    else
                    {
                        return ExpandDateTimeExcelFormatSpecifiersToStringValue(irContext, formatString, "d", dateValue.GetConvertedValue(timeZoneInfo), culture, formatInfo.CancellationToken);
                    }

                    break;
                case TimeValue timeValue:
                    if (formatString != null && hasNumberFmt)
                    {
                        var newNumber = Library.TimeToNumber(IRContext.NotInSource(FormulaType.Number), new TimeValue[] { timeValue });
                        resultString = newNumber.Value.ToString(formatString, culture);
                    }
                    else
                    {
                        var dtValue = Library.TimeToDateTime(formatInfo, IRContext.NotInSource(FormulaType.DateTime), timeValue);
                        return ExpandDateTimeExcelFormatSpecifiersToStringValue(irContext, formatString, "t", dtValue.GetConvertedValue(timeZoneInfo), culture, formatInfo.CancellationToken);
                    }

                    break;
                case BooleanValue b:
                    resultString = b.Value.ToString().ToLower();
                    break;
            }

            return resultString == null ? null : new StringValue(irContext, resultString);
        }

        internal static FormulaValue ExpandDateTimeExcelFormatSpecifiers(IRContext irContext, string format, string defaultFormat, DateTime dateTime, CultureInfo culture, CancellationToken cancellationToken)
        {
            StringValue result = ExpandDateTimeExcelFormatSpecifiersToStringValue(irContext, format, defaultFormat, dateTime, culture, cancellationToken);

            return result == null ? CommonErrors.GenericInvalidArgument(irContext, StringResources.Get(TexlStrings.ErrTextInvalidFormat, culture.Name)) : result;
        }

        internal static StringValue ExpandDateTimeExcelFormatSpecifiersToStringValue(IRContext irContext, string format, string defaultFormat, DateTime dateTime, CultureInfo culture, CancellationToken cancellationToken)
        {
            if (format == null)
            {
                return new StringValue(irContext, dateTime.ToString(defaultFormat, culture));
            }

            // DateTime format
            switch (format.ToLower())
            {
                case "'shortdatetime24'":
                case "'shortdatetime'":
                case "'shorttime24'":
                case "'shorttime'":
                case "'shortdate'":
                case "'longdatetime24'":
                case "'longdatetime'":
                case "'longtime24'":
                case "'longtime'":
                case "'longdate'":
                    return new StringValue(irContext, dateTime.ToString(ExpandDateTimeFormatSpecifiers(format, culture)));
                default:
                    try
                    {
                        var stringResult = ResolveDateTimeFormatAmbiguities(format, dateTime, culture, cancellationToken);
                        return new StringValue(irContext, stringResult);
                    }
                    catch (FormatException)
                    {
                        return null;
                    }
            }
        }

        internal static string ExpandDateTimeFormatSpecifiers(string format, CultureInfo culture)
        {
            var info = DateTimeFormatInfo.GetInstance(culture);

            switch (format.ToLower())
            {
                case "'shortdatetime24'":
                    // TODO: This might be wrong for some cultures
                    return ReplaceWith24HourClock(info.ShortDatePattern + " " + info.ShortTimePattern);
                case "'shortdatetime'":
                    // TODO: This might be wrong for some cultures
                    return info.ShortDatePattern + " " + info.ShortTimePattern;
                case "'shorttime24'":
                    return ReplaceWith24HourClock(info.ShortTimePattern);
                case "'shorttime'":
                    return info.ShortTimePattern;
                case "'shortdate'":
                    return info.ShortDatePattern;
                case "'longdatetime24'":
                    return ReplaceWith24HourClock(info.FullDateTimePattern);
                case "'longdatetime'":
                    return info.FullDateTimePattern;
                case "'longtime24'":
                    return ReplaceWith24HourClock(info.LongTimePattern);
                case "'longtime'":
                    return info.LongTimePattern;
                case "'longdate'":
                    return info.LongDatePattern;
                case "'utc'":
                    return info.UniversalSortableDateTimePattern;
                default:
                    return format;
            }
        }

        private static string ReplaceWith24HourClock(string format)
        {
            format = Regex.Replace(format, "[hH]", "H");
            format = Regex.Replace(format, "t+", string.Empty);

            return format.Trim();
        }

        private static string ResolveDateTimeFormatAmbiguities(string format, DateTime dateTime, CultureInfo culture, CancellationToken cancellationToken)
        {
            var resultString = format;

            resultString = ReplaceDoubleQuotedStrings(resultString, out var replaceList, cancellationToken);
            resultString = TokenizeDatetimeFormat(resultString, cancellationToken);
            resultString = DetokenizeDatetimeFormat(resultString, dateTime, culture);
            resultString = RestoreDoubleQuotedStrings(resultString, replaceList, cancellationToken);

            return resultString;
        }

        private static string RestoreDoubleQuotedStrings(string format, List<string> replaceList, CancellationToken cancellationToken)
        {
            var stringReplaceRegex = new Regex("\u0011");
            var array = replaceList.ToArray();
            var index = 0;

            var match = stringReplaceRegex.Match(format);

            while (match.Success)
            {
                cancellationToken.ThrowIfCancellationRequested();

                format = format.Substring(0, match.Index) + array[index++].Replace("\"", string.Empty) + format.Substring(match.Index + match.Length);
                match = stringReplaceRegex.Match(format);
            }

            return format;
        }

        private static string ReplaceDoubleQuotedStrings(string format, out List<string> replaceList, CancellationToken cancellationToken)
        {
            var ret = string.Empty;

            replaceList = new List<string>();

            foreach (Match match in _internalStringRegex.Matches(format))
            {
                cancellationToken.ThrowIfCancellationRequested();

                replaceList.Add(match.Value);
            }

            return _internalStringRegex.Replace(format, "\u0011");
        }

        private static string DetokenizeDatetimeFormat(string format, DateTime dateTime, CultureInfo culture)
        {
            var hasAmPm = format.Contains('\u0001') || format.Contains('\u0002');

            // Day component            
            format = _daysDetokenizeRegex.Replace(format, dateTime.ToString("dddd", culture))
                          .Replace("\u0004\u0004\u0004", dateTime.ToString("ddd", culture))
                          .Replace("\u0004\u0004", dateTime.ToString("dd", culture))
                          .Replace("\u0004", dateTime.ToString("%d", culture));

            // Month component
            format = _monthsDetokenizeRegex.Replace(format, dateTime.ToString("MMMM", culture))
                          .Replace("\u0003\u0003\u0003", dateTime.ToString("MMM", culture))
                          .Replace("\u0003\u0003", dateTime.ToString("MM", culture))
                          .Replace("\u0003", dateTime.ToString("%M", culture));

            // Year component
            format = _yearsDetokenizeRegex.Replace(format, dateTime.ToString("yyyy", culture));
            format = _years2DetokenizeRegex.Replace(format, dateTime.ToString("yy", culture));

            // Hour component
            format = _hoursDetokenizeRegex.Replace(format, hasAmPm ? dateTime.ToString("hh", culture) : dateTime.ToString("HH", culture))
                          .Replace("\u0006", hasAmPm ? dateTime.ToString("%h", culture) : dateTime.ToString("%H", culture));

            // Minute component
            format = _minutesDetokenizeRegex.Replace(format, dateTime.ToString("mm", culture))
                          .Replace("\u000A", dateTime.ToString("%m", culture));

            // Second component
            format = _secondsDetokenizeRegex.Replace(format, dateTime.ToString("ss", culture))
                          .Replace("\u0008", dateTime.ToString("%s", culture));

            // Milliseconds component
            format = _milisecondsDetokenizeRegex.Replace(format, match =>
            {
                var len = match.Groups[0].Value.Length;
                var subSecondFormat = len == 1 ? "%f" : new string('f', len);
                return dateTime.ToString(subSecondFormat, culture);
            });

            // AM/PM component
            format = format.Replace("\u0001", dateTime.ToString("tt", culture))
                           .Replace("\u0002", dateTime.ToString("%t", culture).ToLower());

            return format;
        }

        private static string TokenizeDatetimeFormat(string format, CancellationToken cancellationToken)
        {
            // Temporary replacements to avoid collisions with upcoming month names, etc.
            format = _ampmReplaceRegex.Replace(format, "\u0001");
            format = _apReplaceRegex.Replace(format, "\u0002");

            // Find all "m" chars for minutes, before seconds
            var match = _minutesBeforeSecondsRegex.Match(format);
            while (match.Success)
            {
                cancellationToken.ThrowIfCancellationRequested();

                format = format.Substring(0, match.Index) + "\u000A" + format.Substring(match.Index + 1);
                match = _minutesBeforeSecondsRegex.Match(format);
            }

            // Find all "m" chars for minutes, after hours
            match = _minutesAfterHoursRegex.Match(format);
            while (match.Success)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var afterHourFormat = format.Substring(match.Index);
                var minuteAfterHourPosition = _minutesRegex.Match(afterHourFormat);
                var pos = match.Index + minuteAfterHourPosition.Index;

                format = format.Substring(0, pos) + "\u000A" + format.Substring(pos + 1);

                match = _minutesAfterHoursRegex.Match(format);
            }

            var sb = new StringBuilder();
            foreach (var c in format)
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (c)
                {
                    case 'm': case 'M': sb.Append('\u0003'); break;
                    case 'd': case 'D': sb.Append('\u0004'); break;
                    case 'y': case 'Y': sb.Append('\u0005'); break;
                    case 'h': case 'H': sb.Append('\u0006'); break;
                    case 's': case 'S': sb.Append('\u0008'); break;
                    case '0': sb.Append('\u000E'); break;
                    default: sb.Append(c); break;
                }
            }

            return sb.ToString();
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-isblank-isempty
        // Take first non-blank value.
        public static async ValueTask<FormulaValue> Coalesce(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            foreach (var arg in args)
            {
                runner.CheckCancel();

                var res = await runner.EvalArgAsync<ValidFormulaValue>(arg, context, arg.IRContext);

                if (res.IsValue)
                {
                    var val = res.Value;
                    if (!(val is StringValue str && str.Value == string.Empty))
                    {
                        return res.ToFormulaValue();
                    }
                }

                if (res.IsError)
                {
                    return res.Error;
                }
            }

            return new BlankValue(irContext);
        }

        public static FormulaValue Lower(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
        {
            return new StringValue(irContext, runner.CultureInfo.TextInfo.ToLower(args[0].Value));
        }

        public static FormulaValue Upper(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
        {
            return new StringValue(irContext, runner.CultureInfo.TextInfo.ToUpper(args[0].Value));
        }

        public static FormulaValue Proper(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
        {
            return new StringValue(irContext, runner.CultureInfo.TextInfo.ToTitleCase(runner.CultureInfo.TextInfo.ToLower(args[0].Value)));
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-len
        public static FormulaValue Len(IRContext irContext, StringValue[] args)
        {
            return new NumberValue(irContext, args[0].Value.Length);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-left-mid-right
        public static FormulaValue Mid(IRContext irContext, FormulaValue[] args)
        {
            var errors = new List<ErrorValue>();
            var start = (NumberValue)args[1];
            if (double.IsNaN(start.Value) || double.IsInfinity(start.Value) || start.Value <= 0)
            {
                errors.Add(CommonErrors.ArgumentOutOfRange(start.IRContext));
            }

            var count = (NumberValue)args[2];
            if (double.IsNaN(count.Value) || double.IsInfinity(count.Value) || count.Value < 0)
            {
                errors.Add(CommonErrors.ArgumentOutOfRange(count.IRContext));
            }

            if (errors.Count != 0)
            {
                return ErrorValue.Combine(irContext, errors);
            }

            var source = (StringValue)args[0];
            var start0Based = (int)(start.Value - 1);
            if (source.Value == string.Empty || start0Based >= source.Value.Length)
            {
                return new StringValue(irContext, string.Empty);
            }

            var minCount = Math.Min((int)count.Value, source.Value.Length - start0Based);
            var result = source.Value.Substring(start0Based, minCount);

            return new StringValue(irContext, result);
        }

        public static FormulaValue Left(IRContext irContext, FormulaValue[] args)
        {
            return LeftOrRight(irContext, args, Left);
        }

        public static FormulaValue Right(IRContext irContext, FormulaValue[] args)
        {
            return LeftOrRight(irContext, args, Right);
        }

        private static string Left(string str, int i)
        {
            if (i >= str.Length)
            {
                return str;
            }

            return str.Substring(0, i);
        }

        private static string Right(string str, int i)
        {
            if (i >= str.Length)
            {
                return str;
            }

            return str.Substring(str.Length - i);
        }

        private static FormulaValue LeftOrRight(IRContext irContext, FormulaValue[] args, Func<string, int, string> leftOrRight)
        {
            if (args[0] is BlankValue || args[1] is BlankValue)
            {
                return new StringValue(irContext, string.Empty);
            }

            if (args[1] is not NumberValue count)
            {
                return CommonErrors.GenericInvalidArgument(irContext);
            }

            var source = (StringValue)args[0];

            if (count.Value < 0)
            {
                return CommonErrors.GenericInvalidArgument(irContext);
            }

            return new StringValue(irContext, leftOrRight(source.Value, (int)count.Value));
        }

        private static FormulaValue Find(IRContext irContext, FormulaValue[] args)
        {
            var findText = (StringValue)args[0];
            var withinText = (StringValue)args[1];
            var startIndexValue = (int)((NumberValue)args[2]).Value;

            if (startIndexValue < 1 || startIndexValue > withinText.Value.Length + 1)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            var index = withinText.Value.IndexOf(findText.Value, startIndexValue - 1);
            return index >= 0 ? new NumberValue(irContext, index + 1)
                              : new BlankValue(irContext);
        }

        private static FormulaValue Replace(IRContext irContext, FormulaValue[] args)
        {
            var source = ((StringValue)args[0]).Value;
            var start = ((NumberValue)args[1]).Value;
            var count = ((NumberValue)args[2]).Value;
            var replacement = ((StringValue)args[3]).Value;

            if (start <= 0 || count < 0)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            if (start >= int.MaxValue)
            {
                start = source.Length + 1;
            }

            var start0Based = (int)(start - 1);
            var prefix = start0Based < source.Length ? source.Substring(0, start0Based) : source;

            var suffixIndex = start0Based + (int)count;
            var suffix = suffixIndex < source.Length ? source.Substring(suffixIndex) : string.Empty;
            var result = prefix + replacement + suffix;

            return new StringValue(irContext, result);
        }

        public static FormulaValue Split(IRContext irContext, StringValue[] args)
        {
            var text = args[0].Value;
            var separator = args[1].Value;

            // The separator can be zero, one, or more characters that are matched as a whole in the text string. Using a zero length or blank
            // string results in each character being broken out individually.
            var substrings = string.IsNullOrEmpty(separator) ? text.Select(c => new string(c, 1)) : text.Split(new string[] { separator }, StringSplitOptions.None);
            var rows = substrings.Select(s => new StringValue(IRContext.NotInSource(FormulaType.String), s));

            return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows.ToArray(), forceSingleColumn: true));
        }

        // This is static analysis before actually executing, so just use string lengths and avoid contents. 
        internal static int SubstituteGetResultLength(
            int sourceLen, int matchLen, int replacementLen, bool replaceAll)
        {
            int maxLenChars;

            if (matchLen > sourceLen)
            {
                // Match is too large, can't be found.
                // So will not match and just return original.
                return sourceLen;
            }

            if (replaceAll)
            {
                // Replace all instances. 
                // Maximum possible length of Substitute, convert all the Match to Replacement. 
                // Unicode, so 2B per character.
                if (matchLen == 0)
                {
                    maxLenChars = sourceLen;
                }
                else
                {
                    // Round up as conservative estimate. 
                    maxLenChars = (int)Math.Ceiling((double)sourceLen / matchLen) * replacementLen;
                }
            }
            else
            {
                // Only replace 1 instance 
                maxLenChars = sourceLen - matchLen + replacementLen;
            }

            // If not match found, will still be source length 
            return Math.Max(sourceLen,  maxLenChars);
        }

        private static FormulaValue Substitute(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var source = (StringValue)args[0];
            var match = (StringValue)args[1];
            var replacement = (StringValue)args[2];

            var instanceNum = -1;
            if (args.Length > 3)
            {
                var nv = (NumberValue)args[3];
                if (nv.Value > source.Value.Length)
                {
                    return source;
                }

                instanceNum = (int)nv.Value;
            }

            // Compute max possible memory this operation may need.
            // Compute max possible memory this operation may need.
            var sourceLen = source.Value.Length;
            var matchLen = match.Value.Length;
            var replacementLen = replacement.Value.Length;

            var maxLenChars = SubstituteGetResultLength(sourceLen, matchLen, replacementLen, instanceNum < 0);
            runner.Governor.CanAllocateString(maxLenChars);

            var result = SubstituteWorker(runner, irContext, source, match, replacement, instanceNum);

            Contracts.Assert(result.Value.Length <= maxLenChars);

            return result;
        }

        private static StringValue SubstituteWorker(EvalVisitor eval, IRContext irContext, StringValue source, StringValue match, StringValue replacement, int instanceNum)
        {
            if (string.IsNullOrEmpty(match.Value))
            {
                return source;
            }

            var sourceValue = source.Value;
            var idx = sourceValue.IndexOf(match.Value);
            if (instanceNum < 0)
            {
                while (idx >= 0)
                {
                    eval.CheckCancel();

                    var temp = sourceValue.Substring(0, idx) + replacement.Value;
                    sourceValue = sourceValue.Substring(idx + match.Value.Length);
                    var idx2 = sourceValue.IndexOf(match.Value);
                    if (idx2 < 0)
                    {
                        idx = idx2;
                    }
                    else
                    {
                        idx = temp.Length + idx2;
                    }

                    sourceValue = temp + sourceValue;
                }
            }
            else
            {
                var num = 0;
                while (idx >= 0 && ++num < instanceNum)
                {
                    eval.CheckCancel();

                    var idx2 = sourceValue.Substring(idx + match.Value.Length).IndexOf(match.Value);
                    if (idx2 < 0)
                    {
                        idx = idx2;
                    }
                    else
                    {
                        idx += match.Value.Length + idx2;
                    }
                }

                if (idx >= 0 && num == instanceNum)
                {
                    sourceValue = sourceValue.Substring(0, idx) + replacement.Value + sourceValue.Substring(idx + match.Value.Length);
                }
            }

            return new StringValue(irContext, sourceValue);
        }

        public static FormulaValue StartsWith(IRContext irContext, StringValue[] args)
        {
            var text = args[0];
            var start = args[1];

            return new BooleanValue(irContext, text.Value.StartsWith(start.Value, StringComparison.OrdinalIgnoreCase));
        }

        public static FormulaValue EndsWith(IRContext irContext, StringValue[] args)
        {
            var text = args[0];
            var end = args[1];

            return new BooleanValue(irContext, text.Value.EndsWith(end.Value, StringComparison.OrdinalIgnoreCase));
        }

        public static FormulaValue Trim(IRContext irContext, StringValue[] args)
        {
            var text = args[0];

            // Remove all whitespace except ASCII 10, 11, 12, 13 and 160, then trim to follow Excel's behavior
            var regex = new Regex(@"[^\S\xA0\n\v\f\r]+");

            var result = regex.Replace(text.Value, " ").Trim();

            return new StringValue(irContext, result);
        }

        public static FormulaValue TrimEnds(IRContext irContext, StringValue[] args)
        {
            var text = args[0];

            var result = text.Value.Trim();

            return new StringValue(irContext, result);
        }

        public static FormulaValue Guid(IRContext irContext, StringValue[] args)
        {
            var text = args[0].Value;
            try
            {
                var guid = new Guid(text);

                return new GuidValue(irContext, guid);
            }
            catch
            {
                return CommonErrors.GenericInvalidArgument(irContext);
            }
        }
    }
}
