using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using System.Xml.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Utils
{
    internal sealed class TextFormatUtils
    {
        static public DateTime BaseDateTime => new DateTime(1899, 12, 30, 0, 0, 0, 0);

        public static bool IsValidFormatArg(string formatArg, out bool hasDateTimeFmt, out bool hasNumericFmt)
        {
            // Verify statically that the format string doesn't contain BOTH numeric and date/time
            // format specifiers. If it does, that's an error according to Excel and our spec.

            // But firstly skip any locale-prefix
            if (formatArg.StartsWith("[$-"))
            {
                var end = formatArg.IndexOf(']', 3);
                if (end > 0)
                {
                    formatArg = formatArg.Substring(end + 1);
                }
            }

            hasDateTimeFmt = formatArg.IndexOfAny(new char[] { 'm', 'M', 'd', 'D', 'y', 'Y', 'h', 'H', 's', 'S', 'a', 'A', 'p', 'P' }) >= 0;
            hasNumericFmt = formatArg.IndexOfAny(new char[] { '0', '#' }) >= 0;
            if (hasDateTimeFmt && hasNumericFmt)
            {
                // Check if the date time format contains '0's after the seconds specifier, which
                // is used for fractional seconds - in which case it is valid
                var formatWithoutZeroSubseconds = Regex.Replace(formatArg, @"[sS]\.?(0+)",
                    m => m.Groups[1].Success ? "" : m.Groups[1].Value);
                hasNumericFmt = formatWithoutZeroSubseconds.IndexOfAny(new char[] { '0', '#' }) >= 0;
            }

            if (hasDateTimeFmt && hasNumericFmt)
            {
                return false;
            }

            return true;
        }

        public static bool IsNumericFormat(string formatArg)
        {
            if (formatArg == null)
            {
                return false;
            }

            return IsValidFormatArg(formatArg, out _, out var hasNumericFmt) && hasNumericFmt;
        }

        public static bool IsDateTimeFormat(string formatArg)
        {
            if (formatArg == null)
            {
                return false;
            }

            return IsValidFormatArg(formatArg, out var hasDateTimeFmt, out _) && hasDateTimeFmt;
        }

        public static DateTime NumberValueToDateTime(NumberValue numberValue)
        {
            var millisecondsPerDay = 86400000; // hours * minutes * seconds * 1000
            var intPart = (int)Math.Floor(numberValue.Value);
            var fracPart = numberValue.Value - intPart;

            return BaseDateTime.AddDays(intPart).AddMilliseconds((int)(fracPart * millisecondsPerDay));
        }
    }
}
