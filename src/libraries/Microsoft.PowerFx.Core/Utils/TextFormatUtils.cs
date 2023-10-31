// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Utils
{
    internal enum DateTimeFmtType
    {
        NoDateTimeFormat = 0,
        GeneralDateTimeFormat = 1,
        EnumDateTimeFormat = 2
    }

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
        /// Numeric/date time format string.
        /// </summary>
        public string FormatArg { get; set; }

        /// <summary>
        /// Type of date time format.
        /// </summary>
        public DateTimeFmtType DateTimeFmt { get; set; }

        /// <summary>
        /// True/False if format string has numeric format or not.
        /// </summary>
        public bool HasNumericFmt { get; set; }
    }

    internal sealed class TextFormatUtils
    {
        internal static readonly IReadOnlyList<DType> AllowedListToUseFormatString = new DType[] { DType.Number, DType.Decimal, DType.DateTime, DType.Date, DType.Time, DType.ObjNull };

        private static readonly Regex _formatWithoutZeroSubsecondsRegex = new Regex(@"[sS]\.?(0+)", RegexOptions.Compiled);
        private static readonly IReadOnlyList<char> _dateTimeCharacters = new char[] { 'm', 'M', 'd', 'D', 'y', 'Y', 'h', 'H', 's', 'S', 'a', 'A', 'p', 'P' };
        private static readonly IReadOnlyList<char> _numericCharacters = new char[] { '0', '#' };
        private static readonly IReadOnlyList<char> _unsupportedCharacters = new char[] { '?', '[', '_', '*', '@', ']' };
        private static readonly IReadOnlyList<char> _specialCharacters = new char[] { 'z', '$', 'b', 'c', 'f', 'n', 'p', 'x', 'B', 'C', 'F', 'N', 'P', 'X' };

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
            textFormatArgs = new TextFormatArgs
            {
                FormatCultureName = null,
                FormatArg = formatString,
                DateTimeFmt = DateTimeFmtType.NoDateTimeFormat,
                HasNumericFmt = false
            };

            // Process locale-prefix to get format culture name and numeric format string
            int startIdx = formatString.IndexOf("[$-", StringComparison.Ordinal);

            // Block locale until we support locale for datetime as well.
            if (startIdx == 0)
            {
                return false;
            }

            var formatStr = textFormatArgs.FormatArg;

            //Block "general", "g", "G"
            if (formatStr == "g" || formatStr == "G" ||
                formatStr.IndexOf("general", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            // Do not allow format string start with '/'
            if (formatStr.Length > 0 && formatStr[0] == '/')
            {
                return false;
            }

            bool hasNumericCharacters = false;
            int decimalPointIndex = -1;
            int sectionCount = 0;
            int mCount = 0;
            bool hasColonWithNum = false;
            bool hasExponentialNotation = false;
            List<int> commaIdxList = new List<int>();
            for (int i = 0; i < formatStr.Length; i++)
            {
                if (formatStr[i] == 'm' || formatStr[i] == 'M')
                {
                    mCount++;
                    if (mCount > 4)
                    {
                        return false;
                    }
                }
                else
                {
                    mCount = 0;
                }

                if (formatStr[i] == 'a' || formatStr[i] == 'A')
                {
                    // Block lower or mix cases of A/P or AM/PM
                    if ((i < formatStr.Length - 2 && formatStr[i] == 'a' && formatStr[i + 1] == '/' && (formatStr[i + 2] == 'p' || formatStr[i + 2] == 'P')) ||
                        (i < formatStr.Length - 2 && formatStr[i] == 'A' && formatStr[i + 1] == '/' && formatStr[i + 2] == 'p') ||
                        (i < formatStr.Length - 4 && formatStr[i] == 'a' && (formatStr[i + 1] == 'm' || formatStr[i + 1] == 'M') && formatStr[i + 2] == '/' && formatStr[i + 3] == 'p' && (formatStr[i + 4] == 'm' || formatStr[i + 4] == 'M')) ||
                        (i < formatStr.Length - 4 && formatStr[i] == 'A' && formatStr[i + 1] == 'm' && formatStr[i + 2] == '/' && formatStr[i + 3] == 'P' && formatStr[i + 4] == 'm'))
                    {
                        return false;
                    }
                }

                if ((i == 0 || textFormatArgs.HasNumericFmt) && _specialCharacters.Contains(formatStr[i]))
                {
                    formatStr = formatStr.Insert(i + 1, "\"");
                    formatStr = formatStr.Insert(i, "\"");
                    i += 2;
                }
                else if (_numericCharacters.Contains(formatStr[i]))
                {
                    // ':' is not allowed between # or 0 (numeric)
                    if (textFormatArgs.DateTimeFmt != DateTimeFmtType.GeneralDateTimeFormat && hasColonWithNum)
                    {
                        return false;
                    }

                    // Use hasNumericCharacters to check if format string has numeric character before group separator or after decimal point.
                    hasNumericCharacters = true;
                    textFormatArgs.HasNumericFmt = true;

                    // Clear comma list for scaling because comma before numeric characters does not use for scaling.
                    if (commaIdxList.Count > 0)
                    {
                        if (formatStr.Contains('.') && decimalPointIndex == -1)
                        {
                            for (int j = commaIdxList.Count - 1; j >= 0; j--)
                            {
                                if (commaIdxList[j] < formatStr.Length - 1 && !_numericCharacters.Contains(formatStr[commaIdxList[j] + 1]))
                                {
                                    formatStr = formatStr.Remove(commaIdxList[j], 1);
                                }
                            }
                        }

                        commaIdxList.Clear();
                    }
                }
                else if (_dateTimeCharacters.Contains(formatStr[i]))
                {
                    textFormatArgs.DateTimeFmt = DateTimeFmtType.GeneralDateTimeFormat;
                }
                else if (textFormatArgs.DateTimeFmt != DateTimeFmtType.GeneralDateTimeFormat && formatStr[i] == ',' && !hasNumericCharacters && decimalPointIndex == -1)
                {
                    // If there is no numeric format character before group separator character, then treat it as an escaping character.
                    formatStr = formatStr.Insert(i, "\\");
                    i++;
                }
                else if (textFormatArgs.DateTimeFmt != DateTimeFmtType.GeneralDateTimeFormat && formatStr[i] == '.')
                {
                    // Reset hasNumericCharacters to false to later check if any numeric character after decimal point.
                    decimalPointIndex = i;
                    hasNumericCharacters = false;

                    if (hasExponentialNotation)
                    {
                        // Block exponential notation with decimal number
                        return false;
                    }

                    if (commaIdxList.Count > 0)
                    {
                        // Remove any comma before decimal point that is not using for scaling
                        if (commaIdxList[commaIdxList.Count - 1] != i - 1)
                        {
                            for (int j = commaIdxList.Count - 1; j >= 0; j--)
                            {
                                formatStr = formatStr.Remove(commaIdxList[j], 1);
                            }

                            // Reset comma index list
                            commaIdxList.Clear();
                        }
                    }
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
                else if (textFormatArgs.HasNumericFmt && formatStr[i] == ',' && i > 0)
                {
                    if (_numericCharacters.Contains(formatStr[i - 1]) || commaIdxList.Contains(i - 1))
                    {
                        // Record each comma index after numeric character and comma to do scaling factor process in the end of format.
                        commaIdxList.Add(i);
                    }
                    else if (formatStr[i - 1] == ',')
                    {
                        // Removing consecutive comma if it is not used for scaling factor
                        formatStr = formatStr.Remove(i, 1);
                        i--;
                    }
                    else if (formatStr[i - 1] != '.')
                    {
                        // Add escaping character to comma
                        formatStr = formatStr.Insert(i, "\\");
                        i++;
                    }
                }
                else if (formatStr[i] == '\'' && (i == 0 || formatStr[i - 1] != '\\'))
                {
                    // Add escaping character to '
                    formatStr = formatStr.Insert(i, "\\");
                    i++;
                }
                else if ((formatStr[i] == 'e' || formatStr[i] == 'E') && (i < formatStr.Length - 1 && (formatStr[i + 1] == '+' || formatStr[i + 1] == '-')))
                {
                    hasExponentialNotation = true;
                }
                else if (formatStr[i] == '%' && hasExponentialNotation)
                {
                    // Block exponential notation with %
                    return false;
                }
                else if (i == formatStr.Length - 1)
                {
                    // If format string ends with backsplash but no following character or opening double quote then format is invalid.
                    if (formatStr[i] == '\\' || formatStr[i] == '\"')
                    {
                        return false;
                    }

                    // If format string of numeric ends with e or e+ (not escaping character) then format is invalid.
                    if (textFormatArgs.DateTimeFmt != DateTimeFmtType.GeneralDateTimeFormat && (formatStr[i] == 'e' || formatStr[i] == 'E' ||
                        (i > 2 && formatStr[i - 2] != '\\' && (formatStr[i - 1] == 'e' || formatStr[i - 1] == 'E') && formatStr[i] == '+')))
                    {
                        return false;
                    }
                }
                else if (formatStr[i] == '\\')
                {
                    if (hasExponentialNotation)
                    {
                        // Block exponential notation with escaping character
                        return false;
                    }

                    if (textFormatArgs.HasNumericFmt || (i < formatStr.Length - 1 && formatStr[i + 1] == '\"'))
                    {
                        // Skip next character if seeing escaping character.
                        i++;
                    }
                    else if (i < formatStr.Length - 1)
                    {
                        // Update \c to "c" to match with Excel                       
                        formatStr = formatStr.Insert(i + 2, "\"");
                        formatStr = formatStr.Insert(i + 1, "\"");
                        formatStr = formatStr.Remove(i, 1);
                        i += 2;
                    }
                }
                else if (formatStr[i] == '\"' && i < formatStr.Length - 1)
                {
                    if (hasExponentialNotation)
                    {
                        // Block exponential notation with escaping character.
                        return false;
                    }

                    // Jump to close quote to pass all escaping characters.
                    i = formatStr.IndexOf('\"', i + 1);

                    // Format is invalid if missing close quote.
                    if (i == -1)
                    {
                        return false;
                    }
                }
            }

            // Each comma after the decimal point and numeric character divides the number by 1,000.
            // Move all commas after the decimal point and numeric character of format string to right before decimal point if it has.
            if (commaIdxList.Count > 0 && decimalPointIndex != -1)
            {
                for (int j = commaIdxList.Count - 1; j >= 0; j--)
                {
                    formatStr = formatStr.Remove(commaIdxList[j], 1);
                    if (commaIdxList[j] < decimalPointIndex)
                    {
                        decimalPointIndex--;
                    }
                }

                formatStr = formatStr.Insert(decimalPointIndex, new string(',', commaIdxList.Count));
            }

            if (textFormatArgs.DateTimeFmt == DateTimeFmtType.GeneralDateTimeFormat && textFormatArgs.HasNumericFmt)
            {
                // Check if the date time format contains '0's after the seconds specifier, which
                // is used for fractional seconds - in which case it is valid
                var formatWithoutZeroSubseconds = _formatWithoutZeroSubsecondsRegex.Replace(formatStr, m => m.Groups[1].Success ? string.Empty : m.Groups[1].Value);
                textFormatArgs.HasNumericFmt = formatWithoutZeroSubseconds.IndexOfAny(_numericCharacters.ToArray()) >= 0;
            }

            if (textFormatArgs.DateTimeFmt == DateTimeFmtType.GeneralDateTimeFormat && textFormatArgs.HasNumericFmt)
            {
                return false;
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

                // Update '‰' to '\‰' to escape '‰' in c# to match with excel.
                formatStr = formatStr.Replace("‰", "\\‰");
            }

            if (textFormatArgs.DateTimeFmt == DateTimeFmtType.GeneralDateTimeFormat)
            {
                // Convert \' in DateTime format to '
                formatStr = formatStr.Replace("\\'", "\'");
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
