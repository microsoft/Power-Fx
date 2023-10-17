﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    // Due to .Net static ctor initialization, must place in a separate class from Library. 
    internal static class LibraryFlags
    {
        public static readonly RegexOptions RegExFlags = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        public static readonly RegexOptions RegExFlags_IgnoreCase = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
    }

    internal static partial class Library
    {
        private static readonly RegexOptions RegExFlags = LibraryFlags.RegExFlags;
        private static readonly RegexOptions RegExFlags_IgnoreCase = LibraryFlags.RegExFlags_IgnoreCase;

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
        private static readonly Regex _tdTagRegex = new Regex("<\\s*(td)[\\s\\S]*?\\/{0,1}>", RegExFlags_IgnoreCase);
        private static readonly Regex _lineBreakTagRegex = new Regex("<\\s*(br|li)[\\s\\S]*?\\/{0,1}>", RegExFlags_IgnoreCase);
        private static readonly Regex _doubleLineBreakTagRegex = new Regex("<\\s*(div|p|tr)[\\s\\S]*?\\/{0,1}>", RegExFlags_IgnoreCase);
        private static readonly Regex _commentTagRegex = new Regex("<!--[\\s\\S]*?--\\s*>", RegExFlags_IgnoreCase);
        private static readonly Regex _headerTagRegex = new Regex("<\\s*(header)[\\s\\S]*?>[\\s\\S]*?<\\s*\\/\\s*(header)\\s*>", RegExFlags_IgnoreCase);
        private static readonly Regex _scriptTagRegex = new Regex("<\\s*(script)[\\s\\S]*?>[\\s\\S]*?<\\s*\\/\\s*(script)\\s*>", RegExFlags_IgnoreCase);
        private static readonly Regex _styleTagRegex = new Regex("<\\s*(style)[\\s\\S]*?>[\\s\\S]*?<\\s*\\/\\s*(style)\\s*>", RegExFlags_IgnoreCase);
        private static readonly Regex _htmlTagsRegex = new Regex("<[^\\>]*\\>", RegExFlags_IgnoreCase);

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
        {
            // Streaming 
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];
            var separator = args.Length > 2 ? ((StringValue)args[2]).Value : string.Empty;

            var sb = new StringBuilder();
            var first = true;

            foreach (var row in arg0.Rows)
            {
                runner.CheckCancel();

                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(separator);
                }

                SymbolContext childContext = context.SymbolContext.WithScopeValues(row.ToFormulaValue());

                var result = await arg1.EvalInRowScopeAsync(context.NewScope(childContext)).ConfigureAwait(false);

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
            return Value(runner.GetFormattingInfo(), irContext, args);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static FormulaValue Value(FormattingInfo formatInfo, IRContext irContext, FormulaValue[] args)
        {
            if (irContext.ResultType is DecimalType)
            {
                return Decimal(formatInfo, irContext, args);
            }
            else
            {
                return Float(formatInfo, irContext, args);
            }
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static FormulaValue Float(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return Float(runner.GetFormattingInfo(), irContext, args);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static FormulaValue Float(FormattingInfo formatInfo, IRContext irContext, FormulaValue[] args)
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
                if (args[1] is StringValue cultureArg && !TextFormatUtils.TryGetCulture(cultureArg.Value, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, cultureArg.Value);
                }
            }

            bool isValue = TryFloat(formatInfo.With(culture), irContext, args[0], out NumberValue result);

            return isValue ? result : CommonErrors.CanNotConvertToNumber(irContext, args[0]);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static bool TryFloat(FormattingInfo formatInfo, IRContext irContext, FormulaValue value, out NumberValue result)
        {
            result = null;

            Contract.Assert(NumberValue.AllowedListConvertToNumber.Contains(value.Type));

            switch (value)
            {
                case NumberValue n:
                    result = n;
                    break;
                case DecimalValue w:
                    result = DecimalToNumber(irContext, w);
                    break;
                case BooleanValue b:
                    result = BooleanToNumber(irContext, b);
                    break;
                case DateValue dv:
                    result = DateToNumber(formatInfo, irContext, dv);
                    break;
                case DateTimeValue dtv:
                    result = DateTimeToNumber(formatInfo, irContext, dtv);
                    break;
                case StringValue sv:
                    var (val, err) = ConvertToNumber(sv.Value, formatInfo.CultureInfo);

                    if (err == ConvertionStatus.Ok)
                    {
                        result = new NumberValue(irContext, val);
                    }

                    break;
            }

            return result != null;
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static FormulaValue Decimal(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return Decimal(runner.GetFormattingInfo(), irContext, args);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static FormulaValue Decimal(FormattingInfo formatInfo, IRContext irContext, FormulaValue[] args)
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
                if (args[1] is StringValue cultureArg && !TextFormatUtils.TryGetCulture(cultureArg.Value, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, cultureArg.Value);
                }
            }

            bool isValue = TryDecimal(formatInfo.With(culture), irContext, args[0], out DecimalValue result);

            return isValue ? result : CommonErrors.CanNotConvertToNumber(irContext, args[0]);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-value
        // Convert string to number
        public static bool TryDecimal(FormattingInfo formatInfo, IRContext irContext, FormulaValue value, out DecimalValue result)
        {
            result = null;

            Contract.Assert(DecimalValue.AllowedListConvertToDecimal.Contains(value.Type));

            switch (value)
            {
                case NumberValue n:
                    var (num, numErr) = ConvertNumberToDecimal(n.Value);
                    if (numErr == ConvertionStatus.Ok)
                    {
                        result = new DecimalValue(irContext, num);
                    }

                    break;
                case DecimalValue w:
                    result = w;
                    break;
                case BooleanValue b:
                    result = BooleanToDecimal(irContext, b);
                    break;
                case DateValue dv:
                    result = DateToDecimal(formatInfo, irContext, dv);
                    break;
                case DateTimeValue dtv:
                    result = DateTimeToDecimal(formatInfo, irContext, dtv);
                    break;
                case StringValue sv:
                    var (str, strErr) = ConvertToDecimal(sv.Value, formatInfo.CultureInfo);

                    if (strErr == ConvertionStatus.Ok)
                    {
                        result = new DecimalValue(irContext, str);
                    }

                    break;
            }

            return result != null;
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-text
        public static FormulaValue Text(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            if (args[0].IsBlank())
            {
                if (args.Length == 1)
                {
                    // When used as a pure conversion function (single argument, no format string), this function propagates null values
                    return new BlankValue(irContext);
                }

                // Set blank to 0 because we only support numeric and datetime input when we have format string
                args[0] = new NumberValue(IRContext.NotInSource(FormulaType.Number), 0);
            }

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].IsBlank())
                {
                    return new StringValue(irContext, string.Empty);
                }
            }

            runner.CancellationToken.ThrowIfCancellationRequested();
            return Text(runner.GetFormattingInfo(), irContext, args, runner.CancellationToken);
        }

        public static FormulaValue Text(FormattingInfo formatInfo, IRContext irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            const int formatSize = 100;
            string formatString = null;
            string defaultLanguage = Language(formatInfo.CultureInfo);
            var textFormatArgs = new TextFormatArgs
            {
                FormatCultureName = null,
                FormatArg = null,
                DateTimeFmt = DateTimeFmtType.NoDateTimeFormat,
                HasNumericFmt = false
            };

            if (args.Length > 1)
            {
                if (!TextFormatUtils.AllowedListToUseFormatString.Contains(args[0].Type._type))
                {
                    var customErrorMessage = StringResources.Get(TexlStrings.ErrNotSupportedFormat_Func, formatInfo.CultureInfo.Name);
                    return CommonErrors.GenericInvalidArgument(irContext, string.Format(CultureInfo.InvariantCulture, customErrorMessage, "Text"));
                }

                if (args[1] is StringValue fs)
                {
                    formatString = fs.Value;
                }
            }

            var culture = formatInfo.CultureInfo;
            if (args.Length > 2 && args[2] is StringValue languageCode)
            {
                if (!TextFormatUtils.TryGetCulture(languageCode.Value, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, languageCode.Value);
                }
            }

            // We limit the format string size
            if (formatString != null && formatString.Length > formatSize)
            {
                var customErrorMessage = StringResources.Get(TexlStrings.ErrTextFormatTooLarge, culture.Name);
                return CommonErrors.GenericInvalidArgument(irContext, string.Format(CultureInfo.InvariantCulture, customErrorMessage, formatSize));
            }

            if (formatString != null && !TextFormatUtils.IsValidFormatArg(formatString, culture, defaultLanguage, out textFormatArgs))
            {
                var customErrorMessage = StringResources.Get(TexlStrings.ErrIncorrectFormat_Func, culture.Name);
                return CommonErrors.GenericInvalidArgument(irContext, string.Format(CultureInfo.InvariantCulture, customErrorMessage, "Text"));
            }

            if (args.Length > 1 && args[1] is OptionSetValue ops)
            {
                textFormatArgs.FormatArg = ops.Option;
                textFormatArgs.DateTimeFmt = DateTimeFmtType.EnumDateTimeFormat;
            }

            bool isText = TryText(formatInfo.With(culture), irContext, args[0], textFormatArgs, cancellationToken, out StringValue result);

            return isText ? result : CommonErrors.GenericInvalidArgument(irContext, StringResources.Get(TexlStrings.ErrTextInvalidFormat, culture.Name));
        }

        public static bool TryText(FormattingInfo formatInfo, IRContext irContext, FormulaValue value, TextFormatArgs textFormatArgs, CancellationToken cancellationToken, out StringValue result)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var timeZoneInfo = formatInfo.TimeZoneInfo;
            var culture = formatInfo.CultureInfo;
            var formatString = textFormatArgs.FormatArg;
            result = null;

            // There is a difference between Windows 10 and 11 for French locale
            // We fix the thousand separator here to be consistent 
            if (culture.Name.Equals("fr-FR", StringComparison.OrdinalIgnoreCase) && culture.NumberFormat.NumberGroupSeparator == "\u00A0")
            {
                culture = (CultureInfo)culture.Clone();
                culture.NumberFormat.NumberGroupSeparator = "\u202F";
            }

            Contract.Assert(StringValue.AllowedListConvertToString.Contains(value.Type));

            switch (value)
            {
                case StringValue sv:
                    result = sv;
                    break;
                case BooleanValue b:
                    result = new StringValue(irContext, b.Value.ToString(culture).ToLowerInvariant());
                    break;
                case GuidValue g:
                    result = new StringValue(irContext, g.Value.ToString("d", CultureInfo.InvariantCulture));
                    break;
                case DecimalValue:
                case NumberValue:
                    NumberValue numberValue = value as NumberValue;
                    if (formatString != null && textFormatArgs.DateTimeFmt != DateTimeFmtType.NoDateTimeFormat)
                    {
                        // It's a number, formatted as date/time. Let's convert it to a date/time value first
                        if (value is DecimalValue decimalValue)
                        {
                            // Convert decimal to number
                            numberValue = new NumberValue(IRContext.NotInSource(FormulaType.Number), (double)decimalValue.Value);
                        }

                        var dateTimeResult = Library.NumberToDateTime(formatInfo, IRContext.NotInSource(FormulaType.DateTime), numberValue).GetConvertedValue(timeZoneInfo);

                        return textFormatArgs.DateTimeFmt == DateTimeFmtType.EnumDateTimeFormat ? TryExpandDateTimeFromEnumFormat(irContext, textFormatArgs, dateTimeResult, timeZoneInfo, culture, cancellationToken, out result) :
                            TryExpandDateTimeExcelFormatSpecifiersToStringValue(irContext, textFormatArgs, "g", dateTimeResult, timeZoneInfo, culture, cancellationToken, out result);
                    }
                    else
                    {
                        if (value is DecimalValue decimalValue)
                        {
                            result = new StringValue(irContext, formatString == string.Empty ? string.Empty : decimalValue.Normalize().ToString(formatString ?? "G", culture));
                        }
                        else
                        {
                            result = new StringValue(irContext, formatString == string.Empty ? string.Empty : numberValue.Value.ToString(formatString ?? "G", culture));
                        }
                    }

                    break;
                case DateTimeValue:
                case DateValue:
                case TimeValue:
                    if (formatString != null && textFormatArgs.HasNumericFmt)
                    {
                        NumberValue numberValueResult;

                        // It's a datetime, formatted as number. Let's convert it to a number value first
                        if (value is DateTimeValue dateTimeValue)
                        {
                            numberValueResult = Library.DateTimeToNumber(formatInfo, IRContext.NotInSource(FormulaType.Number), dateTimeValue);
                        }
                        else if (value is DateValue dateValue)
                        {
                            numberValueResult = Library.DateToNumber(formatInfo, IRContext.NotInSource(FormulaType.Number), dateValue) as NumberValue;
                        }
                        else
                        {
                            var timeValue = value as TimeValue;
                            numberValueResult = Library.TimeToNumber(IRContext.NotInSource(FormulaType.Number), new TimeValue[] { timeValue });
                        }

                        result = new StringValue(irContext, numberValueResult.Value.ToString(formatString, culture));
                    }
                    else
                    {
                        DateTime dateTimeResult;
                        string defaultFormat = "g";

                        if (value is DateTimeValue dateTimeValue)
                        {
                            dateTimeResult = dateTimeValue.GetConvertedValue(timeZoneInfo);
                        }
                        else if (value is DateValue dateValue)
                        {
                            dateTimeResult = dateValue.GetConvertedValue(timeZoneInfo);
                            defaultFormat = "d";
                        }
                        else
                        {
                            var timeValue = value as TimeValue;
                            dateTimeResult = Library.TimeToDateTime(formatInfo, IRContext.NotInSource(FormulaType.DateTime), timeValue).GetConvertedValue(timeZoneInfo);
                            defaultFormat = "t";
                        }

                        return textFormatArgs.DateTimeFmt == DateTimeFmtType.EnumDateTimeFormat ? TryExpandDateTimeFromEnumFormat(irContext, textFormatArgs, dateTimeResult, timeZoneInfo, culture, cancellationToken, out result) :
                            TryExpandDateTimeExcelFormatSpecifiersToStringValue(irContext, textFormatArgs, defaultFormat, dateTimeResult, timeZoneInfo, culture, cancellationToken, out result);
                    }

                    break;
            }

            return result != null;
        }

        internal static bool TryExpandDateTimeFromEnumFormat(IRContext irContext, TextFormatArgs textFormatArgs, DateTime dateTime, TimeZoneInfo timeZoneInfo, CultureInfo culture, CancellationToken cancellationToken, out StringValue result)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if DateTimeFormatEnumValue is DateTime format enum type.
            var info = DateTimeFormatInfo.GetInstance(culture);
            result = null;
            string formatStr;
            switch (textFormatArgs.FormatArg)
            {
                case "UTC":
                    result = new StringValue(irContext, ConvertToUTC(dateTime, timeZoneInfo).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", culture));
                    return result != null;
                case "ShortDateTime":
                    // TODO: This might be wrong for some cultures
                    formatStr = info.ShortDatePattern + " " + info.ShortTimePattern;
                    break;
                case "ShortDateTime24":
                    // TODO: This might be wrong for some cultures
                    formatStr = ReplaceWith24HourClock(info.ShortDatePattern + " " + info.ShortTimePattern);
                    break;
                case "ShortDate":
                    formatStr = info.ShortDatePattern;
                    break;
                case "ShortTime":
                    formatStr = info.ShortTimePattern;
                    break;
                case "ShortTime24":
                    formatStr = ReplaceWith24HourClock(info.ShortTimePattern);
                    break;
                case "LongDateTime":
                    formatStr = info.FullDateTimePattern;
                    break;
                case "LongDateTime24":
                    formatStr = ReplaceWith24HourClock(info.FullDateTimePattern);
                    break;
                case "LongDate":
                    formatStr = info.LongDatePattern;
                    break;
                case "LongTime":
                    formatStr = info.LongTimePattern;
                    break;
                case "LongTime24":
                    formatStr = ReplaceWith24HourClock(info.LongTimePattern);
                    break;
                default:
                    return false;
            }

            result = new StringValue(irContext, dateTime.ToString(formatStr, culture));
            return result != null;
        }

        internal static bool TryExpandDateTimeExcelFormatSpecifiersToStringValue(IRContext irContext, TextFormatArgs textFormatArgs, string defaultFormat, DateTime dateTime, TimeZoneInfo timeZoneInfo, CultureInfo culture, CancellationToken cancellationToken, out StringValue result)
        {
            cancellationToken.ThrowIfCancellationRequested();

            result = null;

            if (textFormatArgs.FormatArg == null)
            {
                result = new StringValue(irContext, dateTime.ToString(defaultFormat, culture));
                return true;
            }

            try
            {
                var stringResult = ResolveDateTimeFormatAmbiguities(textFormatArgs.FormatArg, dateTime, culture, cancellationToken);
                result = new StringValue(irContext, stringResult);
            }
            catch (FormatException)
            {
                return false;
            }

            return result != null;
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
                           .Replace("\u0002", dateTime.ToString("%t", culture));

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

                var res = await runner.EvalArgAsync<ValidFormulaValue>(arg, context, arg.IRContext).ConfigureAwait(false);

                if (res.IsValue)
                {
                    var val = res.Value;
                    if (!(val is StringValue str && str.Value == string.Empty))
                    {
                        return MaybeAdjustToCompileTimeType(res.ToFormulaValue(), irContext);
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

        public static FormulaValue EncodeUrl(IRContext irContext, StringValue[] args)
        {
            return new StringValue(irContext, Uri.EscapeDataString(args[0].Value));
        }

        public static FormulaValue Proper(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, StringValue[] args)
        {
            return new StringValue(irContext, runner.CultureInfo.TextInfo.ToTitleCase(runner.CultureInfo.TextInfo.ToLower(args[0].Value)));
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-len
        public static FormulaValue Len(IRContext irContext, StringValue[] args)
        {
            return NumberOrDecimalValue(irContext, args[0].Value.Length);
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

            TryGetInt(start, out int start1Based);
            var start0Based = start1Based - 1;

            string str = ((StringValue)args[0]).Value;
            if (str == string.Empty || start0Based >= str.Length)
            {
                return new StringValue(irContext, string.Empty);
            }

            TryGetInt(count, out int countValue);
            var minCount = Math.Min(countValue, str.Length - start0Based);
            var result = str.Substring(start0Based, minCount);

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

            if ((count.Value % 1) != 0)
            {
                throw new NotImplementedException("Should have been handled by IR");
            }

            TryGetInt(count, out int intCount);

            return new StringValue(irContext, leftOrRight(source.Value, intCount));
        }

        private static FormulaValue Find(IRContext irContext, FormulaValue[] args)
        {
            var findText = (StringValue)args[0];
            var withinText = (StringValue)args[1];

            if (!TryGetInt(args[2], out int startIndexValue))
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            if (startIndexValue < 1 || startIndexValue > withinText.Value.Length + 1)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            var index = withinText.Value.IndexOf(findText.Value, startIndexValue - 1, StringComparison.Ordinal);

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

            if (!TryGetInt(args[1], out int start1Based))
            {
                start1Based = source.Length + 1;
            }

            var start0Based = start1Based - 1;
            var prefix = start0Based < source.Length ? source.Substring(0, start0Based) : source;

            if (!TryGetInt(args[2], out int intCount))
            {
                intCount = intCount - start0Based;
            }

            var suffixIndex = start0Based + intCount;
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
        internal static int SubstituteGetResultLength(int sourceLen, int matchLen, int replacementLen, bool replaceAll)
        {
            int maxLenChars;

            if (matchLen == 0 || matchLen > sourceLen)
            {
                // Match is empty or too large, can't be found.
                // So will not match and just return original.
                return sourceLen;
            }

            checked
            {
                if (replaceAll)
                {
                    // Replace all instances. 
                    // Maximum possible length of Substitute, convert all the Match to Replacement. 
                    // Unicode, so 2B per character.                        
                    // Round up as conservative estimate. 
                    maxLenChars = (int)Math.Ceiling((double)sourceLen / matchLen) * replacementLen;
                }
                else
                {
                    // Only replace 1 instance 
                    maxLenChars = sourceLen - matchLen + replacementLen;
                }
            }

            // If not match found, will still be source length 
            return Math.Max(sourceLen, maxLenChars);
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

                TryGetInt(nv, out instanceNum);
            }

            // Compute max possible memory this operation may need.
            var sourceLen = source.Value.Length;
            var matchLen = match.Value.Length;
            var replacementLen = replacement.Value.Length;
            var maxLenChars = sourceLen;

            try
            {
                maxLenChars = SubstituteGetResultLength(sourceLen, matchLen, replacementLen, instanceNum < 0);
            }
            catch (OverflowException)
            {
                return CommonErrors.OverflowError(irContext);
            }

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

            StringBuilder strBuilder = new StringBuilder(source.Value);
            if (instanceNum < 0)
            {
                strBuilder.Replace(match.Value, replacement.Value);
            }
            else
            {
                // 0 is an error. This was already enforced by the IR
                Contract.Assert(instanceNum > 0);

                for (int idx = 0; idx < source.Value.Length; idx += match.Value.Length)
                {
                    eval.CheckCancel();

                    idx = source.Value.IndexOf(match.Value, idx, StringComparison.Ordinal);
                    if (idx == -1)
                    {
                        break;
                    }

                    if (--instanceNum == 0)
                    {
                        strBuilder.Replace(match.Value, replacement.Value, idx, match.Value.Length);
                        break;
                    }
                }
            }

            return new StringValue(irContext, strBuilder.ToString());
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

        public static FormulaValue GuidNoArg(IRContext irContext, FormulaValue[] args)
        {
            return new GuidValue(irContext, Guid.NewGuid());
        }

        public static FormulaValue GuidPure(IRContext irContext, StringValue[] args)
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

        public static FormulaValue OptionSetValueToLogicalName(IRContext irContext, OptionSetValue[] args)
        {
            var optionSet = args[0];
            var logicalName = optionSet.Option;
            return new StringValue(irContext, logicalName);
        }

        public static FormulaValue PlainText(IRContext irContext, StringValue[] args)
        {
            string text = args[0].Value.Trim();

            // Replace header/script/style tags with empty text.
            text = _headerTagRegex.Replace(text, string.Empty);
            text = _scriptTagRegex.Replace(text, string.Empty);
            text = _styleTagRegex.Replace(text, string.Empty);

            // Remove all comments.
            text = _commentTagRegex.Replace(text, string.Empty);

            // Insert empty string in place of <td>
            text = _tdTagRegex.Replace(text, string.Empty);

            //Replace <br> or <li> with line break
            text = _lineBreakTagRegex.Replace(text, Environment.NewLine);

            // Insert double line breaks in place of <div>, <p> and <tr> tags.
            text = _doubleLineBreakTagRegex.Replace(text, Environment.NewLine + Environment.NewLine);

            // Replace all other tags with empty text.
            text = _htmlTagsRegex.Replace(text, string.Empty);

            //Decode html specific characters
            text = WebUtility.HtmlDecode(text);

            return new StringValue(irContext, text.Trim());
        }

        private static DateTime ConvertToUTC(DateTime dateTime, TimeZoneInfo fromTimeZone)
        {
            var resultDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), fromTimeZone.GetUtcOffset(dateTime));
            return resultDateTime.UtcDateTime;
        }

        internal static bool TryGetInt(FormulaValue value, out int outputValue)
        {
            double inputValue;
            outputValue = int.MinValue;

            switch (value)
            {
                case NumberValue n:
                    inputValue = n.Value;
                    break;
                case DecimalValue w:
                    inputValue = (double)w.Value;
                    break;
                default:
                    return false;
            }

            if (inputValue > int.MaxValue)
            {
                outputValue = int.MaxValue;
                return false;
            }
            else if (inputValue < int.MinValue)
            {
                outputValue = int.MinValue;
                return false;
            }

            outputValue = (int)inputValue;
            return true;
        }
    }
}
