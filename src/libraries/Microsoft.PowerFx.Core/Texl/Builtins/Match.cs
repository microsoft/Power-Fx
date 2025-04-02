// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsMatch(text:s, regular_expression:s, [options:s])
    internal class IsMatchFunction : BaseMatchFunction
    {
        public IsMatchFunction(RegexTypeCache regexTypeCache)
            : base("IsMatch", TexlStrings.AboutIsMatch, DType.Boolean, regexTypeCache)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsMatchArg1, TexlStrings.IsMatchArg2 };
            yield return new[] { TexlStrings.IsMatchArg1, TexlStrings.IsMatchArg2, TexlStrings.IsMatchArg3 };
        }
    }

    // Match(text:s, regular_expression:s, [options:s])
    internal class MatchFunction : BaseMatchFunction
    {
        public MatchFunction(RegexTypeCache regexTypeCache)
            : base("Match", TexlStrings.AboutMatch, DType.EmptyRecord, regexTypeCache)
        {
        }
    }

    // MatchAll(text:s, regular_expression:s, [options:s])
    internal class MatchAllFunction : BaseMatchFunction
    {
        public MatchAllFunction(RegexTypeCache regexTypeCache)
            : base("MatchAll", TexlStrings.AboutMatchAll, DType.EmptyTable, regexTypeCache)
        {
        }
    }

    // These start with the codes that can come after a regular expression definition in Perl/JavaScript with "/a+/misx" and can also be used in "(?misx)a+".
    // If possible, do not add lower case letters that are Power Fx specific to avoid future conflicts with the industry. We added ^, $, and N as Power Fx specific.
    internal class MatchOptionChar
    {
        public const char Begins = '^';                          // invented by us, adds a '^' at the front of the regex
        public const char Ends = '$';                            // invented by us, adds a '$' at the end of the regex
        public const char IgnoreCase = 'i';
        public const char Multiline = 'm';
        public const char FreeSpacing = 'x';                     // we don't support the double 'xx' mode
        public const char DotAll = 's';                          // otherwise known as "singleline" in other flavors, hence the 's', but note is not the opposite of "multiline"
        public const char ExplicitCapture = 'n';                 // default for Power Fx, can be asserted too for compatibility with inline option (not exposed through Power Fx enum)
        public const char NumberedSubMatches = 'N';              // invented by us, opposite of ExplicitCapture and can't be used together
        public const char ContainsBeginsEndsComplete = 'c';      // invented by us, something to wrap ^ and $ around
    }

    // We insert with the string, and the enums are based on the string. We test with the char, above.
    // It makes a difference when we test for an existing beginswith/endswith/contains/complete directive with a single "c" char, which is inserted for all of these.
    // There isn't a good way to get a constant string from a constant char in C#, so duplicating in close proximity
    internal class MatchOptionString
    {
        public const string BeginsWith = "^c";                    // invented by us, adds a '^' at the front of the regex
        public const string EndsWith = "c$";                      // invented by us, adds a '$' at the end of the regex
        public const string IgnoreCase = "i";
        public const string Multiline = "m";
        public const string FreeSpacing = "x";                    // we don't support the double 'xx' mode
        public const string DotAll = "s";                         // otherwise known as "singleline" in other flavors, hence the 's', but note is not the opposite of "multiline"
        public const string NumberedSubMatches = "N";             // invented by us, opposite of ExplicitCapture and can't be used together
        public const string Contains = "c";                       // invented by us, something to wrap ^ and $ around
        public const string Complete = "^c$";                     // invented by us, with the ^ and $ around
    }

    // What we consider a newline character for the definition of non-dotall . and multiline ^ $
    // We follow the Unicode recommendations in https://unicode.org/reports/tr13/tr13-9.html and https://www.unicode.org/reports/tr18/#RL1.6.
    // \n, \r, and \r\n are obvious, covering Unix and Windows, Power Apps uses \r\n, and supported by Excel with PCRE2 (ANYCRLF)
    // \u2028 and \u2029 are supported by JavaScript and are generally recommended as unambiguous line and paragraph separators
    // \x85 is supported by many regular expression languages
    // \v and \f were debated, but ultimatly supported since it is the Unicode recommendation, Microsoft Word uses \v for line seperation, and it is compatible with PCRE2 (ANY).
    // We transpile these on .NET and JavaScript into a character class
    internal class MatchWhiteSpace
    {
        public const string NewLineEscapesWithoutCRLF = @"\x0b\x0c\x85\u2028\u2029";
        public const string NewLineDoubleEscapesWithoutCRLF = @"\\x0b\\x0c\\x85\\u2028\\u2029";

        public const string NewLineEscapes = @"\r\n" + NewLineEscapesWithoutCRLF;
        public const string NewLineDoubleEscapes = @"\\r\\n" + NewLineDoubleEscapesWithoutCRLF;

        public const string SpaceEscapes = @"\x09\p{Z}";                                // must use with /u or /v modifier flag on JavaScript
        public const string SpaceDoubleEscapes = @"\\x09\\p{Z}";                        // must use with /u or /v modifier flag on JavaScript

        public const string SpaceNewLineEscapes = @"\p{Z}\x09-\x0d\x85";                // must use with /u or /v modifier flag on JavaScript
        public const string SpaceNewLineDoubleEscapes = @"\\p{Z}\\x09-\\x0d\\x85";      // must use with /u or /v modifier flag on JavaScript

        public static bool IsSpaceNewLine(char c) => char.IsWhiteSpace(c);  // matches NewLineEscapes + SpaceEscapes

        public static bool IsNewLine(char c) => c == '\r' || c == '\n' || c == '\x0b' || c == '\x0c' || c == '\x85' || c == '\u2028' || c == '\u2029';
    }

    internal class BaseMatchFunction : BuiltinFunction
    {
        private readonly RegexTypeCache _regexTypeCache;
        private readonly string _cachePrefix;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public override bool UseParentScopeForArgumentSuggestions => true;

        public BaseMatchFunction(string functionName, TexlStrings.StringGetter aboutGetter, DType returnType, RegexTypeCache regexTypeCache)
            : base(functionName, aboutGetter, FunctionCategories.Text, returnType, 0, 2, 3, DType.String, BuiltInEnums.MatchEnum.FormulaType._type, BuiltInEnums.MatchOptionsEnum.FormulaType._type)
        {
            if (regexTypeCache != null)
            {
                _cachePrefix = returnType == DType.Boolean ? "bol_" : (returnType.IsTable ? "tbl_" : "rec_");
                _regexTypeCache = regexTypeCache;
            }
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MatchArg1, TexlStrings.MatchArg2 };
            yield return new[] { TexlStrings.MatchArg1, TexlStrings.MatchArg2, TexlStrings.MatchArg3 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.MatchEnumString, LanguageConstants.MatchOptionsEnumString };
        }

        public override bool HasSuggestionsForParam(int index)
        {
            Contracts.Assert(index >= 0);

            return index <= 2;
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 2 || args.Length == 3);
            Contracts.AssertValue(errors);

            bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsRecord || returnType.IsTable || returnType == DType.Boolean);

            var regExNode = args[1];

            if ((argTypes[1].Kind != DKind.String && argTypes[1].Kind != DKind.OptionSetValue) || !BinderUtils.TryGetConstantValue(context, regExNode, out var regularExpression))
            {
                errors.EnsureError(regExNode, TexlStrings.ErrVariableRegEx);
                return false;
            }

            string regularExpressionOptions = string.Empty;

            if (args.Length == 3)
            {
                var goodTypeAndConstant = false;

                if (argTypes[2].Kind == DKind.String || argTypes[2].Kind == DKind.OptionSetValue)
                {
                    goodTypeAndConstant = BinderUtils.TryGetConstantValue(context, args[2], out regularExpressionOptions);
                }

                if (context.Features.PowerFxV1CompatibilityRules && !goodTypeAndConstant)
                {
                    errors.EnsureError(args[2], TexlStrings.ErrVariableRegExOptions);
                    return false;
                }
                else if (!context.Features.PowerFxV1CompatibilityRules && goodTypeAndConstant && (regularExpressionOptions.Contains(MatchOptionChar.DotAll) || regularExpressionOptions.Contains(MatchOptionChar.FreeSpacing)))
                {
                    // some options are not available pre-V1, we leave the enum value in place and compile time error
                    // we can't detect this if not a constant string, which is supported by pre-V1 but is very uncommon
                    errors.EnsureError(args[2], TexlStrings.ErrInvalidRegExV1Options, args[2]);
                    return false;
                }

                // having pre-V1 variable regex options that we can't process here is OK, as pre-V1 options would not impact the schema.
                // regularExpressionOptions will remain empty for the cache work below which is fine.
            }

            if (!context.Features.PowerFxV1CompatibilityRules)
            {
                // only used for the following analysis and type creation, not modified in the IR
                regularExpressionOptions += MatchOptionChar.NumberedSubMatches;
            }

            if (!fValid)
            {
                return false;
            }

            // Cache entry can vary on:
            // - Table (MatchAll) vs. Record (Match)
            // - Regular expression pattern
            // - NumberedSubMatches vs. Not
            // if another MatchOption is added which impacts the return type, this will need to be updated
            string regexCacheKey = RegexCacheKeyGen(this._cachePrefix, regularExpressionOptions, regularExpression);

            // if the key is found in the cache, then the regular expression must have previously passed IsSupportedRegularExpression (or we are pre V1 and we don't check)
            if (!_regexTypeCache.TryLookup(regexCacheKey, out var typeCacheEntry))
            {
                string alteredOptions = regularExpressionOptions;

                if (context.Features.PowerFxV1CompatibilityRules)
                {
                    // will fill in typeCacheEntry with error information if there is a problem
                    typeCacheEntry = IsSupportedRegularExpression(regExNode, regularExpression, regularExpressionOptions, out alteredOptions);
                }
                
                if (typeCacheEntry == null)
                { 
                    // either didn't check (pre-V1) or no error
                    typeCacheEntry = RegexCacheGetType(regularExpression, alteredOptions, returnType);
                }

                _regexTypeCache.Add(regexCacheKey, typeCacheEntry);
            }

            if (typeCacheEntry.Error != null)
            {
                // remove newlines and other characters that don't display
                // surrogate pairs will be fine
                var cleanParam = Regex.Replace(typeCacheEntry.ErrorParam ?? string.Empty, @"[\p{Cc}\p{Cf}\p{Z}]+", " ").Replace("\"", "\"\"");

                errors.EnsureError(typeCacheEntry.ErrorSeverity, regExNode, (ErrorResourceKey)typeCacheEntry.Error, cleanParam);
                if (typeCacheEntry.ErrorSeverity == DocumentErrorSeverity.Severe)
                {
                    return false;
                }
            } 

            returnType = typeCacheEntry.ReturnType;
            return true;
        }

        private string RegexCacheKeyGen(string prefix, string options, string regex)
        {
            // include any options that could impact the output schema or change the way the regular expression is parsed for validation
            return prefix + (options.Contains(MatchOptionChar.NumberedSubMatches) ? "N" : "-") + (options.Contains(MatchOptionChar.FreeSpacing) ? "X_" : "~_") + regex;
        }

        // Creates a typed result: [Match:s, Captures:*[Value:s], NamedCaptures:r[<namedCaptures>:s]]
        private RegexTypeCacheEntry RegexCacheGetType(string regexPattern, string alteredOptions, DType initialReturnType)
        {
            var regexDotNetOptions = RegexOptions.None;
            Regex regex;

            if (alteredOptions.Contains(MatchOptionChar.FreeSpacing))
            {
                regexDotNetOptions |= RegexOptions.IgnorePatternWhitespace;

                // In x mode, comment line endings are any newline character (as per PCRE2), but .NET only supports \n.
                // For our purposes here to determine the type, we can just replace all the other newline characters wtih \n.
                var regexPatternWhitespace = new Regex("[" + MatchWhiteSpace.NewLineEscapes + "]");
                regexPattern = regexPatternWhitespace.Replace(regexPattern, "\n");
            }

            // .NET doesn't support \u{...} notation, so we translate to \u... notation (no curly braces).
            // We retain as much of the original as possible for error message context, if an error is encountered after this.
            // Canvas pre-V1 allowed \u{...}, with as many digits as desired (but no spaces), so long as the result was less that or equal to 0xffff.
            string ReplaceUCurly(Match m)
            {
                // hex will be limited to 6 hex digits by the regular expression, so will not overflow or be negative
                if (int.TryParse(m.Groups["hex"].Value, NumberStyles.HexNumber, null, out var hex))
                {
                    if (hex <= 0xffff)
                    {
                        return "\\u" + hex.ToString("X4", CultureInfo.InvariantCulture);
                    }
                    else if (hex <= 0x10ffff)
                    {
                        var highSurr = 0xd800 + (((hex - 0x10000) >> 10) & 0x3ff);
                        var lowSurr = 0xdc00 + ((hex - 0x10000) & 0x3ff);
                        return "\\u" + highSurr.ToString("X4", CultureInfo.InvariantCulture) + "\\u" + lowSurr.ToString("X4", CultureInfo.InvariantCulture);
                    }
                }

                // number is out of range, just pass it through and the .NET regex engine will error
                return m.Value;
            }

            regexPattern = Regex.Replace(regexPattern, "\\\\u\\{0*(?<hex>[0-9a-fA-F]{1,6})\\}", new MatchEvaluator(ReplaceUCurly));

            // always .NET compile the regular expression, even if we don't need the return type (boolean), to ensure it is legal in .NET
            try
            {
                regex = new Regex(regexPattern, regexDotNetOptions);
            }
            catch (ArgumentException exception)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                return GetRegexErrorEntry(regexPattern, exception);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            // we don't need to check hidden or do any type calculation if the return type is Boolean (IsMatch)
            if (initialReturnType == DType.Boolean)
            {
                return new RegexTypeCacheEntry()
                {
                    ReturnType = DType.Boolean
                };
            }
            else
            {
                bool fullMatchHidden = false, subMatchesHidden = false, startMatchHidden = false;
                List<TypedName> propertyNames = new List<TypedName>();
                string errorParam = string.Empty;

                foreach (var captureName in regex.GetGroupNames())
                {
                    if (int.TryParse(captureName, out _))
                    {
                        // Unnamed captures are returned as integers, ignoring them
                        continue;
                    }

                    if (captureName == ColumnName_FullMatch.Value)
                    {
                        fullMatchHidden = true;
                        errorParam += ColumnName_FullMatch.Value + " ";
                    }
                    else if (captureName == ColumnName_SubMatches.Value && alteredOptions.Contains(MatchOptionChar.NumberedSubMatches))
                    {
                        subMatchesHidden = true;
                        errorParam += ColumnName_SubMatches.Value + " ";
                    }
                    else if (captureName == ColumnName_StartMatch.Value)
                    {
                        startMatchHidden = true;
                        errorParam += ColumnName_StartMatch.Value + " ";
                    }

                    propertyNames.Add(new TypedName(DType.String, DName.MakeValid(captureName, out _)));
                }

                if (!fullMatchHidden)
                {
                    propertyNames.Add(new TypedName(DType.String, ColumnName_FullMatch));
                }

                if (!subMatchesHidden && alteredOptions.Contains(MatchOptionChar.NumberedSubMatches))
                {
                    propertyNames.Add(new TypedName(DType.CreateTable(new TypedName(DType.String, ColumnName_Value)), ColumnName_SubMatches));
                }

                if (!startMatchHidden)
                {
                    propertyNames.Add(new TypedName(DType.Number, ColumnName_StartMatch));
                }

                var returnType = initialReturnType.IsRecord ? DType.CreateRecord(propertyNames) : DType.CreateTable(propertyNames);
                    
                if (fullMatchHidden || subMatchesHidden || startMatchHidden)
                {
                    return new RegexTypeCacheEntry()
                    {
                        ReturnType = returnType,
                        Error = errorParam.TrimEnd().Contains(' ') ? TexlStrings.InfoRegExCaptureNameHidesPredefinedPlural : TexlStrings.InfoRegExCaptureNameHidesPredefinedSingular,
                        ErrorSeverity = DocumentErrorSeverity.Suggestion,
                        ErrorParam = errorParam.TrimEnd()
                    };
                }
                else
                {
                    return new RegexTypeCacheEntry()
                    {
                        ReturnType = returnType
                    };
                }
            }
        }

        // This is only needed until everyone is on Power Fx V1, where our own error checking is done.
        // The reflection is only needed until we are all on .NET 5 or higher,
        // with support for RegexParserException https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regexparseexception
        // Errors are cached, so this should only need to be hit once for a given pattern.
        [Obsolete("Use with caution, .NET Reflection needed for fine grained regular expression error messages until we can move everyone up to Power Fx V1 or .NET 5+ as our minimum supported platform.")]
        private static RegexTypeCacheEntry GetRegexErrorEntry(string pattern, Exception exception)
        {
            var errorCode = exception.GetType().GetProperty("Error")?.GetValue(exception);
            var errorOffset = exception.GetType().GetProperty("Offset")?.GetValue(exception);

            if (errorCode != null && errorOffset != null)
            {
                var error = TexlStrings.ErrInvalidRegExWithContext;

                // values are from RegexParserError enum https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regexparseerror
                switch ((int)errorCode)
                {
                    case 0: // unknown
                        break;
                    case 1: // AlternationHasTooManyConditions
                    case 2: // AlternationHasMalformedCondition
                        // uncommon and not supported by V1 Power Fx regular expressions, use default message
                        break;
                    case 3: // InvalidUnicodePropertyEscape
                    case 4: // MalformedUnicodePropertyEscape
                        error = TexlStrings.ErrInvalidRegExBadUnicodeCategory;
                        break;
                    case 5: // UnrecognizedEscape
                        error = TexlStrings.ErrInvalidRegExBadEscape;
                        break;
                    case 6: // UnrecognizedControlCharacter        
                    case 7: // MissingControlCharacter
                        // uncommon and not supported by V1 Power Fx regular expressions, use default message
                        break;
                    case 8: // InsufficientOrInvalidHexDigits
                        error = TexlStrings.ErrInvalidRegExBadEscape;
                        break;
                    case 9: // QuantifierOrCaptureGroupOutOfRange
                        error = TexlStrings.ErrInvalidRegExNumberOverflow;
                        break;
                    case 10: // UndefinedNamedReference
                    case 11: // UndefinedNumberedReference
                        error = TexlStrings.ErrInvalidRegExBadBackRefNotDefined;
                        break;
                    case 12: // MalformedNamedReference
                        error = TexlStrings.ErrInvalidRegExBadNamedCaptureName;
                        break;
                    case 13: // UnescapedEndingBackslash
                        error = TexlStrings.ErrInvalidRegExEndsWithBackslash;
                        break;
                    case 14: // UnterminatedComment
                        error = TexlStrings.ErrInvalidRegExUnclosedInlineComment;
                        break;
                    case 15: // InvalidGroupingConstruct
                        error = TexlStrings.ErrInvalidRegExBadParen;
                        break;
                    case 16: // AlternationHasNamedCapture
                    case 17: // AlternationHasComment
                    case 18: // AlternationHasMalformedReference
                    case 19: // AlternationHasUndefinedReference
                        // not common, for conditional alternation, not supported by V1 Power Fx regular expressions
                        break;
                    case 20: // CaptureGroupNameInvalid
                    case 21: // CaptureGroupOfZero
                        error = TexlStrings.ErrInvalidRegExBadNamedCaptureName;
                        break;
                    case 22: // UnterminatedBracket
                        error = TexlStrings.ErrInvalidRegExUnterminatedSquare;
                        break;
                    case 23: // ExclusionGroupNotLast
                        // not common, not supported by V1 Power Fx regular expressions
                        break;
                    case 24: // ReversedCharacterRange
                        error = TexlStrings.ErrInvalidRegExCharacterClassRangeReverse;
                        break;
                    case 25: // ShorthandClassInCharacterRange
                        error = TexlStrings.ErrInvalidRegExCharacterClassCategoryUse;
                        break;
                    case 26: // InsufficientClosingParentheses
                        error = TexlStrings.ErrInvalidRegExUnclosedCaptureGroups;
                        break;
                    case 27: // ReversedQuantifierRange
                        error = TexlStrings.ErrInvalidRegExLowHighQuantifierFlip;
                        break;
                    case 28: // NestedQuantifiersNotParenthesized
                        error = TexlStrings.ErrInvalidRegExBadQuantifier;
                        break;
                    case 29: // QuantifierAfterNothing
                        error = TexlStrings.ErrInvalidRegExQuantifierOnNothing;
                        break;
                    case 30: // InsufficientOpeningParentheses
                        error = TexlStrings.ErrInvalidRegExUnopenedCaptureGroups;
                        break;
                    case 31: // UnrecognizedUnicodeProperty
                        error = TexlStrings.ErrInvalidRegExBadUnicodeCategory;
                        break;
                    default:
                        // go with the default
                        break;
                }

                int intOffset = Math.Min(Math.Max((int)errorOffset, 0), pattern.Length);

                return new RegexTypeCacheEntry()
                {
                    Error = error,
                    ErrorSeverity = DocumentErrorSeverity.Severe,
                    ErrorParam = intOffset > ErrorContextLength ? "..." + pattern.Substring(intOffset - ErrorContextLength, ErrorContextLength) : pattern.Substring(0, intOffset),
                };
            }
            else
            {
                return new RegexTypeCacheEntry()
                {
                    Error = TexlStrings.ErrInvalidRegEx,
                    ErrorSeverity = DocumentErrorSeverity.Severe,
                    ErrorParam = string.Empty,
                };
            }
        }

        // Power Fx regular expressions are limited to features that can be transpiled to native .NET (C# Interpreter), ECMAScript (Canvas), or PCRE2 (Excel).
        // We want the same results everywhere for Power Fx, even if the underlying implementation is different. Even with these limits in place there are some minor semantic differences but we get as close as we can.
        // These tests can be run through all three engines and the results compared with by setting ExpressionEvaluationTests.RegExCompareEnabled, a PCRE2 DLL and NodeJS must be installed on the system.
        //
        // In short, we use the insersection of canonical .NET regular expressions and ECMAScript 2024's "v" flag for escaping rules. 
        // Someday when "v" is more widely available, we can support more of its features such as set subtraction.
        // We chose to use canonical .NET instead of RegexOptions.ECMAScript because we wanted the unicode definitions for words. See https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options#ecmascript-matching-behavior
        //
        // In addition, Power Fx regular expressions are opinionated and try to eliminate some of the ambiguity in the common regular expression language:
        //     Numbered capture groups are disabled by default, and cannot be mixed with named capture groups.
        //     Octal character codes are not supported, use \x or \u instead.
        //     Literal ^, -, [, ], {, and } must be escaped when used in a character class.
        //     Escaping is only supported for special characters and unknown alphanumeric escape sequences are not supported.
        //     Unicode characters are used throughout.
        //     Newlines support Windows friendly \r\n as well as \r and \n.
        //
        // There are significant differences between how possibly empty groups are handled between implementations, therefore in certain situations they are blocked.
        // See Match_CaptureQuant.txt for test cases, which is important to run through both .net and node to determine coverage of the block (See #if MATCHCOMPARE).
        // 
        // See docs/regular-expressions.md for more details on the language supported.
        //
        // In addition, the Power Fx compiler uses the .NET regular expression engine to validate the expression and determine capture group names.
        // So, any regular expression that does not compile with .NET is also automatically disallowed.

        // Configurable constants
        public const int MaxNamedCaptureNameLength = 62;            // maximum length of a capture name, in UTF-16 code units. PCRE2 has a 128 limit, cut in half for possible UFT-8 usage.
        public const int MaxLookBehindPossibleCharacters = 250;     // maximum possible number of characters in a look behind. PCRE2 has a 255 limit.
        public const int MaxGroupStackDepth = 32 * 2;               // 2x maximum number of nested grouping levels (2 used per level), avoids performance issues with a complex group tree.
        public const int ErrorContextLength = 12;                   // number of characters to include in error message context excerpt from formula.

        private static RegexTypeCacheEntry IsSupportedRegularExpression(TexlNode regExNode, string regexPattern, string regexOptions, out string alteredOptions)
        {
            bool freeSpacing = regexOptions.Contains(MatchOptionChar.FreeSpacing);                 // can also be set with inline mode modifier
            bool numberedCpature = regexOptions.Contains(MatchOptionChar.NumberedSubMatches);      // can only be set here, no inline mode modifier

            alteredOptions = regexOptions;

            // Scans the regular expression for interesting constructs, ignoring other elements and constructs that are legal, such as letters and numbers.
            // Order of alternation is important. .NET regular expressions are greedy and will match the first of these that it can.
            // Many subexpressions here take advantage of this, matching something that is valid, before falling through to check for something that is invalid.
            // 
            // For example, consider testing "\\(\a)".  This will match <goodEscape> <openParen> <badEscape> <closeParen>.
            // <badEscapeAlpha> will report an error and stop further processing.
            // One might think that the "\a" could have matched <goodEscape>, but it will match <badEscapeAlpha> first because it is first in the RE.
            // One might think that the "\(" could have matched <goodEscape>, but the double backslashes will be consumed first, which is why it is important
            // to gather all the matches in a linear scan from the beginning to the end.
            //
            // Three regular expressions are utilized:
            // - escapeRE is a regular expression fragment that is shared by the other two, included at the beginning each of the others
            // - generalRE is used outside of a character class
            // - characterClassRE is used inside a character class

            const string escapeRE =
                @"
# leading backslash, escape sequences
                    (?<badOctal>\\0\d*)                                | # \0 and octal are not accepted, ambiguous and not needed (use \x instead)
                    \\k<(?<backRefName>\w+)>                           | # named backreference
                    \\(?<backRefNumber>[1-9]\d*)                       | # numeric backreference, must be enabled with MatchOptions.NumberedSubMatches
                    (?<goodUSurrPair>\\u[dD][89a-bA-B][0-9a-fA-F]{2}     # \u... syntax, valid surrogate pair
                                     \\u[dD][c-fC-F][0-9a-fA-F]{2}) |    # \u{...} syntax is not supported for surrogate pairs (same as JavaScript and PCRE2)
                    (?<badUSurr>\\u[dD][89a-fA-F][0-9a-fA-F]{2}     |    # \u... syntax, surrogate that is not in a pair as it didn't match <goodUSurrPair>
                         \\u\{0{0,2}[dD][89a-fA-F][0-9a-fA-F]{2}\})    | # \u{...} syntax, surrogates are not supported with this syntax
                    (?<goodEscape>\\
                            ([dfnrstw]                             |     # standard regex character classes, missing from .NET are aAeGzZv (no XRegExp support), other common are u{} and o
                            [\^\$\\\.\*\+\?\(\)\[\]\{\}\|\/]       |     # acceptable escaped characters with Unicode aware ECMAScript
                            [\#\ ]                                 |     # added for free spacing, always accepted for conssitency even in character classes, escape needs to be removed on Unicode aware ECMAScript
                            x[0-9a-fA-F]{2}                        |     # hex character, must be exactly 2 hex digits
                            u[0-9a-fA-F]{4}))                          | # unicode character, must be exactly 4 hex digits
                    (?<goodUSmall>\\u\{0{0,4}[0-9a-fA-F]{1,4})\}       | # small (< U+10000) \u{...} code point, can stay as one UTF-16 word, up to 8 hex digits (our arbitrary cutoff, at 32-bits)
                    (?<goodULarge>\\u\{0{0,2}
                            (0?[1-9a-fA-F]|10)[0-9a-fA-F]{4})\}        | # large (>=U+10000) \u{...} code point, must be broken into surrogate pairs, up to 8 hex digits (our arbitrary cutoff, at 32-bits)
                    \\(?<goodUEscape>[pP])\{(?<UCategory>[\w=:-]+)\}   | # Unicode character classes, extra characters here for a better error message
                    (?<goodAnchorOutsideCC>\\[bB])                     | # acceptable outside a character class, includes negative classes until we have character class subtraction, include \P for future MatchOptions.LocaleAware
                    (?<goodEscapeOutsideAndInsideCCIfPositive>\\[DWS]) |
                    (?<goodEscapeInsideCCOnly>\\[&\-!#%,;:<=>@`~\^])   | # https://262.ecma-international.org/#prod-ClassSetReservedPunctuator, others covered with goodEscape above
                    (?<badEscape>\\.)                                  | # all other escaped characters are invalid and reserved for future use
                ";

            const string elseRE =
                @"
# end of both REs, checks for surrogate pair characters and everything else
                    (?<elseUSurrPair>[\ud800-\udbff][\udc00-\udfff])   | # surrogate pairs need to be both inline characters, or both \u codes, but not mixed
                    (?<elseBadUSurr>[\ud800-\udfff])                   |
                    (?<else>.)
                ";

            Regex generalRE = new Regex(
                escapeRE +
                @"
# leading (?<, named captures
                    \(\?<(?<goodNamedCapture>[_\p{L}][_\p{L}\p{Nd}]*)> | # named capture group, name characters are the lowest common denonminator with Unicode PCRE2
                                                                         # .NET uses \w with \u200c and \u200d allowing a number in the first character (seems like it could be confused with numbered captures),
                                                                         # while JavaScript uses identifer characters, including $, and does not allow a number for the first character
                    (?<goodLookAhead>\(\?(=|!))                        |
                    (?<goodLookBehind>\(\?(<=|<!))                     | # Look behind has many limitations
                    (?<badBalancing>\(\?<\w*-\w*>)                     | # .NET balancing captures are not supported
                    (?<badNamedCaptureName>\(\?<[^>]*>)                | # bad named capture name, didn't match goodNamedCapture
                    (?<badSingleQuoteNamedCapture>\(\?'[^']*')         | # single quoted capture names are not supported

# leading (?, misc
                    (?<goodNonCapture>\(\?:)                           | # non-capture group, still need to track to match with closing paren
                    \A\(\?(?<goodInlineOptions>[imnsx]+)\)             | # inline options
                    (?<goodInlineComment>\(\?\#)                       | # inline comment
                    (?<badInlineOptions>\(\?(\w+|\w*-\w+)[\:\)])       | # inline options, including disable of options
                    (?<badConditional>\(\?\()                          | # .NET conditional alternations are not supported

# leading (, used for other special purposes
                    (?<badParen>\([\?\+\*].?)                          | # everything else unsupported that could start with a (, includes atomic groups, recursion, subroutines, branch reset, and future features

# leading ?\*\+, quantifiers
                    (?<badQuantifiers>[\*\+\?][\+\*])                  | # possessive (ends with +) and useless quantifiers (ends with *)
                    (?<goodOneOrMore>[\+]\??)                          | # greedy and lazy quantifiers
                    (?<goodZeroOrMore>[\*]\??)                         |
                    (?<goodZeroOrOne>[\?]\??)                          |

# leading {, exact and limited quantifiers
                    (?<badExact>{\d+}[\+\*\?])                         | # exact quantifier can't be used with a modifier, lazy makes no sense
                    {(?<goodExact>\d+)}                                | # standard exact quantifier, no optional lazy
                    (?<badLimited>{\d+,\d+}[\+\*])                     | # possessive and useless quantifiers
                    {(?<goodLimitedL>\d+),(?<goodLimitedH>\d+)}\??     | # standard limited quantifiers, with optional lazy
                    (?<badUnlimited>{\d+,}[\+\*])                      |
                    {(?<goodUnlimited>\d+),}\??                        |
                    (?<badCurly>[{}])                                  | # more constrained, blocks {,3} and Java/Rust semantics that does not treat this as a literal

# character class
                    (?<badEmptyCharacterClass>\[\]|\[^\])              | # some implementations support empty character class, with varying semantics; we do not
                    \[(?<characterClass>(\\\]|\\\[|[^\]\[])+)\]        | # does not accept empty character class
                    (?<badSquareBrackets>[\[\]])                       | # square brackets that are not escaped and didn't define a character class

# open and close regions
                    (?<openParen>\()                                   |
                    (?<closeParen>\))                                  |                                      
                    (?<alternation>\|)                                 |
                    (?<anchors>[\^\$])                                 |
                    (?<poundComment>\#)                                | # used in free spacing mode (to detect start of comment), treated as else otherwise
                    (?<newline>[" + MatchWhiteSpace.NewLineEscapes + @"])    | # used in free spacing mode (to detect end of comment), treated as else otherwise
                "
                + elseRE, 
                RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            Regex characterClassRE = new Regex(
                escapeRE +
                @"
                    (?<badHyphen>^-|-$)                                | # begin/end literal hyphen not allowed within character class, needs to be escaped (ECMAScript v)
                    (?<badInCharClass>  \/ | \| | \\             |       # https://262.ecma-international.org/#prod-ClassSetSyntaxCharacter
                        \{ | \} | \( | \) | \[ | \] | \^)              | # adding ^ for Power Fx, making it clear that the carets in [^^] have different meanings
                    (?<badDoubleInCharClass> << | == | >> | ::   |       # reserved pairs, see https://262.ecma-international.org/#prod-ClassSetReservedDoublePunctuator 
                        @@ | `` | ~~ | %% | && | ;; | ,, | !!    |       # and https://www.unicode.org/reports/tr18/#Subtraction_and_Intersection
                        \|\| | \#\# | \$\$ | \*\* | \+\+ | \.\.  |       # includes set subtraction 
                        \?\? | \^\^ | \-\-)                            | # 
                    (?<goodHyphen>-)                                   |
                "
                + elseRE, 
                RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            int captureNumber = 0;                                  // last numbered capture encountered
            var groupTracker = new GroupTracker();

            bool openPoundComment = false;                          // there is an open end-of-line pound comment, only in freeFormMode
            bool openInlineComment = false;                         // there is an open inline comment
            int openInlineCommentStart = 0;                         // start of the inlince comment, for error reporting

            foreach (Match token in generalRE.Matches(regexPattern))
            {
                RegexTypeCacheEntry RegExError(ErrorResourceKey? error, int index = -1, int len = 1, bool startContext = false, bool endContext = false, string postContext = null)
                {
                    if (index == -1)
                    {
                        index = token.Index;
                        len = token.Length;
                    }

                    if (index + len < regexPattern.Length && char.IsHighSurrogate(regexPattern[index + len - 1]) && char.IsLowSurrogate(regexPattern[index + len]))
                    {
                        len++;
                    }

                    string found;

                    if (endContext)
                    {
                        var tokenEnd = index + len;
                        var tokenStart = tokenEnd > ErrorContextLength ? tokenEnd - ErrorContextLength : 0;
                        var tokenLen = tokenEnd > ErrorContextLength ? ErrorContextLength : tokenEnd;
                        if (char.IsLowSurrogate(regexPattern[tokenStart]))
                        {
                            tokenStart++;
                            tokenLen--;
                        }

                        found = (tokenStart == 0 ? string.Empty : "...") + regexPattern.Substring(tokenStart, tokenLen);
                    }
                    else if (startContext)
                    {
                        found = index + ErrorContextLength >= regexPattern.Length ? regexPattern.Substring(index) : regexPattern.Substring(index, ErrorContextLength) + "...";
                    }
                    else
                    {
                        // token won't start in the middle of a surrogate pair
                        found = regexPattern.Substring(index, len);
                    }

                    return new RegexTypeCacheEntry()
                    {
                        Error = error,
                        ErrorParam = found + postContext,
                        ErrorSeverity = DocumentErrorSeverity.Severe,
                    };
                }

                if (openPoundComment && token.Groups["newline"].Success)
                {
                    openPoundComment = false;
                }                
                else if (openInlineComment && (token.Groups["closeParen"].Success || token.Groups["goodEscape"].Value == "\\)"))
                {
                    // goodEscape with \\) too as you can't escape the closing paren of an inline comment
                    openInlineComment = false;
                }
                else if (!openPoundComment && !openInlineComment)
                {
                    if (token.Groups["anchors"].Success || token.Groups["goodAnchorOutsideCC"].Success)
                    {
                        // nothing to do
                    }
                    else if (token.Groups["goodZeroOrMore"].Success)
                    {
                        if (!groupTracker.SeenQuantifier(0, -1, out var error))
                        {
                            return RegExError(error, endContext: true);
                        }
                    }
                    else if (token.Groups["goodZeroOrOne"].Success)
                    {
                        if (!groupTracker.SeenQuantifier(0, 1, out var error))
                        {
                            return RegExError(error, endContext: true);
                        }
                    }
                    else if (token.Groups["goodExact"].Success)
                    {
                        if (!int.TryParse(token.Groups["goodExact"].Value, out var exact))
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExNumberOverflow);
                        }

                        if (!groupTracker.SeenQuantifier(exact, exact, out var error))
                        {
                            return RegExError(error, endContext: true);
                        }
                    }
                    else if (token.Groups["goodLimitedL"].Success)
                    {
                        if (!int.TryParse(token.Groups["goodLimitedL"].Value, out var low))
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExNumberOverflow);
                        }

                        if (!int.TryParse(token.Groups["goodLimitedH"].Value, out var high))
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExNumberOverflow);
                        }

                        if (!groupTracker.SeenQuantifier(low, high, out var error))
                        {
                            return RegExError((ErrorResourceKey)error, endContext: true);
                        }
                    }
                    else if (token.Groups["goodOneOrMore"].Success)
                    {
                        if (!groupTracker.SeenQuantifier(1, -1, out var error))
                        {
                            return RegExError(error, endContext: true);
                        }
                    }
                    else if (token.Groups["goodUnlimited"].Success)
                    {
                        if (!int.TryParse(token.Groups["goodUnlimited"].Value, out var low))
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExNumberOverflow);
                        }

                        if (!groupTracker.SeenQuantifier(low, -1, out var error))
                        {
                            return RegExError(error, endContext: true);
                        }
                    }
                    else if (token.Groups["else"].Success)
                    {
                        if (token.Value == "\\" && token.Index == regexPattern.Length - 1)
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExEndsWithBackslash);
                        }

                        groupTracker.SeenNonGroup();
                    }
                    else if (token.Groups["newline"].Success || token.Groups["goodEscape"].Success || token.Groups["goodUSmall"].Success || token.Groups["goodEscapeOutsideAndInsideCCIfPositive"].Success)
                    {
                        groupTracker.SeenNonGroup();
                    }
                    else if (token.Groups["goodUSurrPair"].Success || token.Groups["goodULarge"].Success || token.Groups["elseUSurrPair"].Success)
                    {
                        groupTracker.SeenNonGroup(min: 2, max: 2);
                    }
                    else if (token.Groups["characterClass"].Success)
                    {
                        bool characterClassNegative = token.Groups["characterClass"].Value[0] == '^';
                        string ccString = characterClassNegative ? token.Groups["characterClass"].Value.Substring(1) : token.Groups["characterClass"].Value;
                        int hyphenWait = 0;
                        int hyphenStart = 0;

                        foreach (Match ccToken in characterClassRE.Matches(ccString))
                        {
                            RegexTypeCacheEntry CCRegExError(ErrorResourceKey errKey)
                            {
                                return RegExError(
                                    errKey,
                                    index: ccToken.Index + token.Index + 1 + (characterClassNegative ? 1 : 0), // 1 for opening [ and possibly 1 more for ^
                                    len: ccToken.Length,
                                    endContext: true);
                            }

                            if (ccToken.Groups["goodEscape"].Success || ccToken.Groups["goodUSmall"].Success || ccToken.Groups["goodEscapeInsideCCOnly"].Success || ccToken.Groups["else"].Success)
                            {
                                int charVal;

                                if (ccToken.Groups["else"].Success)
                                {
                                    charVal = ccToken.Value[0];
                                }
                                else
                                {
                                    switch (ccToken.Value[1])
                                    {
                                        case 'r':
                                            charVal = 13;
                                            break;
                                        case 'n':
                                            charVal = 10;
                                            break;
                                        case 't':
                                            charVal = 9;
                                            break;
                                        case 'f':
                                            charVal = 12;
                                            break;
                                        case 'x':
                                        case 'u':
                                            if (!int.TryParse(ccToken.Value.Substring(ccToken.Groups["goodUSmall"].Success ? 3 : 2), NumberStyles.HexNumber, null, out charVal))
                                            {
                                                // should never happen, goodEscape match implies that we have valid hex digits in roughly Unicode range
                                                throw new Exception("hex character did not parse in character class");
                                            }

                                            break;
                                        case 'd':
                                        case 's':
                                        case 'w':
                                            charVal = -1;
                                            break;
                                        default:
                                            charVal = ccToken.Value[1];
                                            break;
                                    }
                                }

                                if (hyphenWait > 1 && charVal == -1)
                                {
                                    return CCRegExError(TexlStrings.ErrInvalidRegExCharacterClassCategoryUse);
                                }

                                if (hyphenWait > 1 && hyphenStart != -1 && charVal != -1 && (charVal - hyphenStart < 0))
                                {
                                    return CCRegExError(TexlStrings.ErrInvalidRegExCharacterClassRangeReverse);
                                }

                                hyphenStart = charVal;
                                hyphenWait--;
                            }
                            else if (ccToken.Groups["goodEscapeOutsideAndInsideCCIfPositive"].Success)
                            {
                                if (hyphenWait > 1)
                                {
                                    return CCRegExError(TexlStrings.ErrInvalidRegExCharacterClassCategoryUse);
                                }

                                if (characterClassNegative)
                                {
                                    return CCRegExError(TexlStrings.ErrInvalidRegExBadEscapeInsideNegativeCharacterClass);
                                }

                                hyphenStart = -1;
                                hyphenWait--;
                            }
                            else if (ccToken.Groups["goodUEscape"].Success)
                            {
                                if (hyphenWait > 1)
                                {
                                    return CCRegExError(TexlStrings.ErrInvalidRegExCharacterClassCategoryUse);
                                }

                                if (ccToken.Groups["goodUEscape"].Value == "P" && characterClassNegative)
                                {
                                    // would be problematic for us to allow this if we wanted to implement MatchOptions.LocaleAware in the future
                                    return CCRegExError(TexlStrings.ErrInvalidRegExBadEscapeInsideNegativeCharacterClass);
                                }

                                if (!UnicodeCategories.Contains(ccToken.Groups["UCategory"].Value))
                                {
                                    return CCRegExError(TexlStrings.ErrInvalidRegExBadUnicodeCategory);
                                }

                                hyphenStart = -1;
                                hyphenWait--;
                            }
                            else if (ccToken.Groups["goodHyphen"].Success)
                            {
                                if (hyphenStart == -1)
                                {
                                    return CCRegExError(TexlStrings.ErrInvalidRegExCharacterClassCategoryUse);
                                }

                                if (hyphenWait > 0)
                                {
                                    return CCRegExError(TexlStrings.ErrInvalidRegExLiteralHyphenInCharacterClass);
                                }

                                // we need to see two characters after a hyphen, to end and start another range, before we can entertain another hyphen
                                hyphenWait = 2;
                            }
                            else if (ccToken.Groups["badEscape"].Success)
                            {
                                return CCRegExError(TexlStrings.ErrInvalidRegExBadEscape);
                            }
                            else if (ccToken.Groups["goodUSurrPair"].Success || ccToken.Groups["elseUSurrPair"].Success || ccToken.Groups["goodULarge"].Success)
                            {
                                return CCRegExError(TexlStrings.ErrInvalidRegExSurrogatePairInCharacterClass);
                            }
                            else if (ccToken.Groups["badUSurr"].Success || ccToken.Groups["elseBadUSurr"].Success)
                            {
                                return CCRegExError(TexlStrings.ErrInvalidRegExMalformedSurrogatePair);
                            }
                            else if (ccToken.Groups["goodAnchorOutsideCC"].Success || ccToken.Groups["backRefName"].Success || ccToken.Groups["backRefNumber"].Success)
                            {
                                return CCRegExError(TexlStrings.ErrInvalidRegExBadEscapeInsideCharacterClass);
                            }
                            else if (ccToken.Groups["badOctal"].Success)
                            {
                                return CCRegExError(TexlStrings.ErrInvalidRegExBadOctal);
                            }
                            else if (ccToken.Groups["badInCharClass"].Success)
                            {
                                return CCRegExError(TexlStrings.ErrInvalidRegExUnescapedCharInCharacterClass);
                            }
                            else if (ccToken.Groups["badDoubleInCharClass"].Success)
                            {
                                return CCRegExError(TexlStrings.ErrInvalidRegExRepeatInCharClass);
                            }
                            else if (ccToken.Groups["badHyphen"].Success)
                            {
                                return CCRegExError(TexlStrings.ErrInvalidRegExLiteralHyphenInCharacterClass);
                            }
                            else
                            {
                                // This should never be hit. It is here in case one of the names checked doesn't match the RE, in which case running tests would hit this.
                                throw new NotImplementedException("Unknown character class regular expression match: CC = " + token.Value + ", ccToken = " + ccToken.Value);
                            }
                        }

                        groupTracker.SeenNonGroup();
                    }
                    else if (token.Groups["goodNamedCapture"].Success)
                    {
                        var namedCapture = token.Groups["goodNamedCapture"].Value;

                        if (numberedCpature)
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExMixingNamedAndNumberedSubMatches);
                        }

                        if (namedCapture.Length > MaxNamedCaptureNameLength)
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExNamedCaptureNameTooLong);
                        }

                        if (!groupTracker.SeenOpen(out var error, name: namedCapture))
                        {
                            return RegExError((ErrorResourceKey)error, startContext: true);
                        }
                    }
                    else if (token.Groups["goodNonCapture"].Success)
                    {
                        if (!groupTracker.SeenOpen(out var error))
                        {
                            return RegExError((ErrorResourceKey)error, startContext: true);
                        }
                    }
                    else if (token.Groups["goodLookBehind"].Success)
                    {
                        if (!groupTracker.SeenOpen(out var error, isLookBehind: true))
                        {
                            return RegExError((ErrorResourceKey)error, startContext: true);
                        }
                    }
                    else if (token.Groups["goodLookAhead"].Success)
                    {
                        if (!groupTracker.SeenOpen(out var error, isLookAhead: true))
                        {
                            return RegExError((ErrorResourceKey)error, startContext: true);
                        }
                    }
                    else if (token.Groups["alternation"].Success)
                    {
                        groupTracker.SeenAlternation();
                    }
                    else if (token.Groups["openParen"].Success)
                    {
                        if (numberedCpature)
                        {
                            captureNumber++;
                            if (!groupTracker.SeenOpen(out var error, name: captureNumber.ToString(CultureInfo.InvariantCulture)))
                            {
                                return RegExError((ErrorResourceKey)error, startContext: true);
                            }
                        }
                        else
                        {
                            if (!groupTracker.SeenOpen(out var error))
                            {
                                return RegExError((ErrorResourceKey)error, startContext: true);
                            }
                        }
                    }
                    else if (token.Groups["closeParen"].Success)
                    {
                        if (!groupTracker.SeenClose(out var error))
                        {
                            return RegExError((ErrorResourceKey)error, endContext: true);
                        }
                    }
                    else if (token.Groups["backRefName"].Success || token.Groups["backRefNumber"].Success)
                    {
                        string backRefName;

                        if (token.Groups["backRefName"].Success)
                        {
                            backRefName = token.Groups["backRefName"].Value;

                            if (numberedCpature)
                            {
                                return RegExError(TexlStrings.ErrInvalidRegExMixingNamedAndNumberedSubMatches);
                            }
                        }
                        else
                        {
                            backRefName = token.Groups["backRefNumber"].Value;

                            if (!numberedCpature)
                            {
                                return RegExError(TexlStrings.ErrInvalidRegExNumberedSubMatchesDisabled);
                            }
                        }

                        if (!groupTracker.SeenBackRef(backRefName, out var error))
                        {
                            return RegExError((ErrorResourceKey)error);
                        }
                    }
                    else if (token.Groups["goodUEscape"].Success)
                    {
                        if (!UnicodeCategories.Contains(token.Groups["UCategory"].Value))
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExBadUnicodeCategory);
                        }

                        groupTracker.SeenNonGroup();
                    }
                    else if (token.Groups["goodInlineOptions"].Success)
                    {
                        var inlineOptions = token.Groups["goodInlineOptions"].Value;

                        if (Regex.IsMatch(inlineOptions, @"(?<char>.).*\k<char>"))
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExRepeatedInlineOption);
                        }

                        if (inlineOptions.Contains(MatchOptionChar.ExplicitCapture) && numberedCpature)
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExInlineOptionConflictsWithNumberedSubMatches);
                        }

                        if (inlineOptions.Contains(MatchOptionChar.FreeSpacing))
                        {
                            freeSpacing = true;
                        }
                    }
                    else if (token.Groups["goodInlineComment"].Success)
                    {
                        openInlineComment = true;
                        openInlineCommentStart = token.Index;
                    }
                    else if (token.Groups["poundComment"].Success)
                    {
                        if (freeSpacing)
                        {
                            openPoundComment = true;
                        }
                        else
                        {
                            groupTracker.SeenNonGroup();
                        }
                    }
                    else if (token.Groups["badNamedCaptureName"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadNamedCaptureName);
                    }
                    else if (token.Groups["badOctal"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadOctal);
                    }
                    else if (token.Groups["badBalancing"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadBalancing);
                    }
                    else if (token.Groups["badInlineOptions"].Success)
                    {
                        return RegExError(token.Groups["badInlineOptions"].Index > 0 ? TexlStrings.ErrInvalidRegExInlineOptionNotAtStart : TexlStrings.ErrInvalidRegExBadInlineOptions);
                    }
                    else if (token.Groups["badSingleQuoteNamedCapture"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadSingleQuoteNamedCapture);
                    }
                    else if (token.Groups["badConditional"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadConditional);
                    }
                    else if (token.Groups["badEscape"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadEscape);
                    }
                    else if (token.Groups["goodEscapeInsideCCOnly"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadEscapeOutsideCharacterClass);
                    }
                    else if (token.Groups["badQuantifiers"].Success || token.Groups["badLimited"].Success || token.Groups["badUnlimited"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadQuantifier);
                    }
                    else if (token.Groups["badExact"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadExactQuantifier);
                    }
                    else if (token.Groups["badCurly"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadCurly);
                    }
                    else if (token.Groups["badParen"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadParen);
                    }
                    else if (token.Groups["badSquareBrackets"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExBadSquare, endContext: true);
                    }
                    else if (token.Groups["badEmptyCharacterClass"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExEmptyCharacterClass);
                    }
                    else if (token.Groups["badUSurr"].Success || token.Groups["elseeUSurr"].Success)
                    {
                        return RegExError(TexlStrings.ErrInvalidRegExMalformedSurrogatePair);
                    }
                    else
                    {
                        // This should never be hit. It is here in case one of the Groups names checked doesn't match the RE, in which case running tests would hit this.
                        throw new NotImplementedException("Unknown general regular expression match: " + token.Value);
                    }
                }
            }

            if (!groupTracker.Complete(out var completeError, out var completeErrorArg))
            {
                return new RegexTypeCacheEntry() 
                { 
                    Error = completeError, 
                    ErrorParam = completeErrorArg, 
                    ErrorSeverity = DocumentErrorSeverity.Severe 
                };
            }

            if (openInlineComment)
            {
                return new RegexTypeCacheEntry()
                {
                    Error = TexlStrings.ErrInvalidRegExUnclosedInlineCommentWithContext,
                    ErrorParam = regexPattern.Length - openInlineCommentStart > ErrorContextLength ? regexPattern.Substring(openInlineCommentStart, ErrorContextLength) + "..." : regexPattern.Substring(openInlineCommentStart),
                    ErrorSeverity = DocumentErrorSeverity.Severe
                };
            }

            // may be modifed by inline options; we only care about x and N in the next stage
            alteredOptions = (freeSpacing ? "x" : string.Empty) + (numberedCpature ? "N" : string.Empty);

            return null;
        }

        private static readonly IReadOnlyCollection<string> UnicodeCategories = new HashSet<string>()
        {
            "L", "Lu", "Ll", "Lt", "Lm", "Lo",
            "M", "Mn", "Mc", "Me",
            "N", "Nd", "Nl", "No",
            "P", "Pc", "Pd", "Ps", "Pe", "Pi", "Pf", "Po",
            "S", "Sm", "Sc", "Sk", "So",
            "Z", "Zs", "Zl", "Zp",
            "Cc", "Cf", 

            // "C", "Cs", "Co", "Cn", are left out for now until we have a good scenario, as they differ between implementations
        };

        // Tracks the groups in the regular expression.  Groups includes named and numbered capture groups, non-capture groups, and look arounds.
        // Groups that have been closed, even though they aren't still on the stack, may still have references through GroupInfo.Parent, captures, and backRefs lists.
        // The depth is limited so that the tree walk in GroupInfo.IsPossibleZeroCapture() is limited.
        //
        // For each group, there are two items put on the stack: one for the group, and one for the first (and possibly only) alternation in that group.
        // The stack will always be a multiple of 2, and hence the max size MaxGroupStackDepth is defined in terms of a multiple of 2.
        // If an alternation is seen, the first is closed and a second opened, etc. This allows us to see if a capture group is in the same alternation as a back reference.
        //
        // Graphically, it looks something like this (with lots of whitspace, think (?x):
        //
        //               a  (  b  |   (  c  )  |  d  )    e   |  f 
        //  Stack 5:                    ---                             if there are any alternation in that group (there are not)
        //  Stack 4:                  -------                           container for the group starting at "( c ..."
        //  Stack 3:          ---    ---------   ---                    if there are any alternations in that group (there are)
        //  Stack 2:        --------------------------                  container for the group starting at "( b ..."
        //  Stack 1:    -------------------------------------   ---     if there are any alternations at the top level (there are)
        //  Stack 0:   ---------------------------------------------    base for the entire RE
        private class GroupTracker
        {
            private readonly Stack<GroupInfo> _groupStack = new Stack<GroupInfo>();         // current stack of open groups
            private GroupInfo _quantTarget;                                                 // pointer to the literal or group seen, to apply next quantifier to
            private GroupInfo _inLookBehind;                                                // are we in a look behind?  they have special rules
            private readonly Dictionary<string, GroupInfo> _captureNames = new Dictionary<string, GroupInfo>();       // named and numbered group lookup
            private readonly List<string> _backRefs = new List<string>();                   // backrefs seen, processed in Complete to ensure they are non empty

            public GroupTracker()
            {
                // We add a base level for stuff at the root of the regular expression, for example "a|b|c".
                // This level is analyzed for backreferences, where "(a)|\1" should be an error
                SeenOpen(out _);
            }

            public bool SeenOpen(out ErrorResourceKey? error, string name = null, bool isLookBehind = false, bool isLookAhead = false)
            {
                if (_groupStack.Count >= MaxGroupStackDepth)
                {
                    error = TexlStrings.ErrInvalidRegExGroupTrackerOverflow;
                    return false;
                }

                if (_inLookBehind != null && isLookBehind)
                {
                    error = TexlStrings.ErrInvalidRegExNestedLookAround;
                    return false;
                }

                var groupInfo = new GroupInfo { IsNonCapture = name == null, IsGroup = true, IsLookAround = isLookBehind || isLookAhead };

                if (name != null)
                {
                    if (_inLookBehind != null)
                    {
                        error = TexlStrings.ErrInvalidRegExCaptureInLookAround;
                        return false;
                    }

                    if (_captureNames.ContainsKey(name))
                    {
                        error = TexlStrings.ErrInvalidRegExBadNamedCaptureAlreadyExists;
                        return false;
                    }

                    _captureNames.Add(name, groupInfo);
                }

                if (isLookBehind)
                {
                    _inLookBehind = groupInfo;
                }

                if (_groupStack.Count != 0)
                {
                    groupInfo.Parent = _groupStack.Peek();
                }

                _groupStack.Push(groupInfo);

                var child = new GroupInfo
                {
                    Parent = groupInfo
                };
                _groupStack.Push(child);

                _quantTarget = null;

                error = null;
                return true;
            }

            public void SeenAlternation()
            {
                _quantTarget = null;

                // close existing subgroup
                var closed = _groupStack.Pop();
                closed.CloseSubGroup();

                // open new subgroup, same parent
                var group = new GroupInfo
                {
                    Parent = closed.Parent
                };
                _groupStack.Push(group);

                // set this only after closing
                closed.Parent.HasAlternation = true;
            }

            public void SeenNonGroup(int min = 1, int max = 1)
            {
                _quantTarget = _groupStack.Peek().SeenNonGroup(min, max);
            }

            public bool SeenQuantifier(int min, int max, out ErrorResourceKey? error)
            {
                // look behinds cannot contain an unlimited qunatifier
                if (_inLookBehind != null && max == -1)
                {
                    error = TexlStrings.ErrInvalidRegExUnlimitedQuantifierInLookAround;
                    return false;
                }

                if (_quantTarget == null)
                {
                    error = TexlStrings.ErrInvalidRegExQuantifierOnNothing;
                    return false;
                }

                var result = _groupStack.Peek().SeenQuantifier(_quantTarget, min, max, out error);
                _quantTarget = null;
                return result;
            }

            public bool SeenClose(out ErrorResourceKey? error)
            {
                if (_groupStack.Count <= 2)
                {
                    error = TexlStrings.ErrInvalidRegExUnopenedCaptureGroups;
                    return false;
                }

                var subGroup = _groupStack.Pop();
                subGroup.CloseSubGroup();
                _quantTarget = _groupStack.Pop();
                
                // Look behinds are limited in number of characters, PCRE2 doesn't support more than 255.
                // This is why we track the MaxSize on all groups and literal characters.
                // Can't just check the look around group, could be on a containing group, for example "((?=a))*".
                if (_inLookBehind == _quantTarget)
                {
                    if (_quantTarget.MaxSize > MaxLookBehindPossibleCharacters)
                    {
                        error = TexlStrings.ErrInvalidRegExLookbehindTooManyChars;
                        return false;
                    }

                    _inLookBehind = null;
                }

                // Look behinds can never have quantification, JavaScript doesn't support this.
                if (_quantTarget.IsLookAround)
                {
                    _quantTarget.ErrorOnQuant_LookBehind = true;
                }

                return _quantTarget.SeenClose(out error);
            }

            public bool SeenBackRef(string name, out ErrorResourceKey? error)
            {
                if (!_captureNames.ContainsKey(name))
                {
                    error = TexlStrings.ErrInvalidRegExBadBackRefNotDefined;
                    return false;
                }

                // group is not closed and thus self referencing
                if (!_captureNames[name].IsClosed)
                {
                    error = TexlStrings.ErrInvalidRegExBadBackRefSelfReferencing;
                    return false;
                }

                if (_inLookBehind != null && _captureNames[name].MaxSize == -1)
                {
                    error = TexlStrings.ErrInvalidRegExUnlimitedQuantifierInLookAround;
                    return false;
                }

                // Backref maxsize is based on the size inside the capture, not any quantifiers on the capture
                SeenNonGroup(_captureNames[name].MinSize, _captureNames[name].MaxSize);

                // If a backref is in the same group as its capture, even if the group is possibly empty, it is OK.
                // If that isn't the case, no need to add the backRef for later checking (but it may be added later from another location)
                if (_captureNames[name].IsBlockedBackRef(out _, _groupStack.Peek()) && !_backRefs.Contains(name))
                {
                    _backRefs.Add(name);
                }

                error = null;
                return true;
            }

            public bool Complete(out ErrorResourceKey? error, out string errorArg)
            {
                var subGroup = _groupStack.Pop();
                subGroup.CloseSubGroup();
                var topGroup = _groupStack.Pop();
                topGroup.SeenEnd();

                if (_groupStack.Count != 0)
                {
                    error = TexlStrings.ErrInvalidRegExUnclosedCaptureGroups;
                    errorArg = null;
                    return false;
                }

                // for caompatibility reasons, back refs can't reference something that is posibly empty
                foreach (var backRef in _backRefs)
                {
                    if (_captureNames[backRef].IsBlockedBackRef(out error))
                    {
                        errorArg = char.IsDigit(backRef[0]) ? "\\" + backRef : "\\k<" + backRef + ">";
                        return false;
                    }
                }

                error = null;
                errorArg = null;
                return true;
            }

            // Information tracked for each group
            private class GroupInfo
            {
                public bool IsNonCapture;               // this group is a non-capture group
                public bool IsLookAround;               // is this gruop a look around? if so, limitations on quantifiers
                public bool IsGroup;                    // is this an actual group, vs a group created for literal characters, escapes, character classes, etc.
                public bool IsClosed;                   // this group is closed (we've seen the ending paren), ok to use for backref
                public bool ErrorOnQuant;               // we have a possibly empty capture group which has already seen a quantifier, next quantifier gets an error
                public bool ErrorOnQuant_LookBehind;    // used for more specific error message for look behinds
                public bool HasZeroQuant;               // this group has a quantifier
                public bool HasAlternation;             // this group has an alternation within
                public bool ContainsCapture;            // this gruop contains a cpature, perhaps nested down
                public int MaxSize;                     // maximum size of this group, -1 for unlimited
                public int MinSize;                     // minimum size of this group, 0 being the minimum
                public GroupInfo Parent;                // reference to the next level down

                // for characters that aren't in a group, such as literal characters, anchors, escapes, character classes, etc.
                public GroupInfo SeenNonGroup(int minSize = 1, int maxSize = 1)
                {
                    MinSize += minSize;
                    MaxSize += maxSize;

                    // this is a disconnected GroupInfo just for this literal, something for the next quantifier (if there is one) to work on
                    return new GroupInfo()
                    {
                        MinSize = minSize,
                        MaxSize = maxSize
                    };
                }

                public bool SeenQuantifier(GroupInfo lastGroup, int low, int high, out ErrorResourceKey? error)
                {
                    // high can't be less than low, unless high is unlimited
                    if (high < low && high != -1)
                    {
                        error = TexlStrings.ErrInvalidRegExLowHighQuantifierFlip;
                        return false;
                    }

                    if (lastGroup.IsGroup)
                    {
                        if (lastGroup.ErrorOnQuant_LookBehind)
                        {
                            error = TexlStrings.ErrInvalidRegExQuantifierOursideLookAround;
                            return false;
                        }

                        if (lastGroup.ErrorOnQuant)
                        {
                            error = TexlStrings.ErrInvalidRegExQuantifiedCapture;
                            return false;
                        }

                        lastGroup.HasZeroQuant = low == 0;

                        if (low <= 1 && lastGroup.ErrorOnQuant)
                        {
                            error = TexlStrings.ErrInvalidRegExQuantifiedCapture;
                            return false;
                        }

                        if ((low == 0 || (low > 0 && (lastGroup.MinSize == 0 || lastGroup.ContainsCapture))) && (!lastGroup.IsNonCapture || lastGroup.ContainsCapture))
                        {
                            ErrorOnQuant = true;
                        }
                    }

                    MaxSize = MaxSize == -1 || high == -1 ? -1 : MaxSize + (lastGroup.MaxSize * (high - 1));
                    MinSize += lastGroup.MinSize * (low - 1);

                    error = null;
                    return true;
                }

                public void CloseSubGroup()
                {
                    if (Parent.HasAlternation)
                    {
                        Parent.MaxSize = Math.Max(Parent.MaxSize, MaxSize);
                        Parent.MinSize = Math.Min(Parent.MinSize, MinSize);
                    }
                    else
                    {
                        Parent.MaxSize = MaxSize;
                        Parent.MinSize = MinSize;
                    }

                    SeenEnd();

                    Parent.ErrorOnQuant |= ErrorOnQuant;
                    Parent.ErrorOnQuant_LookBehind |= ErrorOnQuant_LookBehind;
                    Parent.ContainsCapture |= ContainsCapture;
                }

                // broken out from SeenClose for the end of the regular expression which may not have a closing paren
                public void SeenEnd()
                {
                    // If we are possibly empty, we can't suport a zero quantifier.
                    if (MinSize == 0)
                    {
                        ErrorOnQuant = true;
                    }

                    if (HasAlternation)
                    {
                        // an alternation with a capture can effectively be empty, for example "(a|(b)|c)".
                        // The capture group "b" in this case is not empty, but it is a capture in an alternation.
                        ErrorOnQuant = ErrorOnQuant || ContainsCapture;
                    }
                }

                public bool SeenClose(out ErrorResourceKey? error)
                {
                    SeenEnd();

                    IsClosed = true;

                    Parent.ErrorOnQuant |= ErrorOnQuant;
                    Parent.ErrorOnQuant_LookBehind |= ErrorOnQuant_LookBehind;

                    if (!IsNonCapture)
                    {
                        Parent.ContainsCapture = true;
                    }

                    // Look arounds have no size with respect to their parent
                    if (!IsLookAround)
                    {
                        Parent.MaxSize = Parent.MaxSize == -1 || MaxSize == -1 ? -1 : Parent.MaxSize + MaxSize;
                        Parent.MinSize += MinSize;
                    }

                    error = null;
                    return true;
                }

                // used to determine if a backref is acceptable
                // descent down the tree will stop if a stop GroupInfo is provided, used for detecting backrefs that are in the same group as their definition
                public bool IsBlockedBackRef(out ErrorResourceKey? error, GroupInfo stop = null)
                {
                    GroupInfo ptr;
                    error = null;

                    int maxDepth = MaxGroupStackDepth;

                    // ok to have alternation in the capture group itself, unless it is empty that PossibleEmpty would detect
                    if (HasZeroQuant || MinSize == 0)
                    {
                        error = TexlStrings.ErrInvalidRegExBackRefToZeroCapture;
                        return true;
                    }

                    // walk nested groups up to the base
                    // if we weren't already erroring on lookarounds,
                    // we would want to stop at a a lookaround, as it will appear to be zero below and quantifiers are not allowed on lookarounds and below
                    for (ptr = this.Parent; ptr != null && ptr != stop && maxDepth-- >= 0; ptr = ptr.Parent)
                    {
                        if (ptr.HasZeroQuant || ptr.HasAlternation || ptr.MinSize == 0)
                        {
                            error = TexlStrings.ErrInvalidRegExBackRefToZeroCapture;
                            return true;
                        }

                        if (ptr.IsLookAround)
                        {
                            error = TexlStrings.ErrInvalidRegExBackRefToCaptureInLookaround;
                            return true;
                        }
                    }

                    // failsafe, should never happen
                    if (maxDepth < 0)
                    {
                        throw new IndexOutOfRangeException("regular expression maximum GroupTracker depth exceeded");
                    }

                    // if we are checking for a specific group, and we didn't hit that gruop, the backref is not OK.
                    // no error message required, we'll report it later on the final scan.
                    if (stop != null && stop != ptr)
                    {
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name²
