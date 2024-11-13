// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        // Limit regular expressions to common features that are supported, with consistent semantics, by both canonical .NET and XRegExp.
        // It is better to disallow now and bring back with customer demand or as platforms add more support.
        //
        // Features that are disallowed:
        //     Capture groups
        //         Numbered capture groups, use named capture groups instead (.NET different from XRegExp).
        //         Self-referncing groups, such as "(a\1)" (.NET different from XRegExp).
        //         Single quoted "(?'name'..." and "\k'name'" (.NET only).
        //         Balancing capture groups (.NET only).
        //     Octal character codes, use \x or \u instead (.NET different from XRegExp)
        //         "\o" could be added in the future, but we should avoid "\0" which causes backreference confusion.
        //     Inline options
        //         Anywhere in the expression except the beginning (.NET only).
        //         For subexpressions (.NET only).
        //     Character classes
        //         Character class subtraction "[a-z-[m-n]]" (.NET only).
        //     Conditional alternation (.NET only).
        //
        // Features that aren't supported by canonical .NET will be blocked automatically when the regular expression is instantiated in TryCreateReturnType.
        //
        // We chose to use canonical .NET instead of RegexOptions.ECMAScript because we wanted the unicode definitions for words.
        // See https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options#ecmascript-matching-behavior for more details
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
                    \\k<(?<backRefName>\w+)>                           | # named backreference
                    (?<badOctal>\\0\d*)                                | # \0 and octal are not accepted, ambiguous and not needed (use \x instead)
                    \\(?<backRefNumber>\d+)                            | # numeric backreference, must be enabled with MatchOptions.NumberedSubMatches
                    (?<goodEscape>\\
                           ([dfnrstw]                              |     # standard regex character classes, missing from .NET are aAeGzZv (no XRegExp support), other common are u{} and o
                            [\^\$\\\.\*\+\?\(\)\[\]\{\}\|\/\#\ ]   |     # acceptable escaped characters with Unicode aware ECMAScript with # and space for Free Spacing
                            c[a-zA-Z]                              |     # Ctrl character classes
                            x[0-9a-fA-F]{2}                        |     # hex character, must be exactly 2 hex digits
                            u[0-9a-fA-F]{4}))                          | # Unicode characters, must be exactly 4 hex digits
                    \\(?<goodUEscape>[pP])\{(?<UCategory>[\w=:-]+)\}   | # Unicode chaeracter classes, extra characters here for a better error message
                    (?<goodEscapeOutsideCC>\\[bB])                     | # acceptable outside a character class, includes negative classes until we have character class subtraction, include \P for future MatchOptions.LocaleAware
                    (?<goodEscapeOutsideAndInsideCCIfPositive>\\[DWS]) |
                    (?<goodEscapeInsideCCOnly>\\[\-%!,:;<=>@`~])       | # https://262.ecma-international.org/#prod-ClassSetReservedPunctuator, others covered with goodEscape above
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
                    (?<goodQuantifiers>[\?\*\+]\??)                    | # greedy and lazy quantifiers
                    (?<badQuantifiers>[\?\*\+][\+\*])                  | # possessive (ends with +) and useless quantifiers (ends with *)

                    # leading {, limited quantifiers
                    (?<goodLimited>{\d+(,\d*)?}\??)                    | # standard limited quantifiers
                    (?<badLimited>{\d+(,\d*)?}[\+|\*])                 | # possessive and useless quantifiers
                    (?<badCurly>[{}])                                  | # more constrained, blocks {,3} and Java/Rust semantics that does not treat this as a literal

                    # character class
                    \[(?<characterClass>(\\\]|\\\[|[^\]\[])+)\]        | # does not accept empty character class
                    (?<badEmptyCharacterClass>\[\])                    |
                    (?<badSquareBrackets>[\[\]])                       |

                    # open and close regions
                    (?<openParen>\()                                   |
                    (?<closeParen>\))                                  |
                    (?<poundComment>\#)                                | # used in free spacing mode (to detect start of comment), ignored otherwise
                    (?<newline>[\r\n])                                   # used in free spacing mode (to detect end of comment), ignored otherwise
                ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            var characterClassRE = new Regex(
                escapeRE +
                @"
                    (?<badHyphen>^-|-$)                                | # literal not allowed within character class, needs to be escaped (ECMAScript v)
                    (?<badInCharClass>  \/ | \| | \\             |       # https://262.ecma-international.org/#prod-ClassSetSyntaxCharacter
                        \{ | \} | \( | \) | \[ | \])                   |
                    (?<badDoubleInCharClass> << | == | >> | ::   |       # reserved pairs, see https://262.ecma-international.org/#prod-ClassSetReservedDoublePunctuator 
                        @@ | `` | ~~ | %% | && | ;; | ,, | !!    |       # and https://www.unicode.org/reports/tr18/#Subtraction_and_Intersection
                        \|\| | \#\# | \$\$ | \*\* | \+\+ | \.\.  |       # includes set subtraction 
                        \?\? | \^\^ | \-\-)                                                                
                ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            int captureNumber = 0;                                  // last numbered capture encountered
            var captureStack = new Stack<string>();                 // stack of all open capture groups, including null for non capturing groups, for detecting if a named group is closed
            var captureNames = new List<string>();                  // list of seen named groups, does not included numbered groups or non capture groups

            bool openPoundComment = false;                          // there is an open end-of-line pound comment, only in freeFormMode
            bool openInlineComment = false;                         // there is an open inline comment

            foreach (Match token in generalRE.Matches(regexPattern))
            {
                void RegExError(ErrorResourceKey errKey, Match errToken = null, bool context = false)
                {
                    if (errToken == null)
                    {
                        errToken = token;
                    }

                    if (context)
                    {
                        const int contextLength = 8;
                        var tokenEnd = errToken.Index + errToken.Length;
                        var found = tokenEnd >= contextLength ? "..." + regexPattern.Substring(tokenEnd - contextLength, contextLength) : regexPattern.Substring(0, tokenEnd);
                        errors.EnsureError(regExNode, errKey, found);
                    }
                    else
                    {
                        errors.EnsureError(regExNode, errKey, errToken.Value);
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
                    if (token.Groups["goodEscape"].Success || token.Groups["goodQuantifiers"].Success || token.Groups["goodLimited"].Success || token.Groups["goodEscapeOutsideCC"].Success || token.Groups["goodEscapeOutsideAndInsideCCIfPositive"].Success)
                    {
                        // all is well, nothing to do
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
                    }
                    else if (token.Groups["goodNamedCapture"].Success)
                    {
                        if (numberedCpature)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExMixingNamedAndNumberedSubMatches);
                            return false;
                        }

                        if (captureNames.Contains(token.Groups["goodNamedCapture"].Value))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExBadNamedCaptureAlreadyExists);
                            return false;
                        }

                        captureStack.Push(token.Groups["goodNamedCapture"].Value);
                        captureNames.Add(token.Groups["goodNamedCapture"].Value);
                    }
                    else if (token.Groups["goodNonCapture"].Success || token.Groups["goodLookaround"].Success)
                    {
                        captureStack.Push(null);
                    }
                    else if (token.Groups["openParen"].Success)
                    {
                        if (numberedCpature)
                        {
                            captureNumber++;
                            captureStack.Push(captureNumber.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            captureStack.Push(null);
                        }
                    }
                    else if (token.Groups["closeParen"].Success)
                    {
                        if (captureStack.Count == 0)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExUnopenedCaptureGroups, context: true);
                            return false;
                        }
                        else
                        {
                            captureStack.Pop();
                        }
                    }
                    else if (token.Groups["backRefName"].Success)
                    {
                        var backRefName = token.Groups["backRefName"].Value;

                        if (numberedCpature)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExMixingNamedAndNumberedSubMatches);
                            return false;
                        }

                        // group isn't defined, or not defined yet
                        if (!captureNames.Contains(backRefName))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExBadBackRefNotDefined);
                            return false;
                        }

                        // group is not closed and thus self referencing
                        if (captureStack.Contains(backRefName))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExBadBackRefSelfReferencing);
                            return false;
                        }
                    }
                    else if (token.Groups["backRefNumber"].Success)
                    {
                        var backRefNumber = Convert.ToInt32(token.Groups["backRefNumber"].Value, CultureInfo.InvariantCulture);

                        if (!numberedCpature)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExNumberedSubMatchesDisabled);
                            return false;
                        }

                        // back ref number has not yet been defined
                        if (backRefNumber < 1 || backRefNumber > captureNumber)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExBadBackRefNotDefined);
                            return false;
                        }

                        // group is not closed and thus self referencing
                        if (captureStack.Contains(token.Groups["goodBackRefNumber"].Value))
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
                        if (Regex.IsMatch(token.Groups["goodInlineOptions"].Value, @"(?<char>.).*\k<char>"))
                        {
                            RegExError(TexlStrings.ErrInvalidRegExRepeatedInlineOption);
                            return false;
                        }

                        if (token.Groups["goodInlineOptions"].Value.Contains("n") && numberedCpature)
                        {
                            RegExError(TexlStrings.ErrInvalidRegExInlineOptionConflictsWithNumberedSubMatches);
                            return false;
                        }

                        if (token.Groups["goodInlineOptions"].Value.Contains("x"))
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
                    else if (token.Groups["badQuantifier"].Success || token.Groups["badLimited"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadQuantifier);
                        return false;
                    }
                    else if (token.Groups["badCurly"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadCurly);
                        return false;
                    }
                    else if (token.Groups["badParen"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadParen, context: true);
                        return false;
                    }
                    else if (token.Groups["badSquareBrackets"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExBadSquare, context: true);
                        return false;
                    }
                    else if (token.Groups["badEmptyCharacterClass"].Success)
                    {
                        RegExError(TexlStrings.ErrInvalidRegExEmptyCharacterClass);
                        return false;
                    }
                    else
                    {
                        // This should never be hit. It is here in case one of the names checked doesn't match the RE, in which case running tests would hit this.
                        throw new NotImplementedException("Unknown general regular expression match: " + token.Value);
                    }
                }
            }

            if (openInlineComment)
            {
                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExUnclosedInlineComment);
                return false;
            }

            if (captureStack.Count > 0)
            {
                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExUnclosedCaptureGroups);
                return false;
            }

            // may be modifed by inline options; we only care about x and N in the next stage
            alteredOptions = (freeSpacing ? "x" : string.Empty) + (numberedCpature ? "N" : string.Empty);

            return true;
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
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name²
