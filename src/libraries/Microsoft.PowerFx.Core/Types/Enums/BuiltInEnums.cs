// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Logging;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    internal static class BuiltInEnums
    {
        public static readonly EnumSymbol ColorEnum = new EnumSymbol(
            new DName(LanguageConstants.ColorEnumString),
            DType.Color,
            ColorTable.InvariantNameToHexMap.Select(kvp => new KeyValuePair<string, object>(kvp.Key, Convert.ToDouble(kvp.Value))),
            canCoerceToBackingKind: true);

        public static readonly EnumSymbol StartOfWeekEnum = new EnumSymbol(
            new DName(LanguageConstants.StartOfWeekEnumString),
            DType.Number,
            new Dictionary<string, object>()
            {
                { "Sunday", 1 },
                { "Monday", 2 },
                { "MondayZero", 3 },
                { "Tuesday", 12 },
                { "Wednesday", 13 },
                { "Thursday", 14 },
                { "Friday", 15 },
                { "Saturday", 16 },
            });

        public static readonly EnumSymbol DateTimeFormatEnum = new EnumSymbol(
            new DName(LanguageConstants.DateTimeFormatEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "LongDate", "'longdate'" },
                { "ShortDate", "'shortdate'" },
                { "LongTime", "'longtime'" },
                { "ShortTime", "'shorttime'" },
                { "LongTime24", "'longtime24'" },
                { "ShortTime24", "'shorttime24'" },
                { "LongDateTime", "'longdatetime'" },
                { "ShortDateTime", "'shortdatetime'" },
                { "LongDateTime24", "'longdatetime24'" },
                { "ShortDateTime24", "'shortdatetime24'" },
                { "UTC", "utc" }
            },
            canCoerceFromBackingKind: true);

        public static readonly EnumSymbol TimeUnitEnum = new EnumSymbol(
            new DName(LanguageConstants.TimeUnitEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Years", "years" },
                { "Quarters", "quarters" },
                { "Months", "months" },
                { "Days", "days" },
                { "Hours", "hours" },
                { "Minutes", "minutes" },
                { "Seconds", "seconds" },
                { "Milliseconds", "milliseconds" },
            });

        public static readonly EnumSymbol SortOrderEnum = new EnumSymbol(
            new DName(LanguageConstants.SortOrderEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Ascending", "ascending" },
                { "Descending", "descending" },
            });

        public static readonly EnumSymbol MatchOptionsEnum = new EnumSymbol(
            new DName(LanguageConstants.MatchOptionsEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "BeginsWith", MatchOptionString.BeginsWith },
                { "EndsWith", MatchOptionString.EndsWith },
                { "Complete", MatchOptionString.Complete },
                { "Contains", MatchOptionString.Contains },
                { "IgnoreCase", MatchOptionString.IgnoreCase },
                { "Multiline", MatchOptionString.Multiline },
                { "FreeSpacing", MatchOptionString.FreeSpacing },
                { "DotAll", MatchOptionString.DotAll },
                { "NumberedSubMatches", MatchOptionString.NumberedSubMatches }
            },
            canConcatenateStronglyTyped: true);

        // For Match.Email, our objective is to define a simple, general purpose RE that matches most common email addresses
        // - Useful for validating user input as looking like an email address when filling out a form.
        // - Useful for extracint email addresses from a longer text stream.
        // - We are not trying to validate every detail of the email address and domain name RFCs. We will accept email addresses that aren't legal.
        // - If someone wants tighter boundaries, they are free to bring their own RE. The web has many examples, each tuned for various needs.
        // - Composable with MatchOptions.BeginsWith, MatchOptions.EndsWith, and MatchOptions.Complete.
        // - Support Unicode characters, interantional and emoji, looking to the future, including Iternationalized Domain Names
        //
        // Our definition is exclusionary as international Unicode characters and emojis are somewhat accepted as both an email address and in domain names.  
        // We are excluding punctuation that is very unlikely to be allowed in either of these areas in the future, and accepting most everything else.
        // It is helpful not to include common punctuation so that an email address can be extracted from, for example "Bob Contoso <bob@contoso.com>" and "Welcome bob@contoso.com!".
        //
        // Here is the breakdown of the RE:
        //   (?:                                     # Entire thing is wrapped in a group, so that a quantifier could be used with it (such as a ?).
        //     (?:[\{\}] |                           # These are excluded by the \p{P...} next, but should be included as per RFC 822, so added back here.
        //        [^\s@<>,;:\\""                     # Excluding spaces and a few punctuation characters, more covered next; international letters, numbers, emojis, etc. are included
        //        \p{Pi}\p{Ps}\p{Pe}\p{Pf}]          # Excluding "specials" in the RFC, with international end and start punctuation, such as ( [ “ « 「
        //     )+                                    # At least one character is required
        //     @                                     # The @ character in "a@b.com"
        //     (?:
        //       (?:
        //         [\-                               # Besides lowercase ASCII letters, - is the only allowed character in base hostnames.
        //           _                               # Although illegal, we are accepting underscores as a hostname character as some use it.
        //           \xb7\u05f3\u05f4\u0f0b\u30fb]   # These are the five characters that IDNA 2008 supports that are in \p{P} (Po to be exact) and would otherwise not be supported by the next line
        //         | [^\s\p{P}<=>+\|]                # Everything but spaces, all punctuation, and common ASCII delimiters (in \p{Sm} but we don't need all of that). This does not exclude IDNA or emojis.
        //       )\.                                 # The dot before subdomains, domains, and top level domains, the dot in "a@b.com"
        //     )+                                    # At least one character and then one dot is needed before the top level domain. Prevents two dots, one dot at beginning.
        //     (?:[\-                                # Same as above, but without the trailing .
        //          _                                #
        //          \xb7\u05f3\u05f4\u0f0b\u30fb]    # 
        //        | [^\s\p{P}<=>+\|]                 # 
        //     )+                                    # At least one character in the TLD. Two is the practical lower bound today, but there is talk of single character for CJK in the future
        //   )
        //
        // local-part (before the @) is driven by:
        // - The "specials" production in https://www.w3.org/Protocols/rfc822/3_Lexical.html#z2
        // - In our RE, \p{Pi}\p{Ps}\p{Pe}\p{Pf} is inspired by "specials" including ( ) [ ] and ", we internationalize it, and it is useful to have these delimiters for email address extraction.
        // - Technically it is possible to include these characters in an email address today - there are no character bounds on SMTPUTF8 - but it is highly unlikely and discouraged. Some work to better define this: https://www.ietf.org/archive/id/draft-ietf-mailmaint-smtputf8-syntax-00.html
        // - We don't support the uncommon quoted email addresses or comments in email addresses. https://datatracker.ietf.org/doc/html/rfc5321 states that systems shouldn't use the Quoted-string form.
        // 
        // domain-name and top-level-domain (after the @) is driven by:
        // - ASCII:
        //     - Original domain names only supported [a-z0-9\-]. Domain names are case insensitive, so effectively [A-Z] is also supported.
        //     - Beyond \p{P}, < = > + | are commonly called out as illegal characters and excluded here.  < > in particular is commonly used to wrap email addresses with a companion display name.
        //     - Although illegal, we are accepting underscores as a hostname character as some use it.
        // - Internationalized domain names:
        //     - See https://learn.microsoft.com/en-us/globalization/reference/idn and https://en.wikipedia.org/wiki/Internationalized_domain_name for general introduction to IDN
        //     - For IDNs, almost all punctuation characters not included in https://www.unicode.org/Public/idna/idna2008derived/Idna2008-16.0.0.txt, but a few exceptions noted in https://datatracker.ietf.org/doc/html/rfc5892 and included in the RE.
        // - Emojis in domain names:
        //     - Is possible and supported in some TLDs today https://en.wikipedia.org/wiki/Emoji_domain, so we should be prepared for this to be more common, which is why we don't use an inclusive RE with some combination of \p{L}\p{N}\p{M}
        //     - But unlikely to become common place as it is discouraged by ICANN for good reasons https://itp.cdn.icann.org/en/files/security-and-stability-advisory-committee-ssac-reports/sac-095-en.pdf
        // - Domain name lengths:
        //     - Second level domains can be one character long https://en.wikipedia.org/wiki/Single-letter_second-level_domain.
        //     - There are no single letter top level domains at this time and unlikely to be added, as two letter are reserved for country codes and new gTLD applicants must be 3 characters or more https://newgtlds.icann.org/en/applicants/global-support/faqs/faqs-en

        public static readonly EnumSymbol MatchEnum = new EnumSymbol(
            new DName(LanguageConstants.MatchEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Any", "." },
                { "Comma", "," },
                { "Digit", "\\d" },
                { "Email", @"(?:(?:[\{\}]|[^\s@<>,;:\\""\p{Pi}\p{Ps}\p{Pe}\p{Pf}])+@(?:(?:[\-_\xb7\u05f3\u05f4\u0f0b\u30fb]|[^\s\p{P}<=>+\|])+\.)+(?:[\-_\xb7\u05f3\u05f4\u0f0b\u30fb]|[^\s\p{P}<=>+\|])+)" },
                { "Hyphen", "-" },
                { "LeftParen", "\\(" },
                { "Letter", "\\p{L}" },
                { "MultipleDigits", "\\d+" },
                { "MultipleLetters", "\\p{L}+" },
                { "MultipleNonSpaces", "\\S+" },
                { "MultipleSpaces", "\\s+" },
                { "NonSpace", "\\S" },
                { "OptionalDigits", "\\d*" },
                { "OptionalLetters", "\\p{L}*" },
                { "OptionalNonSpaces", "\\S*" },
                { "OptionalSpaces", "\\s*" },
                { "Period", "\\." },
                { "RightParen", "\\)" },
                { "Space", "\\s" },
                { "Tab", "\\t" }
            },
            canCoerceFromBackingKind: true,
            canConcatenateStronglyTyped: true);

        // This is the previous definition of the Match enum used by Power Apps pre-V1. Provided here for Power Apps' use.
        public static readonly EnumSymbol MatchEnumPreV1 = new EnumSymbol(
            new DName(LanguageConstants.MatchEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Any", "." },
                { "Comma", "," },
                { "Digit", "\\d" },
                { "Email", ".+@.+\\.[^.]{2,}" }, // differs from V1, suffers from not not excluding spaces, matching the entire sentence of "Welcome bob@contoso.com!".
                { "Hyphen", "\\-" },             // differs from V1, escaped hyphen not supported by Power Fx and JavaScript outside of character classes
                { "LeftParen", "\\(" },
                { "Letter", "\\p{L}" },
                { "MultipleDigits", "\\d+" },
                { "MultipleLetters", "\\p{L}+" },
                { "MultipleNonSpaces", "\\S+" },
                { "MultipleSpaces", "\\s+" },
                { "NonSpace", "\\S" },
                { "OptionalDigits", "\\d*" },
                { "OptionalLetters", "\\p{L}*" },
                { "OptionalNonSpaces", "\\S*" },
                { "OptionalSpaces", "\\s*" },
                { "Period", "\\." },
                { "RightParen", "\\)" },
                { "Space", "\\s" },
                { "Tab", "\\t" }
            },
            canCoerceFromBackingKind: true,
            canConcatenateStronglyTyped: true);

        public static readonly EnumSymbol ErrorKindEnum = new EnumSymbol(
            new DName(LanguageConstants.ErrorKindEnumString),
            DType.Number,
            new Dictionary<string, object>()
            {
                { "None", 0 },
                { "Sync", 1 },
                { "MissingRequired", 2 },
                { "CreatePermission", 3 },
                { "EditPermissions", 4 },
                { "DeletePermissions", 5 },
                { "Conflict", 6 },
                { "NotFound", 7 },
                { "ConstraintViolated", 8 },
                { "GeneratedValue", 9 },
                { "ReadOnlyValue", 10 },
                { "Validation", 11 },
                { "Unknown", 12 },
                { "Div0", 13 },
                { "BadLanguageCode", 14 },
                { "BadRegex", 15 },
                { "InvalidFunctionUsage", 16 },
                { "FileNotFound", 17 },
                { "AnalysisError", 18 },
                { "ReadPermission", 19 },
                { "NotSupported", 20 },
                { "InsufficientMemory", 21 },
                { "QuotaExceeded", 22 },
                { "Network", 23 },
                { "Numeric", 24 },
                { "InvalidArgument", 25 },
                { "Internal", 26 },
                { "NotApplicable", 27 },
                { "Timeout", 28 },
                { "ServiceUnavailable", 29 },
                { "InvalidJSON", 30 },
                { "Custom", 1000 },
            },
            canCompareNumeric: true,
            canCoerceToBackingKind: true);

        public static readonly EnumSymbol JSONFormatEnum = new EnumSymbol(
            new DName(LanguageConstants.JSONFormatEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Compact", string.Empty },
                { "IndentFour", "4" },
                { "IgnoreBinaryData", "G" },
                { "IncludeBinaryData", "B" },
                { "IgnoreUnsupportedTypes", "I" },
                { "FlattenValueTables", "_" }
            },
            canConcatenateStronglyTyped: true);

        public static readonly EnumSymbol TraceSeverityEnum = new EnumSymbol(
            new DName(LanguageConstants.TraceSeverityEnumString),
            DType.Number,
            TraceSeverityDictionary());

        private static Dictionary<string, object> TraceSeverityDictionary()
        {
            var traceDictionary = new Dictionary<string, object>();
            foreach (TraceSeverity severity in Enum.GetValues(typeof(TraceSeverity)))
            {
                traceDictionary[severity.ToString()] = (int)severity;
            }

            return traceDictionary;
        }

        public static readonly EnumSymbol TraceOptionsEnum = new EnumSymbol(
            new DName(LanguageConstants.TraceOptionsEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "None", "none" },
                { "IgnoreUnsupportedTypes", TraceFunction.IgnoreUnsupportedTypesEnumValue },
            },
            canConcatenateStronglyTyped: true);

        public static readonly EnumSymbol JoinTypeEnum = new EnumSymbol(
            new DName(LanguageConstants.JoinTypeEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Inner", "inner" },
                { "Left", "left" },
                { "Right", "right" },
                { "Full", "full" },
            });

        public static readonly EnumSymbol MapLengthEnum = new EnumSymbol(
            new DName(LanguageConstants.MapLengthEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Shortest", "shortest" },
                { "Longest", "longest" },
                { "Equal", "equal" },
                { "First", "first" },
            });
    }
}
