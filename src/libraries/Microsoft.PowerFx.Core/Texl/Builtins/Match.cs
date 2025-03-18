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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
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
                errors.EnsureError(typeCacheEntry.ErrorSeverity, regExNode, (ErrorResourceKey)typeCacheEntry.Error, typeCacheEntry.ErrorParam);
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
                // For our purposes here to determine the type, we can just replace the other newline characters wtih \n.
                var regexPatternWhitespace = new Regex("[" + MatchWhiteSpace.NewLineEscapes + "]");
                regexPattern = regexPatternWhitespace.Replace(regexPattern, "\n");
            }

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
        private static RegexTypeCacheEntry GetRegexErrorEntry(string pattern, Exception exception)
        {
            string errorParam = string.Empty;
            ErrorResourceKey error = TexlStrings.ErrInvalidRegEx;

            var errorCode = exception.GetType().GetProperty("Error")?.GetValue(exception);
            var errorOffset = exception.GetType().GetProperty("Offset")?.GetValue(exception);

            if (errorCode != null && errorOffset != null)
            {
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
                        // uncommon and not blocked by V1 Power Fx regular expression parser, use default message
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
                        error = TexlStrings.ErrInvalidRegExBadSquare;
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

                int intOffset = Math.Max(Math.Min((int)errorOffset, 0), pattern.Length);

                errorParam = intOffset > ErrorContextLength ? "..." + pattern.Substring(intOffset - ErrorContextLength, ErrorContextLength) : pattern.Substring(0, intOffset);
            }

            return new RegexTypeCacheEntry()
            {
                Error = error,
                ErrorSeverity = DocumentErrorSeverity.Severe,
                ErrorParam = errorParam,
            };
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
        public const int MaxGroupStackDepth = 32;                   // maximum number of nested grouping levels, avoids performance issues with a complex group tree.
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
                    (?<goodEscape>\\
                           ([dfnrstw]                              |     # standard regex character classes, missing from .NET are aAeGzZv (no XRegExp support), other common are u{} and o
                            [\^\$\\\.\*\+\?\(\)\[\]\{\}\|\/]       |     # acceptable escaped characters with Unicode aware ECMAScript
                            [\#\ ]                                 |     # added for free spacing, always accepted for conssitency even in character classes, escape needs to be removed on Unicode aware ECMAScript
                            x[0-9a-fA-F]{2}                        |     # hex character, must be exactly 2 hex digits
                            u[0-9a-fA-F]{4}))                          | # Unicode characters, must be exactly 4 hex digits
                    \\(?<goodUEscape>[pP])\{(?<UCategory>[\w=:-]+)\}   | # Unicode character classes, extra characters here for a better error message
                    (?<goodAnchorOutsideCC>\\[bB])                     | # acceptable outside a character class, includes negative classes until we have character class subtraction, include \P for future MatchOptions.LocaleAware
                    (?<goodEscapeOutsideAndInsideCCIfPositive>\\[DWS]) |
                    (?<goodEscapeInsideCCOnly>\\[&\-!#%,;:<=>@`~\^])   | # https://262.ecma-international.org/#prod-ClassSetReservedPunctuator, others covered with goodEscape above
                    (?<badEscape>\\.)                                  | # all other escaped characters are invalid and reserved for future use
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
                    (?<badExact>{\d+}[\+\*\?])                         | # exact quantifier can't be used with a modifier
                    {(?<goodExact>\d+)}                                | # standard exact quantifier, no optional lazy
                    (?<badLimited>{\d+,\d+}[\+|\*])                    | # possessive and useless quantifiers
                    {(?<goodLimitedL>\d+),(?<goodLimitedH>\d+)}\??     | # standard limited quantifiers, with optional lazy
                    (?<badUnlimited>{\d+,}[\+|\*])                     |
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
                    (?<else>.)
                ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

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
                    (?<else>.)
                ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            int captureNumber = 0;                                  // last numbered capture encountered
            var groupTracker = new GroupTracker();

            bool openPoundComment = false;                          // there is an open end-of-line pound comment, only in freeFormMode
            bool openInlineComment = false;                         // there is an open inline comment

            foreach (Match token in generalRE.Matches(regexPattern))
            {
                RegexTypeCacheEntry RegExError(ErrorResourceKey? error, Match errToken = null, bool startContext = false, bool endContext = false, string postContext = null)
                {
                    if (errToken == null)
                    {
                        errToken = token;
                    }

                    string found = errToken.Value;

                    if (endContext)
                    {
                        var tokenEnd = errToken.Index + errToken.Length;
                        found = tokenEnd > ErrorContextLength ? "..." + regexPattern.Substring(tokenEnd - ErrorContextLength, ErrorContextLength) : regexPattern.Substring(0, tokenEnd);
                    }
                    else if (startContext)
                    {
                        found = errToken.Index + ErrorContextLength >= regexPattern.Length ? regexPattern.Substring(errToken.Index) : regexPattern.Substring(errToken.Index, ErrorContextLength) + "...";
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
                    else if (token.Groups["else"].Success || token.Groups["newline"].Success || token.Groups["goodEscape"].Success || token.Groups["goodEscapeOutsideAndInsideCCIfPositive"].Success)
                    {
                        if (token.Value == "\\" && token.Index == regexPattern.Length)
                        {
                            return RegExError(TexlStrings.ErrInvalidRegExEndsWithBackslash);
                        }

                        groupTracker.SeenNonGroup();
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
                                return RegExError(errKey, errToken: ccToken);
                            }

                            if (ccToken.Groups["goodEscape"].Success || ccToken.Groups["goodEscapeInsideCCOnly"].Success || ccToken.Groups["else"].Success)
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
                                            int.TryParse(ccToken.Value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out charVal);
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
                                // intentionally RegExError to get the whole character class as this is on the ends
                                return RegExError(TexlStrings.ErrInvalidRegExLiteralHyphenInCharacterClass);
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
                    Error = TexlStrings.ErrInvalidRegExUnclosedInlineComment, 
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
        private class GroupTracker
        {
            private readonly Stack<GroupInfo> _groupStack = new Stack<GroupInfo>();         // current stack of open groups
            private GroupInfo _quantTarget;                                                 // pointer to the literal or group seen, to apply quantifiers to
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

                _quantTarget = null;

                error = null;
                return true;
            }

            public void SeenAlternation()
            {
                _quantTarget = null;
                _groupStack.Peek().SeenAlternation();
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
                if (_groupStack.Count <= 1)
                {
                    error = TexlStrings.ErrInvalidRegExUnopenedCaptureGroups;
                    return false;
                }

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

                _backRefs.Add(name);

                error = null;
                return true;
            }

            public bool Complete(out ErrorResourceKey? error, out string errorArg)
            {
                _groupStack.Peek().SeenEnd();

                if (_groupStack.Count != 1)
                {
                    error = TexlStrings.ErrInvalidRegExUnclosedCaptureGroups;
                    errorArg = null;
                    return false;
                }

                // for caompatibility reasons, back refs can't reference something that is posibly empty
                foreach (var backRef in _backRefs)
                {
                    if (_captureNames[backRef].IsPossibleZeroLength())
                    {
                        error = TexlStrings.ErrInvalidRegExBackRefToZeroCapture;
                        errorArg = char.IsDigit(backRef[0]) ? "\\" + backRef : "\\k<" + backRef + ">";
                        return false;
                    }
                }

                error = null;
                errorArg = null;
                return true;
            }

            // Information tracked for each group and its parent
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
                public bool KnownNonZero;               // we've checked previously; this group is known not to be zero sized
                public int MaxSize;                     // maximum size of this group, -1 for unlimited
                public int MinSize;                     // minimum size of this group, 0 being the minimum
                public int MaxSizeAlternation;          // maximum size of the largest alternation option
                public int MinSizeAlternation;          // minimim size of the smallest alternation option
                public GroupInfo Parent;                // reference to the next level down

                // for characters that aren't in a group, such as literal characters, anchors, escapes, character classes, etc.
                public GroupInfo SeenNonGroup(int minSize = 1, int maxSize = 1)
                {
                    MinSize += minSize;
                    MaxSize += maxSize;

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

                public void SeenAlternation()
                {
                    // we need to examine each part of the alternation for a potential empty result
                    // PossibleEmpty is reset and PossibleEmptyAlternation is the accumulator for any empty parts
                    // HasAlternation is important because 
                    if (HasAlternation)
                    {
                        MinSizeAlternation = Math.Min(MinSizeAlternation, MinSize);
                        MaxSizeAlternation = MaxSizeAlternation == -1 || MaxSize == -1 ? -1 : Math.Max(MaxSizeAlternation, MaxSize);
                    }
                    else
                    {
                        MinSizeAlternation = MinSize;
                        MaxSizeAlternation = MaxSize;
                    }

                    HasAlternation = true;

                    MinSize = 0;
                    MaxSize = 0;
                }

                // broken out from SeenClose for the end of the regular expression which may not have a closing paren
                public void SeenEnd()
                {
                    if (HasAlternation)
                    {
                        SeenAlternation();                                // closes the last part of the alternation, the "c" in "(a|b|c)"
                        MinSize = MinSizeAlternation;
                        MaxSize = MaxSizeAlternation;

                        // an alternation with a capture can effectively be empty, for example "(a|(b)|c)".
                        // The capture group "b" in this case is not empty, but it is a capture in an alternation.
                        ErrorOnQuant = ErrorOnQuant || ContainsCapture;
                    }
                }

                public bool SeenClose(out ErrorResourceKey? error)
                {
                    SeenEnd();

                    IsClosed = true;

                    // If we are possibly empty, we can't suport a zero quantifier.
                    if (MinSize == 0)
                    {
                        ErrorOnQuant = true;
                    }

                    if (Parent != null)
                    {
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
                    }

                    error = null;
                    return true;
                }

                // used to determine if a backref is acceptable
                public bool IsPossibleZeroLength()
                {
                    // short circuit in case we are asked about the same capture
                    if (KnownNonZero)
                    {
                        return false;
                    }

                    int maxDepth = MaxGroupStackDepth;

                    // ok to have alternation in the capture group itself, unless it is empty that PossibleEmpty would detect
                    if (HasZeroQuant || MinSize == 0)
                    {
                        return true;
                    }

                    // walk nested groups up to the base
                    // stop at a a lookaround, as it will appear to be zero below and quantifiers are not allowed on lookarounds and below
                    for (var ptr = this.Parent; ptr != null && maxDepth-- >= 0 && !ptr.IsLookAround; ptr = ptr.Parent)
                    {
                        if (ptr.HasZeroQuant || ptr.HasAlternation || ptr.MinSize == 0)
                        {
                            return true;
                        }
                    }

                    // failsafe, should never happen
                    if (maxDepth < 0)
                    {
                        throw new IndexOutOfRangeException("regular expression maximum GroupTracker depth exceeded");
                    }

                    KnownNonZero = true;

                    return false;
                }
            }
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name²
