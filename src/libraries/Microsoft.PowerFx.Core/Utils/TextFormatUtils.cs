// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// Definition for format string object ([$-FormatCultureName]FormatArg).
    /// </summary>
    internal class TextFormatArgs
    {
        /// <summary>
        /// Culture name of the format string.
        /// </summary>
        public string FormatCultureName { get; set; }

        /// <summary>
        /// numeric/date time format string.
        /// </summary>
        public string FormatArg { get; set; }

        /// <summary>
        /// True/False if format string has DateTime format or not.
        /// </summary>
        public bool HasDateTimeFmt { get; set; }

        /// <summary>
        /// True/False if format string has numeric format or not.
        /// </summary>
        public bool HasNumericFmt { get; set; }
    }

    internal sealed class TextFormatUtils
    {
        private static readonly Regex _formatWithoutZeroSubsecondsRegex = new Regex(@"[sS]\.?(0+)", RegexOptions.Compiled);
        private static readonly IReadOnlyList<char> _dateTimeCharacters = new char[] { 'm', 'M', 'd', 'D', 'y', 'Y', 'h', 'H', 's', 'S', 'a', 'A', 'p', 'P' };
        private static readonly IReadOnlyList<char> _numericCharacters = new char[] { '0', '#' };
        private static readonly IReadOnlyList<char> _unsupportedCharacters = new char[] { '?', '[', '_', '*', '@' };

        /// <summary>
        /// Validate if format string is valid or not and return format string object.
        /// </summary>
        /// <param name="formatString">Raw input format string.</param>
        /// <param name="formatCulture">Current format culture.</param>
        /// <param name="defaultLanguage">Default value for TextFormatArgs.FormatCultureName. This can be overwritten by using the [$-LanguageCode] syntax in the string format.</param>
        /// <param name="textFormatArgs">Return format string object.</param>
        /// <returns>True/False based on whether format string is valid or not.</returns> 
        public static bool IsValidFormatArg(string formatString, CultureInfo formatCulture, string defaultLanguage, out TextFormatArgs textFormatArgs)
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

            // Block locale until we support locale for datetime as well. Remove this when we support locale later.
            if (startIdx == 0)
            {
                return false;
            }

            if (startIdx == 0)
            {
                endIdx = formatString.IndexOf(']', 3);
                if (endIdx > 0)
                {
                    textFormatArgs.FormatCultureName = formatString.Substring(3, endIdx - 3).Trim();
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

            bool hasNumericCharacters = false;
            int decimalPointIndex = -1;
            int sectionCount = 0;
            var formatStr = textFormatArgs.FormatArg;
            bool hasColonWithNum = false;

            //Block "gen"/"general"
            if (formatStr.IndexOf("gen", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            for (int i = 0; i < formatStr.Length; i++)
            {
                // Do not allow format string start with '/'
                if (i == 0 && formatStr[i] == '/')
                {
                    return false;
                }

                if (_numericCharacters.Contains(formatStr[i]))
                {
                    // ':' is not allowed between # or 0 (numeric)
                    if (!textFormatArgs.HasDateTimeFmt && hasColonWithNum)
                    {
                        return false;
                    }

                    textFormatArgs.HasNumericFmt = true;

                    // Use hasNumericCharacters to check if format string has numeric character before group separator or after decimal point.
                    hasNumericCharacters = true;
                }
                else if (_dateTimeCharacters.Contains(formatStr[i]))
                {
                    textFormatArgs.HasDateTimeFmt = true;
                }
                else if (!textFormatArgs.HasDateTimeFmt && formatStr[i] == ',' && !hasNumericCharacters)
                {
                    // If there is no numeric format character before group separator character, then treat it as an escaping character.
                    formatStr = formatStr.Insert(i, "\\");
                    i++;
                }
                else if (!textFormatArgs.HasDateTimeFmt && formatStr[i] == '.')
                {
                    // Reset hasNumericCharacters to false to later check if any numeric character after decimal point.
                    decimalPointIndex = i;
                    hasNumericCharacters = false;
                }
                else if (_unsupportedCharacters.Contains(formatStr[i]))
                {
                    // Block all unsupported characters
                    return false;
                }
                else if (formatStr[i] == ':')
                {
                    hasColonWithNum = true;
                }
                else if (formatStr[i] == ';')
                {
                    // Does not allow number of section are more than 2.
                    sectionCount++;
                    if (sectionCount > 2)
                    {
                        return false;
                    }
                }
                else if (i == formatStr.Length - 1)
                {
                    // If format string ends with backsplash but no following character (both numeric and datetime format), format is invalid.
                    if (formatStr[i] == '\\')
                    {
                        return false;
                    }

                    // If format string ends with e or e+ (not escaping character) then format is invalid (only applied for numeric format)
                    if (textFormatArgs.HasNumericFmt & (formatStr[i] == 'e' || (i > 2 && formatStr[i - 2] != '\\' && formatStr[i - 1] == 'e' && formatStr[i] == '+')))
                    {
                        return false;
                    }
                }
                else if (formatStr[i] == '\\')
                {
                    if (textFormatArgs.HasNumericFmt || (i < formatStr.Length - 1 && formatStr[i + 1] == '\"'))
                    {
                        // Skip next character if seeing escaping character \.
                        i++;
                    }
                    else
                    {
                        // Update \c to "c" to match with                       
                        formatStr = formatStr.Insert(i + 2, "\"");
                        formatStr = formatStr.Insert(i + 1, "\"");
                        formatStr = formatStr.Remove(i, 1);
                        i += 2;
                    }
                }
                else if (formatStr[i] == '\"' && i < formatStr.Length - 1)
                {
                    // Jump to close quote to pass all escaping characters.
                    i = formatStr.IndexOf('\"', i + 1);

                    // Format is invalid if missing close quote.
                    if (i == -1)
                    {
                        return false;
                    }
                }

                if (textFormatArgs.HasDateTimeFmt && textFormatArgs.HasNumericFmt)
                {
                    // Check if the date time format contains '0's after the seconds specifier, which
                    // is used for fractional seconds - in which case it is valid
                    var formatWithoutZeroSubseconds = _formatWithoutZeroSubsecondsRegex.Replace(formatStr, m => m.Groups[1].Success ? string.Empty : m.Groups[1].Value);
                    textFormatArgs.HasNumericFmt = formatWithoutZeroSubseconds.IndexOfAny(_numericCharacters.ToArray()) >= 0;
                }

                if (textFormatArgs.HasDateTimeFmt && textFormatArgs.HasNumericFmt)
                {
                    return false;
                }
            }

            // If there is no numeric format character (all escaping characters - backsplash or double quote) after decimal point then treat it as an escaping character.
            if (textFormatArgs.HasNumericFmt)
            {
                // Block '/' for numeric format string
                if (formatStr.Contains("/"))
                {
                    return false;
                }

                if (decimalPointIndex != -1 && !hasNumericCharacters)
                {
                    formatStr = formatStr.Insert(decimalPointIndex, "\\");
                }

                // Update \' or "'" to escaping character ' to match with C# then update any \' to ' to match with Excel (ex: \'' to \'\').
                formatStr = formatStr.Replace("\"\'\"", "\'");
                formatStr = formatStr.Replace("\\'", "\'");
                formatStr = formatStr.Replace("\'", "\\'");

                // Update '‰' to '\‰' to escape '‰' in c# to match with excel.
                formatStr = formatStr.Replace("‰", "\\‰");
            }

            textFormatArgs.FormatArg = formatStr;

            if (string.IsNullOrEmpty(textFormatArgs.FormatCultureName))
            {
                textFormatArgs.FormatCultureName = defaultLanguage;
            }

            // Use en-Us format string if a culture is defined in format string.
            if (formatCulture != null && !string.IsNullOrEmpty(textFormatArgs.FormatCultureName))
            {
                var enUSformatString = textFormatArgs.FormatArg;

                if (!TryGetCulture(textFormatArgs.FormatCultureName, out formatCulture))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(enUSformatString) && textFormatArgs.HasNumericFmt)
                {
                    // Get en-US numeric format string.
                    // \uFEFF is the zero width no-break space codepoint. This will be used to swap with number group seperator character.
                    const string numberGroupSeperator = "\uFEFF";
                    var numberCultureFormat = formatCulture.NumberFormat;

                    enUSformatString = enUSformatString.Replace(numberCultureFormat.NumberGroupSeparator, numberGroupSeperator);
                    if (string.IsNullOrWhiteSpace(numberCultureFormat.NumberGroupSeparator))
                    {
                        enUSformatString = enUSformatString.Replace(" ", numberGroupSeperator).Replace("\u202F", numberGroupSeperator);
                    }

                    enUSformatString = enUSformatString.Replace(numberCultureFormat.NumberDecimalSeparator, ".");
                    enUSformatString = enUSformatString.Replace(numberGroupSeperator, ",");
                }

                textFormatArgs.FormatArg = enUSformatString;
            }

            return true;
        }

        /// <summary>
        /// Legacy validate if format string is valid or not and return format string object.
        /// This is justed called in pre-V1 scenarios by Text function's CheckTypes().
        /// </summary>
        /// <param name="formatArg">Raw input format string.</param>
        /// <returns>True/False based on whether format string is valid or not.</returns> 
        public static bool IsLegacyValidCompiledTimeFormatArg(string formatArg)
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

        /// <summary>
        /// Try to get culture info from culture name.
        /// </summary>
        /// <param name="name">Culture name.</param>
        /// <param name="value">Return culture info.</param>
        /// <returns>True/False based on whether it can get culture info by culture name or not.</returns> 
        public static bool TryGetCulture(string name, out CultureInfo value)
        {
            value = null;

            try
            {
                value = new CultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
                return false;
            }

            return true;
        }
    }
}
