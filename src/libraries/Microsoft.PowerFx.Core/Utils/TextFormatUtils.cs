// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Microsoft.PowerFx.Core.Utils
{
    internal class TextFormatArgs
    {
        internal string FormatCultureName { get; set; }

        internal string FormatArg { get; set; }

        internal bool HasDateTimeFmt { get; set; }

        internal bool HasNumericFmt { get; set; }
    }

    internal sealed class TextFormatUtils
    {
        private static readonly Regex _formatWithoutZeroSubsecondsRegex = new Regex(@"[sS]\.?(0+)", RegexOptions.Compiled);

        public static bool IsValidFormatArg(string formatString, out TextFormatArgs textFormatArgs)
        {
            // Verify statically that the format string doesn't contain BOTH numeric and date/time
            // format specifiers. If it does, that's an error according to Excel and our spec.
            int endIdx = -1;
            textFormatArgs = new TextFormatArgs
            {
                FormatCultureName = null,
                FormatArg = formatString,
                HasDateTimeFmt = false,
                HasNumericFmt = false
            };

            // Process locale-prefix to get format culture name and numeric format string
            int startIdx = formatString.IndexOf("[$-", StringComparison.Ordinal);
            if (startIdx == 0)
            {
                endIdx = formatString.IndexOf(']', 3);
                if (endIdx > 0)
                {
                    textFormatArgs.FormatCultureName = formatString.Substring(3, endIdx - 3);
                    textFormatArgs.FormatArg = formatString.Substring(endIdx + 1);

                    if (string.IsNullOrEmpty(textFormatArgs.FormatCultureName))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else if (startIdx > 0)
            {
                return false;
            }

            textFormatArgs.HasDateTimeFmt = textFormatArgs.FormatArg.IndexOfAny(new char[] { 'm', 'M', 'd', 'D', 'y', 'Y', 'h', 'H', 's', 'S', 'a', 'A', 'p', 'P' }) >= 0;
            textFormatArgs.HasNumericFmt = textFormatArgs.FormatArg.IndexOfAny(new char[] { '0', '#' }) >= 0;
            if (textFormatArgs.HasDateTimeFmt && textFormatArgs.HasNumericFmt)
            {
                // Check if the date time format contains '0's after the seconds specifier, which
                // is used for fractional seconds - in which case it is valid
                var formatWithoutZeroSubseconds = _formatWithoutZeroSubsecondsRegex.Replace(textFormatArgs.FormatArg, m => m.Groups[1].Success ? string.Empty : m.Groups[1].Value);
                textFormatArgs.HasNumericFmt = formatWithoutZeroSubseconds.IndexOfAny(new char[] { '0', '#' }) >= 0;
            }

            if (textFormatArgs.HasDateTimeFmt && textFormatArgs.HasNumericFmt)
            {
                return false;
            }

            return true;
        }

        public static bool IsValidCompiledTimeFormatArg(string formatArg)
        {
            // Verify statically that the format string doesn't contain BOTH numeric and date/time
            // format specifiers. If it does, that's an error according to Excel and our spec.

            // But firstly skip any locale-prefix
            if (formatArg.StartsWith("[$-", StringComparison.Ordinal))
            {
                var end = formatArg.IndexOf(']', 3);
                if (end > 0)
                {
                    formatArg = formatArg.Substring(end + 1);
                }
            }

            var hasDateTimeFmt = formatArg.IndexOfAny(new char[] { 'm', 'd', 'y', 'h', 'H', 's', 'a', 'A', 'p', 'P' }) >= 0;
            var hasNumericFmt = formatArg.IndexOfAny(new char[] { '0', '#' }) >= 0;
            if (hasDateTimeFmt && hasNumericFmt)
            {
                // Check if the date time format contains '0's after the seconds specifier, which
                // is used for fractional seconds - in which case it is valid
                var formatWithoutZeroSubseconds = Regex.Replace(formatArg, @"[sS]\.?(0+)", m => m.Groups[1].Success ? string.Empty : m.Groups[1].Value);
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
