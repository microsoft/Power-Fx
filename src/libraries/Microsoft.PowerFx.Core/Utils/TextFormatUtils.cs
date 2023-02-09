// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.RegularExpressions;

namespace Microsoft.PowerFx.Core.Utils
{
    internal sealed class TextFormatUtils
    {
        private static readonly Regex _formatWithoutZeroSubsecondsRegex = new Regex(@"[sS]\.?(0+)", RegexOptions.Compiled);

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
                var formatWithoutZeroSubseconds = _formatWithoutZeroSubsecondsRegex.Replace(formatArg, m => m.Groups[1].Success ? string.Empty : m.Groups[1].Value);
                hasNumericFmt = formatWithoutZeroSubseconds.IndexOfAny(new char[] { '0', '#' }) >= 0;
            }

            if (hasDateTimeFmt && hasNumericFmt)
            {
                return false;
            }

            return true;
        }
    }
}
