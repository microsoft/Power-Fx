// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Microsoft.PowerFx.Core.Utils
{
    internal sealed class TextFormatUtils
    {
        private static readonly Regex _formatWithoutZeroSubsecondsRegex = new Regex(@"[sS]\.?(0+)", RegexOptions.Compiled);

        public static bool IsValidFormatArg(string formatString, out string formatCultureName, out string formatArg, out bool hasDateTimeFmt, out bool hasNumericFmt)
        {
            // Verify statically that the format string doesn't contain BOTH numeric and date/time
            // format specifiers. If it does, that's an error according to Excel and our spec.
            hasDateTimeFmt = false;
            hasNumericFmt = false;
            int endIdx = -1;
            formatCultureName = null;
            formatArg = formatString;

            // But firstly skip any locale-prefix
            int startIdx = formatString.IndexOf("[$-", StringComparison.Ordinal);
            if (startIdx == 0)
            {
                endIdx = formatString.IndexOf(']', 3);
                if (endIdx > 0)
                {
                    formatCultureName = formatString.Substring(3, endIdx - 3);
                    formatArg = formatString.Substring(endIdx + 1);

                    if (string.IsNullOrEmpty(formatCultureName))
                    {
                        return false;
                    }
                }
            }

            hasDateTimeFmt = (formatArg.IndexOfAny(new char[] { 'm', 'M', 'd', 'D', 'y', 'Y', 'h', 'H', 's', 'S', 'a', 'A', 'p', 'P' }) >= 0) 
                && (formatArg.IndexOf("us", StringComparison.OrdinalIgnoreCase) < 0);

            hasNumericFmt = formatArg.IndexOfAny(new char[] { '0', '#' }) >= 0;
            if (hasDateTimeFmt && hasNumericFmt)
            {
                // Check if the date time format contains '0's after the seconds specifier, which
                // is used for fractional seconds - in which case it is valid
                var formatWithoutZeroSubseconds = _formatWithoutZeroSubsecondsRegex.Replace(formatArg, m => m.Groups[1].Success ? string.Empty : m.Groups[1].Value);
                hasNumericFmt = formatWithoutZeroSubseconds.IndexOfAny(new char[] { '0', '#' }) >= 0;
            }

            if ((hasDateTimeFmt && hasNumericFmt) || (startIdx > 0) || (startIdx == 0 && endIdx <= 0))
            {
                return false;
            }

            return true;
        }
    }
}
