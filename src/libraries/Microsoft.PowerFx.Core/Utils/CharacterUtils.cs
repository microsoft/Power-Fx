// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using StringBuilderCache = Microsoft.PowerFx.Core.Utils.StringBuilderCache<Microsoft.PowerFx.Core.Utils.CharacterUtils>;

namespace Microsoft.PowerFx.Core.Utils
{
    internal sealed class CharacterUtils
    {
        private CharacterUtils()
        {
            // Do nothing.
        }

        /// <summary>
        /// Bit masks of the UnicodeCategory enum. A couple extra values are defined
        /// for convenience for the C# lexical grammar.
        /// </summary>
        [Flags]
        public enum UniCatFlags : uint
        {
            // Letters
            LowercaseLetter = 1 << UnicodeCategory.LowercaseLetter, // Ll
            UppercaseLetter = 1 << UnicodeCategory.UppercaseLetter, // Lu
            TitlecaseLetter = 1 << UnicodeCategory.TitlecaseLetter, // Lt
            ModifierLetter = 1 << UnicodeCategory.ModifierLetter, // Lm
            OtherLetter = 1 << UnicodeCategory.OtherLetter, // Lo

            // Marks
            NonSpacingMark = 1 << UnicodeCategory.NonSpacingMark, // Mn
            SpacingCombiningMark = 1 << UnicodeCategory.SpacingCombiningMark, // Mc

            // Numbers
            DecimalDigitNumber = 1 << UnicodeCategory.DecimalDigitNumber, // Nd
            LetterNumber = 1 << UnicodeCategory.LetterNumber, // Nl (i.e. roman numeral one 0x2160)

            // Spaces
            SpaceSeparator = 1 << UnicodeCategory.SpaceSeparator, // Zs
            LineSeparator = 1 << UnicodeCategory.LineSeparator, // Zl
            ParagraphSeparator = 1 << UnicodeCategory.ParagraphSeparator, // Zp

            // Other
            Format = 1 << UnicodeCategory.Format, // Cf
            Control = 1 << UnicodeCategory.Control, // Cc
            OtherNotAssigned = 1 << UnicodeCategory.OtherNotAssigned, // Cn
            PrivateUse = 1 << UnicodeCategory.PrivateUse, // Co
            Surrogate = 1 << UnicodeCategory.Surrogate, // Cs

            // Punctuation
            ConnectorPunctuation = 1 << UnicodeCategory.ConnectorPunctuation, // Pc

            // Useful combinations.
            IdentStartChar = UppercaseLetter | LowercaseLetter | TitlecaseLetter |
              ModifierLetter | OtherLetter | LetterNumber,

            IdentPartChar = IdentStartChar | NonSpacingMark | SpacingCombiningMark |
              DecimalDigitNumber | ConnectorPunctuation | Format,
        }

        /// <summary>
        /// Escapes a minimal set of characters (', \0, \b, \t, \n, \v, \f, \r, \u0085, \u2028, \u2029)
        /// by replacing them with their escape codes.
        /// </summary>
        public static string Escape(string input)
        {
            Contracts.CheckValue(input, nameof(input));

            return EscapeString(input);
        }

