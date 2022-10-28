// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
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
            var arg0 = args[0];

            if (arg0 is NumberValue n)
            {
                return n;
            }

            if (arg0 is DateValue dv)
            {
                return DateToNumber(irContext, new DateValue[] { dv });
            }

            if (arg0 is DateTimeValue dtv)
            {
                return DateTimeToNumber(irContext, new DateTimeValue[] { dtv });
            }

            string str = null;

            if (arg0 is StringValue sv)
            {
                str = sv.Value; // No Trim
            }

            if (string.IsNullOrEmpty(str))
            {
                return new BlankValue(irContext);
            }

            // culture will have Cultural info in case one was passed in argument else it will have the default one.
            var culture = runner.CultureInfo;
            if (args.Length > 1)
            {
                if (args[1] is StringValue cultureArg && !TryGetCulture(cultureArg.Value, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, cultureArg.Value);
                }
            }

            var (val, err) = ConvertToNumber(str, culture);

            if (err == ConvertionStatus.Ok)
            {
                return new NumberValue(irContext, val);
            }

            return CommonErrors.ArgumentOutOfRange(irContext);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-text
        public static FormulaValue Text(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            // only DateValue and DateTimeValue are supported for now with custom format strings.
            if (args[0] is StringValue sv)
            {
                return new StringValue(irContext, sv.Value);
            }

            string resultString = null;
            string formatString = null;

            if (args.Length > 1 && args[1] is StringValue fs)
            {
                formatString = fs.Value;
            }

            var culture = runner.CultureInfo;
            if (args.Length > 2 && args[2] is StringValue languageCode)
            {
                if (!TryGetCulture(languageCode.Value, out culture))
                {
                    return CommonErrors.BadLanguageCode(irContext, languageCode.Value);
                }
            }

            switch (args[0])
            {
                case NumberValue num:
                    resultString = num.Value.ToString(formatString ?? "g", culture);
                    break;
                case StringValue s:
                    resultString = s.Value;
                    break;
                case DateValue d:
                    formatString = ExpandDateTimeFormatSpecifiers(formatString, culture);
                    resultString = d.Value.ToString(formatString ?? "M/d/yyyy", culture);
                    break;
                case DateTimeValue dt:
                    formatString = ExpandDateTimeFormatSpecifiers(formatString, culture);
                    resultString = dt.Value.ToString(formatString ?? "g", culture);
                    break;
                case TimeValue t:
                    formatString = ExpandDateTimeFormatSpecifiers(formatString, culture);
                    resultString = _epoch.Add(t.Value).ToString(formatString ?? "t", culture);
                    break;
                default:
                    break;
            }

            if (resultString != null)
            {
                return new StringValue(irContext, resultString);
            }

            return CommonErrors.NotYetImplementedError(irContext, $"Text format for {args[0]?.GetType().Name}");
        }

        internal static string ExpandDateTimeFormatSpecifiers(string format, CultureInfo culture)
        {
            if (format == null)
            {
                return format;
            }

            var info = DateTimeFormatInfo.GetInstance(culture);

            switch (format.ToLower().Trim('\''))
            {
                case "shortdatetime24":
                    // TODO: This might be wrong for some cultures
                    return ReplaceWith24HourClock(info.ShortDatePattern + " " + info.ShortTimePattern);
                case "shortdatetime":
                    // TODO: This might be wrong for some cultures
                    return info.ShortDatePattern + " " + info.ShortTimePattern;
                case "shorttime24":
                    return ReplaceWith24HourClock(info.ShortTimePattern);
                case "shorttime":
                    return info.ShortTimePattern;
                case "shortdate":
                    return info.ShortDatePattern;
                case "longdatetime24":
                    return ReplaceWith24HourClock(info.FullDateTimePattern);
                case "longdatetime":
                    return info.FullDateTimePattern;
                case "longtime24":
                    return ReplaceWith24HourClock(info.LongTimePattern);
                case "longtime":
                    return info.LongTimePattern;
                case "longdate":
                    return info.LongDatePattern;
                case "utc":
                    return info.UniversalSortableDateTimePattern;
            }

            return format;
        }

        private static string ReplaceWith24HourClock(string format)
        {
            var pattern = @"^(?<openAMPM>\s*t+\s*)? " +
                             @"(?(openAMPM) h+(?<nonHours>[^ht]+)$ " +
                             @"| \s*h+(?<nonHours>[^ht]+)\s*t+)";
            return Regex.Replace(format, pattern, "HH${nonHours}", RegexOptions.IgnorePatternWhitespace);
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

        private static FormulaValue Substitute(IRContext irContext, FormulaValue[] args)
        {
            var source = (StringValue)args[0];
            var match = (StringValue)args[1];
            var replacement = (StringValue)args[2];

            if (string.IsNullOrEmpty(match.Value))
            {
                return source;
            }

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

            var sourceValue = source.Value;
            var idx = sourceValue.IndexOf(match.Value);
            if (instanceNum < 0)
            {
                while (idx >= 0)
                {
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
