// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        public IsMatchFunction()
            : base("IsMatch", TexlStrings.AboutIsMatch, DType.Boolean, null)
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

    internal class BaseMatchFunction : BuiltinFunction
    {
        private readonly ConcurrentDictionary<string, Tuple<DType, bool, bool, bool>> _regexTypeCache;
        private readonly string _cachePrefix;
        private readonly int _regexCacheSize;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public override bool UseParentScopeForArgumentSuggestions => true;

        public BaseMatchFunction(string functionName, TexlStrings.StringGetter aboutGetter, DType returnType, RegexTypeCache regexTypeCache)
            : base(functionName, aboutGetter, FunctionCategories.Text, returnType, 0, 2, 3, DType.String, BuiltInEnums.MatchEnum.FormulaType._type, BuiltInEnums.MatchOptionsEnum.FormulaType._type)
        {
            if (regexTypeCache != null)
            {
                _cachePrefix = returnType.IsTable ? "tbl_" : "rec_";
                _regexTypeCache = regexTypeCache.Cache;
                _regexCacheSize = regexTypeCache.CacheSize;
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

            string regularExpressionOptions = string.Empty;
            var regExNode = args[1];

            if ((argTypes[1].Kind != DKind.String && argTypes[1].Kind != DKind.OptionSetValue) || !BinderUtils.TryGetConstantValue(context, regExNode, out var regularExpression))
            {
                errors.EnsureError(regExNode, TexlStrings.ErrVariableRegEx);
                return false;
            }

            if (context.Features.PowerFxV1CompatibilityRules && args.Length == 3 &&
                ((argTypes[2].Kind != DKind.String && argTypes[2].Kind != DKind.OptionSetValue) || !BinderUtils.TryGetConstantValue(context, args[2], out regularExpressionOptions)))
            {
                errors.EnsureError(args[2], TexlStrings.ErrVariableRegExOptions);
                return false;
            }

            if (!context.Features.PowerFxV1CompatibilityRules)
            {
                // only used for the following analysis and type creation, not modified in the IR
                regularExpressionOptions += "N";
            }

            string alteredOptions = regularExpressionOptions;

            return fValid &&
                    (!context.Features.PowerFxV1CompatibilityRules || IsSupportedRegularExpression(regExNode, regularExpression, regularExpressionOptions, out alteredOptions, errors)) &&
                    (returnType == DType.Boolean || TryCreateReturnType(regExNode, regularExpression, alteredOptions, errors, ref returnType));
        }

        // Creates a typed result: [Match:s, Captures:*[Value:s], NamedCaptures:r[<namedCaptures>:s]]
        private bool TryCreateReturnType(TexlNode regExNode, string regexPattern, string alteredOptions, IErrorContainer errors, ref DType returnType)
        {
            Contracts.AssertValue(regexPattern);
            string prefixedRegexPattern = this._cachePrefix + regexPattern;

            if (_regexTypeCache != null && _regexTypeCache.ContainsKey(prefixedRegexPattern))
            {
                var cachedType = _regexTypeCache[prefixedRegexPattern];
                if (cachedType != null)
                {
                    returnType = cachedType.Item1;
                    AddWarnings(regExNode, errors, cachedType.Item2, cachedType.Item3, cachedType.Item4);
                    return true;
                }
                else
                {
                    errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegEx);
                    return false;
                }
            }

            if (_regexTypeCache != null && _regexTypeCache.Count >= _regexCacheSize)
            {
                // To preserve memory during authoring, we clear the cache if it gets
                // too large. This should only happen in a minority of cases and
                // should have no impact on deployed apps.
                _regexTypeCache.Clear();
            }

            try
            {
                var regexDotNetOptions = RegexOptions.None;
                if (alteredOptions.Contains("x"))
                {
                    regexDotNetOptions |= RegexOptions.IgnorePatternWhitespace;

                    // In x mode, comment line endings are [\r\n], but .NET only supports \n.  For our purposes here, we can just replace the \r.
                    regexPattern = regexPattern.Replace('\r', '\n');
                }

                var regex = new Regex(regexPattern, regexDotNetOptions);

                List<TypedName> propertyNames = new List<TypedName>();
                bool fullMatchHidden = false, subMatchesHidden = false, startMatchHidden = false;

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
                    }
                    else if (captureName == ColumnName_SubMatches.Value)
                    {
                        subMatchesHidden = true;
                    }
                    else if (captureName == ColumnName_StartMatch.Value)
                    {
                        startMatchHidden = true;
                    }

                    propertyNames.Add(new TypedName(DType.String, DName.MakeValid(captureName, out _)));
                }

                if (!fullMatchHidden)
                {
                    propertyNames.Add(new TypedName(DType.String, ColumnName_FullMatch));
                }

                if (!subMatchesHidden && alteredOptions.Contains("N"))
                {
                    propertyNames.Add(new TypedName(DType.CreateTable(new TypedName(DType.String, ColumnName_Value)), ColumnName_SubMatches));
                }

                if (!startMatchHidden)
                {
                    propertyNames.Add(new TypedName(DType.Number, ColumnName_StartMatch));
                }

                returnType = returnType.IsRecord
                    ? DType.CreateRecord(propertyNames)
                    : DType.CreateTable(propertyNames);

                AddWarnings(regExNode, errors, hidesFullMatch: fullMatchHidden, hidesSubMatches: subMatchesHidden, hidesStartMatch: startMatchHidden);

                if (_regexTypeCache != null)
                {
                    _regexTypeCache[prefixedRegexPattern] = Tuple.Create(returnType, fullMatchHidden, subMatchesHidden, startMatchHidden);
                }

                return true;
            }
            catch (ArgumentException)
            {
                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegEx);
                if (_regexTypeCache != null)
                {
                    _regexTypeCache[prefixedRegexPattern] = null; // Cache to avoid evaluating again
                }

                return false;
            }
        }

        private void AddWarnings(TexlNode regExNode, IErrorContainer errors, bool hidesFullMatch, bool hidesSubMatches, bool hidesStartMatch)
        {
            if (hidesFullMatch)
            {
                errors.EnsureError(DocumentErrorSeverity.Suggestion, regExNode, TexlStrings.InfoRegExCaptureNameHidesPredefinedFullMatchField, ColumnName_FullMatch.Value);
            }

            if (hidesSubMatches)
            {
                errors.EnsureError(DocumentErrorSeverity.Suggestion, regExNode, TexlStrings.InfoRegExCaptureNameHidesPredefinedSubMatchesField, ColumnName_SubMatches.Value);
            }

            if (hidesStartMatch)
            {
                errors.EnsureError(DocumentErrorSeverity.Suggestion, regExNode, TexlStrings.InfoRegExCaptureNameHidesPredefinedStartMatchField, ColumnName_StartMatch.Value);
            }
        }

        // Power Fx regular expressions are limited to features that can be transpiled to native .NET (C# Interpreter), ECMAScript (Canvas), or PCRE2 (Excel).
        // We want the same results everywhere for Power Fx, even if the underlying implementation is different. Even with these limits in place there are some minor semantic differences but we get as close as we can.
        // These tests can be run through all three engines and the results compared with by setting ExpressionEvaluationTests.RegExCompareEnabled, a PCRE2 DLL and NodeJS must be installed on the system.
        //
        // In short, we use the insersection of canonical .NET regular expressions and ECMAScript 2024's "v" flag for escaping rules. 
        // Someday when "v" is more widely avaialble, we can support more of its features such as set subtraction.
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
        // Features that are supported:
        //     Literal characters. Any character except the special characters [ ] \ ^ $ . | ? * + ( ) can be inserted directly.
        //     Escaped special characters. \ (backslash) followed by a special character to insert it directly, includes \- when in a character class.
        //     Operators
        //         Dot (.), matches everything except [\r\n] unless MatchOptions.DotAll is used.
        //         Anchors, ^ and $, matches the beginning and end of the string, or of a line if MatchOptions.Multiline is used.
        //     Quanitfiers
        //         Greedy quantifiers. ? matches 0 or 1 times, + matches 1 or more times, * matches 0 or more times, {3} matches exactly 3 times, {1,} matches at least 1 time, {1,3} matches between 1 and 3 times. By default, matching is "greedy" and the match will be as large as possible.
        //         Lazy quantifiers. Same as the greedy quantifiers followed by ?, for example *? or {1,3}?. With the lazy modifier, the match will be as small as possible.
        //     Alternation. a|b matches "a" or "b".
        //     Character classes
        //         Custom character class. [abc] list of characters, [a-fA-f0-9] range of characters, [^a-z] everything but these characters. Character classes cannot be nested, subtracted, or intersected, and the same special character cannot be repeated in the character class.
        //         Word characters and breaks. \w, \W, \b, \B, using the Unicode definition of letters [\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}\p{Lm}].
        //         Digit characters. \d includes the digits 0-9 and \p{Nd}, \D matches everything except characters matched by \d.
        //         Space characters. \s includes spacing characters [ \r\n\t\f\x0B\x85\p{Z}], \S which matches everything except characters matched by \s, \r carriage return, \n newline, \t tab, \f form feed.
        //         Control characters. \cA, where the control character is [A-Za-z].
        //         Hexadecimal and Unicode character codes. \x20 with two hexadecimal digits, \u2028 with four hexadecimal digits.
        //         Unicode character class and property. \p{Ll} matches all Unicode lowercase letters, while \P{Ll} matches everything that is not a Unicode lowercase letter.
        //     Capture groups
        //         Non capture group. (?:a), group without capturing the result as a named or numbered sub-match.
        //         Named group and back reference. (?<name>chars) captures a sub-match with the name name, referenced with \k<name>. Cannot be used if MatchOptions.NumberedSubMatches is enabled.
        //         Numbered group and back referencs. (a|b) captures a sub-match, referenced with \1. MatchOptions.NumberedSubMatches must be enabled.
        //     Lookahead and lookbehind. (?=a), (?!a), (?<=b), (?<!b).
        //     Free spacing mode. Whitepsace within the regular expression is ignored and # starts an end of line comment.
        //     Inline comments. (?# comment here), which is ignored as a comment. See MatchOptions.FreeSpacing for an alternative to formatting and commenting regular expressions.
        //     Inline mode modifiers. (?im) is the same as using MatchOptions.IgnoreCase and MatchOptions.Multiline. Must be used at the beginning of the regular expression. Supported inline modes are [imsx], corresponding to MatchOptions.IgnoreCase, MatchOptions.Multiline, MatchOptions.DotAll, and MatchOptions.FreeSpacing, respectively.
        //
        // Significant features that are not supported:
        //     Capture groups
        //         Numbered capture groups are disable by default, use named captures or MatchOptions.NumberedSubMatches
        //         Self-referncing groups, such as "(a\1)"
        //         Single quoted named capture groups "(?'name'..." and "\k'name'"
        //         Balancing capture groups
        //         Recursion
        //     Character classes
        //         \W, \D, \P, \S are not supported inside character classes if the character class is negated (starts with [^...])
        //         Use of ^, -, [, or ] without an escape inside a character class is not supported
        //         Character class set operations, such as subraction or intersection
        //         Empty character classes
        //     Inline options
        //         Turning options on or off
        //         Changing options later in the expression
        //         Setting options for a subexpression
        //     Conditionals
        //     Octal characters
        //     \x{...} and \u{...} notation
        //     Subroutines
        //     Possessive quantifiers
        //
        // In addition, the Power Fx compiler uses the .NET regular expression engine to validate the expression and determine capture group names.
        // So, any regular expression that does not compile with .NET is also automatically disallowed.
        private bool IsSupportedRegularExpression(TexlNode regExNode, string regexPattern, string regexOptions, out string alteredOptions, IErrorContainer errors)
        {
            bool freeSpacing = regexOptions.Contains("x");          // can also be set with inline mode modifier
            bool numberedCpature = regexOptions.Contains("N");      // can only be set here, no inline mode modifier

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
                            c[a-zA-Z]                              |     # Ctrl character classes
                            x[0-9a-fA-F]{2}                        |     # hex character, must be exactly 2 hex digits
                            u[0-9a-fA-F]{4}))                          | # Unicode characters, must be exactly 4 hex digits
                    \\(?<goodUEscape>[pP])\{(?<UCategory>[\w=:-]+)\}   | # Unicode chaeracter classes, extra characters here for a better error message
                    (?<goodEscapeOutsideCC>\\[bB])                     | # acceptable outside a character class, includes negative classes until we have character class subtraction, include \P for future MatchOptions.LocaleAware
                    (?<goodEscapeOutsideAndInsideCCIfPositive>\\[DWS]) |
                    (?<goodEscapeInsideCCOnly>\\[&\-!#%,;:<=>@`~\^])   | # https://262.ecma-international.org/#prod-ClassSetReservedPunctuator, others covered with goodEscape above
                    (?<badEscape>\\.)                                  | # all other escaped characters are invalid and reserved for future use
                ";

            var generalRE = new Regex(
                escapeRE +
                @"
                    # leading (?<, named captures
                    \(\?<(?<goodNamedCapture>[a-zA-Z][a-zA-Z\d]*)>     | # named capture group, can only be letters and numbers and must start with a letter
                    (?<goodLookaround>\(\?(=|!|<=|<!))                 | # lookahead and lookbehind
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
                    (?<badQuantifiers>[\*\+][\+\*])                    | # possessive (ends with +) and useless quantifiers (ends with *)
                    (?<goodQuantifiers>[\*\+]\??)                      | # greedy and lazy quantifiers
                    (?<badZeroOrOne>[\?][\+\*])                        |
                    (?<goodZeroOrOne>[\?]\??)                          |

                    # leading {, exact and limited quantifiers
                    (?<badExact>{\d+}[\+\*\?])                         | # exact quantifier can't be used with a modifier
                    (?<goodExact>{\d+})                                | # standard exact quantifier, no optional lazy
                    (?<badLimited>{\d+,\d+}[\+|\*])                    | # possessive and useless quantifiers
                    {(?<goodLimitedL>\d+),(?<goodLimitedH>\d+)}\??     | # standard limited quantifiers, with optional lazy
                    (?<badUnlimited>{\d+,}[\+|\*])                     |
                    (?<goodUnlimited>{\d+,}\??)                        |
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
                    (?<poundComment>\#)                                | # used in free spacing mode (to detect start of comment), ignored otherwise
                    (?<newline>[\r\n])                                 | # used in free spacing mode (to detect end of comment), ignored otherwise
                    (?<else>.)
                ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            var characterClassRE = new Regex(
                escapeRE +
                @"
                    (?<badHyphen>^-|-$)                                | # begin/end literal hyphen not allowed within character class, needs to be escaped (ECMAScript v)
                    (?<badInCharClass>  \/ | \| | \\             |       # https://262.ecma-international.org/#prod-ClassSetSyntaxCharacter
                        \{ | \} | \( | \) | \[ | \] | \^)              | # adding ^ for Power Fx, making it clear that the carets in [^^] have different meanings
                    (?<badDoubleInCharClass> << | == | >> | ::   |       # reserved pairs, see https://262.ecma-international.org/#prod-ClassSetReservedDoublePunctuator 
                        @@ | `` | ~~ | %% | && | ;; | ,, | !!    |       # and https://www.unicode.org/reports/tr18/#Subtraction_and_Intersection
                        \|\| | \#\# | \$\$ | \*\* | \+\+ | \.\.  |       # includes set subtraction 
                        \?\? | \^\^ | \-\-)                                                                
                ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            int captureNumber = 0;                                  // last numbered capture encountered
            var groupStack = new GroupStack();                      // stack of all open groups, including captures and non-captures
            var captures = new Dictionary<string, GroupInfo>();     // capture map from name or number (as a string)
            List<string> backRefs = new List<string>();             // list of back references

            bool openPoundComment = false;                          // there is an open end-of-line pound comment, only in freeFormMode
            bool openInlineComment = false;                         // there is an open inline comment

            foreach (Match token in generalRE.Matches(regexPattern))
            {
                void RegExError(ErrorResourceKey errKey, Match errToken = null, bool startContext = false, bool endContext = false, string postContext = null)
                {
                    const int contextLength = 8;

                    if (errToken == null)
                    {
                        errToken = token;
                    }

                    if (endContext)
                    {
                        var tokenEnd = errToken.Index + errToken.Length;
                        var found = tokenEnd >= contextLength ? "..." + regexPattern.Substring(tokenEnd - contextLength, contextLength) : regexPattern.Substring(0, tokenEnd);
                        errors.EnsureError(regExNode, errKey, found + postContext);
                    }
                    else if (startContext)
                    {
                        var found = errToken.Index + contextLength >= regexPattern.Length ? regexPattern.Substring(errToken.Index) : regexPattern.Substring(errToken.Index, contextLength) + "...";
                        errors.EnsureError(regExNode, errKey, found + postContext);
                    }
                    else
                    {
                        errors.EnsureError(regExNode, errKey, errToken.Value + postContext);
                    }
                }

                if (token.Groups["newline"].Success)
                {
                    openPoundComment = false;
                }
                else if (openInlineComment && (token.Groups["closeParen"].Success || token.Groups["goodEscape"].Value == "\\)"))
                {
                    openInlineComment = false;
                }
                else if (!openPoundComment && !openInlineComment)
                {
                    if (token.Groups["anchors"].Success || token.Groups["goodZeroOrOne"].Success || token.Groups["goodExact"].Success)
                    {
                        // nothing to do
                    }
                    else if (token.Groups["goodLimitedL"].Success)
                    {
                        var low = Convert.ToInt32(token.Groups["goodLimitedL"].Value, CultureInfo.InvariantCulture);
                        var high = Convert.ToInt32(token.Groups["goodLimitedH"].Value, CultureInfo.InvariantCulture);

                        if (high < low)
                        {
                            // todo new error
                            RegExError(TexlStrings.ErrInvalidRegExQuantifierInLookAround, endContext: true);
                            return false;
                        }

                        if (groupStack.InLookAround() && (high - low > 64))
                        {
                            // todo new error
                            RegExError(TexlStrings.ErrInvalidRegExQuantifierInLookAround, endContext: true);
                            return false;
                        }
                    }
                    else if (token.Groups["goodQuantifiers"].Success || token.Groups["goodUnlimited"].Success)
                    {
                        if (groupStack.InLookAround())
                        {
                            RegExError(TexlStrings.ErrInvalidRegExQuantifierInLookAround, endContext: true);
                            return false;
                        }
                    }
                    else if (token.Groups["else"].Success || token.Groups["goodEscape"].Success || token.Groups["goodEscapeOutsideCC"].Success || token.Groups["goodEscapeOutsideAndInsideCCIfPositive"].Success)
                    {
                        if (!Regex.IsMatch(regexPattern.Substring(token.Index + token.Length), @"^(\*|\?|\{0+[,\}])"))
                        {
                            groupStack.Peek().SeenNonEmpty();
                        }
                    }
                    else if (token.Groups["characterClass"].Success)
                    {
                        bool characterClassNegative = token.Groups["characterClass"].Value[0] == '^';
                        string ccString = characterClassNegative ? token.Groups["characterClass"].Value.Substring(1) : token.Groups["characterClass"].Value;

                        foreach (Match ccToken in characterClassRE.Matches(ccString))
                        {
                            void CCRegExError(ErrorResourceKey errKey)
                            {
                                RegExError(errKey, errToken: ccToken);
                            }

                            if (ccToken.Groups["goodEscape"].Success || ccToken.Groups["goodEscapeInsideCCOnly"].Success)
                            {
                                // all good, nothing to do
                            }
                            else if (ccToken.Groups["goodEscapeOutsideAndInsideCCIfPositive"].Success)
                            {
                                if (characterClassNegative)
                                {
                                    CCRegExError(TexlStrings.ErrInvalidRegExBadEscapeInsideNegativeCharacterClass);
                                    return false;
                                }
                            }
                            else if (ccToken.Groups["goodUEscape"].Success)
                            {
                                if (ccToken.Groups["goodUEscape"].Value == "P" && characterClassNegative)
                                {
                                    // would be problematic for us to allow this if we wanted to implement MatchOptions.LocaleAware in the future
                                    CCRegExError(TexlStrings.ErrInvalidRegExBadEscapeInsideNegativeCharacterClass);
                                    return false;
                                }

                                if (!UnicodeCategories.Contains(ccToken.Groups["UCategory"].Value))
                                {
                                    CCRegExError(TexlStrings.ErrInvalidRegExBadUnicodeCategory);
                                    return false;
                                }
                            }
                            else if (ccToken.Groups["badEscape"].Success)
                            {
                                CCRegExError(TexlStrings.ErrInvalidRegExBadEscape);
                                return false;
                            }
                            else if (ccToken.Groups["goodEscapeOutsideCC"].Success || ccToken.Groups["backRefName"].Success || ccToken.Groups["backRefNumber"].Success)
                            {
                                CCRegExError(TexlStrings.ErrInvalidRegExBadEscapeInsideCharacterClass);
                                return false;
                            }
                            else if (ccToken.Groups["badOctal"].Success)
                            {
                                CCRegExError(TexlStrings.ErrInvalidRegExBadOctal);
                                return false;
                            }
                            else if (ccToken.Groups["badInCharClass"].Success)
                            {
                                CCRegExError(TexlStrings.ErrInvalidRegExUnescapedCharInCharacterClass);
                                return false;
                            }
                            else if (ccToken.Groups["badDoubleInCharClass"].Success)
                            {
                                CCRegExError(TexlStrings.ErrInvalidRegExRepeatInCharClass);
                                return false;
                            }
                            else if (ccToken.Groups["badHyphen"].Success)
                            {
                                // intentionally RegExError to get the whole character class as this is on the ends
                                RegExError(TexlStrings.ErrInvalidRegExLiteralHyphenInCharacterClass);
                                return false;
                            }
                            else
                            {
                                // This should never be hit. It is here in case one of the names checked doesn't match the RE, in which case running tests would hit this.
                                throw new NotImplementedException("Unknown character class regular expression match: CC = " + token.Value + ", ccToken = " + ccToken.Value);
                            }
                        }

                        if (!Regex.IsMatch(regexPattern.Substring(token.Index + token.Length), @"^(\*|\?|\{0+[,\}])"))
                        {
                            groupStack.Peek().SeenNonEmpty();
                        }
                    }
                    else if (token.Groups["goodNamedCapture"].Success)
                    {
                        var namedCapture = token.Groups["goodNamedCapture"].Value;

                        if (groupStack.InLookAround())
                        {
                            RegExError(TexlStrings.ErrInvalidRegExCaptureInLookAround);
                            return false;
                        }

                        if (numberedCpature)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExMixingNamedAndNumberedSubMatches);
                            return false;
                        }

                        if (captures.ContainsKey(namedCapture))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExBadNamedCaptureAlreadyExists);
                            return false;
                        }

                        var captureInfo = new GroupInfo();
                        if (!groupStack.Push(captureInfo))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExGroupStackOverflow, startContext: true);
                            return false;
                        }

                        captures.Add(namedCapture, captureInfo);
                    }
                    else if (token.Groups["goodNonCapture"].Success)
                    {
                        var captureInfo = new GroupInfo() { IsNonCapture = true };
                        if (!groupStack.Push(captureInfo))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExGroupStackOverflow, startContext: true);
                            return false;
                        }
                    }
                    else if (token.Groups["goodLookaround"].Success)
                    {
                        if (groupStack.InLookAround())
                        {
                            RegExError(TexlStrings.ErrInvalidRegExNestedLookAround, startContext: true);
                            return false;
                        }

                        var captureInfo = new GroupInfo() { IsLookAround = true };
                        if (!groupStack.Push(captureInfo))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExGroupStackOverflow, startContext: true);
                            return false;
                        }
                    }
                    else if (token.Groups["alternation"].Success)
                    {
                        groupStack.Peek().SeenAlternation();
                    }
                    else if (token.Groups["openParen"].Success)
                    {
                        if (numberedCpature)
                        {
                            if (groupStack.InLookAround())
                            {
                                RegExError(TexlStrings.ErrInvalidRegExCaptureInLookAround, startContext: true);
                                return false;
                            }

                            var captureInfo = new GroupInfo();
                            if (!groupStack.Push(captureInfo))
                            {
                                RegExError(TexlStrings.ErrInvalidRegExGroupStackOverflow, startContext: true);
                                return false;
                            }

                            captureNumber++;
                            captures.Add(captureNumber.ToString(CultureInfo.InvariantCulture), captureInfo);
                        }
                        else
                        {
                            var captureInfo = new GroupInfo() { IsNonCapture = true };
                            if (!groupStack.Push(captureInfo))
                            {
                                RegExError(TexlStrings.ErrInvalidRegExGroupStackOverflow, startContext: true);
                                return false;
                            }
                        }
                    }
                    else if (token.Groups["closeParen"].Success)
                    {
                        if (groupStack.IsEmpty())
                        {
                            RegExError(TexlStrings.ErrInvalidRegExUnopenedCaptureGroups);
                            return false;
                        }

                        var captureInfo = groupStack.Pop();
                        var lookAhead = regexPattern.Substring(token.Index + 1);

                        if (!captureInfo.SeenClose(lookAhead, out var error))
                        {
                            var match = Regex.Match(lookAhead, @"^(\*|\?|\+|\{[^\}]*\})");
                            RegExError((ErrorResourceKey)error, endContext: true, postContext: match.Value);
                            return false;
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
                                RegExError(TexlStrings.ErrInvalidRegExMixingNamedAndNumberedSubMatches);
                                return false;
                            }
                        }
                        else // token.Groups["backRefNumber"].Success
                        {
                            backRefName = token.Groups["backRefNumber"].Value;

                            if (!numberedCpature)
                            {
                                RegExError(TexlStrings.ErrInvalidRegExNumberedSubMatchesDisabled);
                                return false;
                            }
                        }

                        backRefs.Add(backRefName);

                        // group isn't defined, or not defined yet
                        if (!captures.ContainsKey(backRefName))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExBadBackRefNotDefined);
                            return false;
                        }

                        // group is not closed and thus self referencing
                        if (!captures[backRefName].IsClosed)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExBadBackRefSelfReferencing);
                            return false;
                        }
                    }
                    else if (token.Groups["goodUEscape"].Success)
                    {
                        if (!UnicodeCategories.Contains(token.Groups["UCategory"].Value))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExBadUnicodeCategory);
                            return false;
                        }
                    }
                    else if (token.Groups["goodInlineOptions"].Success)
                    {
                        var inlineOptions = token.Groups["goodInlineOptions"].Value;

                        if (Regex.IsMatch(inlineOptions, @"(?<char>.).*\k<char>"))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExRepeatedInlineOption);
                            return false;
                        }

                        if (inlineOptions.Contains("n") && numberedCpature)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExInlineOptionConflictsWithNumberedSubMatches);
                            return false;
                        }

                        if (inlineOptions.Contains("x"))
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
                        openPoundComment = freeSpacing;
                    }
                    else if (token.Groups["badNamedCaptureName"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadNamedCaptureName);
                        return false;
                    }
                    else if (token.Groups["badOctal"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadOctal);
                        return false;
                    }
                    else if (token.Groups["badBalancing"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadBalancing);
                        return false;
                    }
                    else if (token.Groups["badInlineOptions"].Success)
                    {
                        RegExError(token.Groups["badInlineOptions"].Index > 0 ? TexlStrings.ErrInvalidRegExInlineOptionNotAtStart : TexlStrings.ErrInvalidRegExBadInlineOptions);
                        return false;
                    }
                    else if (token.Groups["badSingleQuoteNamedCapture"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadSingleQuoteNamedCapture);
                        return false;
                    }
                    else if (token.Groups["badConditional"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadConditional);
                        return false;
                    }
                    else if (token.Groups["badEscape"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadEscape);
                        return false;
                    }
                    else if (token.Groups["goodEscapeInsideCCOnly"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadEscapeOutsideCharacterClass);
                        return false;
                    }
                    else if (token.Groups["badQuantifiers"].Success || token.Groups["badLimited"].Success || token.Groups["badZeroOrOne"].Success || token.Groups["badUnlimited"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadQuantifier);
                        return false;
                    }
                    else if (token.Groups["badExact"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadExactQuantifier);
                        return false;
                    }
                    else if (token.Groups["badCurly"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadCurly);
                        return false;
                    }
                    else if (token.Groups["badParen"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadParen);
                        return false;
                    }
                    else if (token.Groups["badSquareBrackets"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadSquare, endContext: true);
                        return false;
                    }
                    else if (token.Groups["badEmptyCharacterClass"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExEmptyCharacterClass);
                        return false;
                    }
                    else
                    {
                        // This should never be hit. It is here in case one of the Groups names checked doesn't match the RE, in which case running tests would hit this.
                        throw new NotImplementedException("Unknown general regular expression match: " + token.Value);
                    }
                }
            }

            if (!groupStack.IsEmpty())
            {
                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExUnclosedCaptureGroups);
                return false;
            }

            // simulate "close" on the root expression, setup for back ref checks if the base had any alternations.
            // can't error as there are no quantifiers.
            groupStack.Peek().SeenClose(string.Empty, out _);

            foreach (var backRef in backRefs)
            {
                if (captures[backRef].IsPossibleZeroCapture())
                {
                    errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBackRefToZeroCapture, CharacterUtils.IsDigit(backRef[0]) ? "\\" + backRef : "\\k<" + backRef + ">");
                    return false;
                }
            }

            if (openInlineComment)
            {
                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExUnclosedInlineComment);
                return false;
            }

            // may be modifed by inline options; we only care about x and N in the next stage
            alteredOptions = (freeSpacing ? "x" : string.Empty) + (numberedCpature ? "N" : string.Empty);

            return true;
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

        // Used by IsSupportedRegularExpression, current stack of GroupInfos for open groups.
        // Groups that have been closed may still have references through GroupInfo.Parent and captures list.
        // The depth is limited so that the recursive descent of GroupInfo.IsPossibleZeroCapture() is limited.
        private class GroupStack
        {
            private readonly Stack<GroupInfo> _groupStack = new Stack<GroupInfo>();
            private bool _inLookAround;
            public const int MaxStackDepth = 32;

            public GroupStack()
            {
                // We add a base level for stuff at the root of the regular expression, for example "a|b|c".
                // This level is analyzed for backreferences, where "(a)|\1" should be an error
                Push(new GroupInfo());
            }

            public bool IsEmpty()
            {
                return _groupStack.Count <= 1;
            }

            public bool InLookAround()
            {
                return _inLookAround;
            }

            public bool Push(GroupInfo groupInfo)
            {
                if (groupInfo.IsLookAround)
                {
                    _inLookAround = true;
                }

                if (_groupStack.Count >= MaxStackDepth)
                {
                    return false;
                }

                if (_groupStack.Count != 0)
                {
                    groupInfo.Parent = _groupStack.Peek();
                }

                _groupStack.Push(groupInfo);

                return true;
            }

            public GroupInfo Peek()
            {
                return _groupStack.Peek();
            }

            public GroupInfo Pop()
            {
                var groupInfo = _groupStack.Pop();

                if (groupInfo.IsLookAround)
                {
                    // LookArounds cannot be nested, no need to restore to previous value
                    // We want _inLookAround to be true if there are gruops within the LookAround
                    _inLookAround = false;
                }

                return groupInfo;
            }
        }

        private class GroupInfo
        {
            public bool IsNonCapture;               // this group is a non-capture group
            public bool IsLookAround;               // this gruop is a look behind or look ahead
            public bool IsClosed;                   // this group is closed (we've seen the ending paren), ok to use for backref
            public bool PossibleEmpty = true;       // this group could be empty, set to false if we see something in it that can't be zero sized
            public bool PossibleEmptyAlteration;    // this group contains an alternation, and one part of the alternation could be empty
            public bool ErrorOnZeroQuant;           // we have a possibly empty capture group which has already seen a quantifier, next quantifier gets an error
            public bool ErrorOnAnyQuant;            // used for look arounds, erorr on any quantifier
            public bool HasZeroQuant;               // this group has a quantifier
            public bool HasAlternation;             // this group has an alternation within
            public bool ContainsCapture;            // this gruop contains a cpature, perhaps nested down
            public bool BackRefOK;                  // we've checked, and this back ref is OK
            public GroupInfo Parent;                // reference to the next level down

            public void SeenAlternation()
            {
                // we need to examine each part of the alternation for a potential empty result
                // PossibleEmpty is reset and PossibleEmptyAlternation is the accumulator for any empty parts
                // HasAlternation is important because 

                HasAlternation = true;

                if (PossibleEmpty)
                {
                    PossibleEmptyAlteration = true;
                }

                PossibleEmpty = true;
            }

            public void SeenNonEmpty()
            {
                // groups that contain anything that isn't zero sized (for example, quantified by a * or ?)
                PossibleEmpty = false;
            }

            public bool SeenClose(string lookAhead, out ErrorResourceKey? error)
            {
                IsClosed = true;

                // in processing the end of capture groups, we lookahead for any quantifiers
                //   -- Zero of More, including ranges that being with {0,
                //   -- One or More, including ranges such as {3,10}, excludes the identity {1} and {1,1}
                // quantifiers are later processed again as tokens for other errors, such as {hi,lo}
                var zeroQuant = Regex.IsMatch(lookAhead, @"^(\*|\?|\{0+[,\}])");
                var oneQuant = Regex.IsMatch(lookAhead, @"^(\+|\{0*([2-9]|1[0-9]|1,(?!0*1})))");

                HasZeroQuant = zeroQuant;

                if (HasAlternation)
                {
                    SeenAlternation();                                // closes the last part of the alternation, the "c" in "(a|b|c)"
                    PossibleEmpty = PossibleEmptyAlteration;          // pulls the aggregate as the final value

                    // an alternation with a capture can effectively be empty, for example "(a|(b)|c)".
                    // The capture group "b" in this case is not empty, but it is a capture in an alternation.
                    ErrorOnZeroQuant = ErrorOnZeroQuant || ContainsCapture;   
                }

                // If we are possibly empty, we can't suport a zero quantifier.
                if (PossibleEmpty)
                {
                    ErrorOnZeroQuant = true;
                }

                if ((zeroQuant || oneQuant) && ErrorOnZeroQuant)
                {
                    error = TexlStrings.ErrInvalidRegExQuantifiedCapture;
                    return false;
                }

                // Look arounds can never have quantification, JavaScript doesn't support this.
                // Can't just check the look around group, could be on a containing group, for example "((?=a))*".
                if (IsLookAround)
                {
                    ErrorOnAnyQuant = true;
                }

                // only do this regex test if we have a look around, the only way ErrorOnAnyQuant would be true
                if (ErrorOnAnyQuant && Regex.IsMatch(lookAhead, @"^(\+|\*|\?|\{)"))
                {
                    error = TexlStrings.ErrInvalidRegExQuantifierOursideLookAround;
                    return false;
                }

                // This needs to come after the error checks above, for the next iteration.
                if ((zeroQuant || (oneQuant && (PossibleEmpty || ContainsCapture))) && (!IsNonCapture || ContainsCapture))
                {
                    ErrorOnZeroQuant = true;
                }

                if (Parent != null)
                {
                    if (!PossibleEmpty && !zeroQuant)
                    {
                        Parent.PossibleEmpty = false;
                    }

                    Parent.ErrorOnZeroQuant |= ErrorOnZeroQuant;
                    Parent.ErrorOnAnyQuant |= ErrorOnAnyQuant;

                    if (!IsNonCapture)
                    {
                        Parent.ContainsCapture = true;
                    }
                }

                error = null;
                return true;
            }

            public bool IsPossibleZeroCapture()
            {
                // short circuit in case we are asked about the same capture
                if (BackRefOK)
                {
                    return false;
                }

                int maxDepth = GroupStack.MaxStackDepth;

                for (var ptr = this; ptr != null && maxDepth-- >= 0; ptr = ptr.Parent)
                {
                    if (ptr.HasZeroQuant || ptr.HasAlternation || ptr.PossibleEmpty)
                    {
                        return true;
                    }
                }

                BackRefOK = true;

                return false;
            }
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name²