        public static string ToPlainText(string input)
        {
            Contracts.CheckValue(input, nameof(input));

            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var length = input.Length;
            var lengthForBuilder = EstimateEscapedStringLength(length) + 2 /* for the quotes */;
            var sb = StringBuilderCache.Acquire(lengthForBuilder);

            sb.Append("\"");
            InternalEscapeString(input, length, /* lengthForBuilder */ 0, ref sb, finalizeBuilder: false); // 'lengthForBuilder' will not be used.
            sb.Append("\"");

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        // Sanitizes a name so it can be used as an identifer/variable name in JavaScript.
        public static string ToJsIdentifier(string name)
        {
            Contracts.AssertNonEmpty(name);

            var length = name.Length;
            var estimatedLength = EstimateEscapedStringLength(length);
            var charsToAdd = 0;
            StringBuilder sb = null;

            for (var i = 0; i < length; i++)
            {
                var ch = name[i];

                if (IsLatinAlpha(ch))
                {
                    charsToAdd++;
                }
                else if (ch == '_')
                {
                    UpdateEscapeInternals("__", name, estimatedLength, i, ref charsToAdd, ref sb);
                }
                else if (IsDigit(ch))
                {
                    if (i == 0)
                    {
                        UpdateEscapeInternals("_" + ch, name, estimatedLength, i, ref charsToAdd, ref sb);
                    }
                    else
                    {
                        charsToAdd++;
                    }
                }
                else
                {
                    UpdateEscapeInternals("_" + ((uint)ch).ToString("X", CultureInfo.InvariantCulture), name, estimatedLength, i, ref charsToAdd, ref sb);
                }
            }

            // The original string wasn't modified.
            if (sb == null)
            {
                return name;
            }

            if (charsToAdd > 0)
            {
                sb.Append(name, length - charsToAdd, charsToAdd);
            }

            Contracts.Assert(sb.Length > 0);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        // Escape the specified string.
        public static string EscapeString(string value)
        {
            Contracts.AssertValue(value);

            var length = value.Length;
            var lengthForBuilder = EstimateEscapedStringLength(length);
            StringBuilder sb = null;

            return InternalEscapeString(value, length, lengthForBuilder, ref sb, finalizeBuilder: true);
        }

        public static string ExcelEscapeString(string value, bool isValueAnInterpolatedString = false)
        {
            Contracts.AssertValue(value);

            var length = value.Length;
            var lengthForBuilder = EstimateEscapedStringLength(length);
            var charsToAdd = 0;
            StringBuilder sb = null;

            for (var i = 0; i < length; i++)
            {
                switch (value[i])
                {
                    case '\"':
                        UpdateEscapeInternals("\"\"", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '{':
                    case '}':
                        if (isValueAnInterpolatedString)
                        {
                            UpdateEscapeInternals($"{value[i]}{value[i]}", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        }

                        break;
                    default:
                        charsToAdd++;
                        break;
                }
            }

            // The original string wasn't modified.
            if (sb == null)
            {
                return value;
            }

            if (charsToAdd > 0)
            {
                sb.Append(value, length - charsToAdd, charsToAdd);
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(char ch)
        {
            if (ch < 128)
            {
                return ((uint)ch - '0') <= 9;
            }

            return (GetUniCatFlags(ch) & UniCatFlags.DecimalDigitNumber) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFormatCh(char ch)
        {
            return ch >= 128 && (GetUniCatFlags(ch) & UniCatFlags.Format) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLatinAlpha(char ch)
        {
            return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniCatFlags GetUniCatFlags(char ch)
        {
            return (UniCatFlags)(1u << (int)CharUnicodeInfo.GetUnicodeCategory(ch));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSpace(char ch)
        {
            if (ch >= 128)
            {
                return (GetUniCatFlags(ch) & UniCatFlags.SpaceSeparator) != 0;
            }

            // Character is regular space or tab
            return ch == 32 || IsTabulation(ch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasSpaces(string str)
        {
            var length = str.Length;

            for (var i = 0; i < length; i++)
            {
                if (IsSpace(str[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTabulation(char ch)
        {
            switch (ch)
            {
                // character tabulation
                case '\u0009':
                // line tabulation
                case '\u000B':
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLineTerm(char ch)
        {
            switch (ch)
            {
                // line feed, unicode 0x000A
                case '\n':
                // carriage return, unicode 0x000D
                case '\r':
                // Unicode next line
                case '\u0085':
                // Unicode line separator
                case '\u2028':
                // Unicode paragraph separator
                case '\u2029':
                // form feed
                case '\u000C':
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EstimateEscapedStringLength(int length)
        {
            return Math.Max((length * 112) / 100, length + 8);
        }

        private static string InternalEscapeString(string value, int length, int lengthForBuilder, ref StringBuilder sb, bool finalizeBuilder)
        {
            var charsToAdd = 0;

            for (var i = 0; i < length; i++)
            {
                switch (value[i])
                {
                    case '\\':
                        UpdateEscapeInternals("\\\\", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\"':
                        UpdateEscapeInternals("\\\"", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\'':
                        UpdateEscapeInternals("\\\'", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\0':
                        UpdateEscapeInternals("\\0", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\b':
                        UpdateEscapeInternals("\\b", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\t':
                        UpdateEscapeInternals("\\t", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\n':
                        UpdateEscapeInternals("\\n", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\v':
                        UpdateEscapeInternals("\\v", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\f':
                        UpdateEscapeInternals("\\f", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\r':
                        UpdateEscapeInternals("\\r", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\u0085':
                        UpdateEscapeInternals("\\u0085", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\u2028':
                        UpdateEscapeInternals("\\u2028", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    case '\u2029':
                        UpdateEscapeInternals("\\u2029", value, lengthForBuilder, i, ref charsToAdd, ref sb);
                        break;
                    default:
                        charsToAdd++;
                        break;
                }
            }

            // The original string wasn't modified.
            if (sb == null)
            {
                return value;
            }

            if (charsToAdd > 0)
            {
                sb.Append(value, length - charsToAdd, charsToAdd);
            }

            return finalizeBuilder ? StringBuilderCache.GetStringAndRelease(sb) : string.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateEscapeInternals(string escapedValue, string input, int estimatedLength, int currentPosition, ref int charsToAdd, ref StringBuilder sb)
        {
            if (sb == null)
            {
                sb = StringBuilderCache.Acquire(estimatedLength);
                sb.Append(input, 0, currentPosition);
                charsToAdd = 0;
            }
            else if (charsToAdd > 0)
            {
                sb.Append(input, currentPosition - charsToAdd, charsToAdd);
                charsToAdd = 0;
            }

            sb.Append(escapedValue);
        }

        // If a string is going to be inserted into a string which is then used as a format string, it needs to be escaped first
        // eg foo = string.Format("bad column name {0}", bar);
        // xyz = string.Format(foo, "hello");
        // This will die if bar is "{hmm}" for example, fix is to wrap bar with this function
        public static string MakeSafeForFormatString(string value)
        {
            Contracts.AssertNonEmpty(value);

            return value.Replace("{", "{{").Replace("}", "}}");
        }
    }
}
