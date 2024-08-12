// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
    // Match(text:s, regular_expression:s, [options:s])
    internal class MatchFunction : BaseMatchFunction
    {
        public MatchFunction(RegexTypeCache regexCache)
            : base("Match", TexlStrings.AboutMatch, DType.EmptyRecord, regexCache)
        {
        }
    }

    // MatchAll(text:s, regular_expression:s, [options:s])
    internal class MatchAllFunction : BaseMatchFunction
    {
        public MatchAllFunction(RegexTypeCache regexCache)
            : base("MatchAll", TexlStrings.AboutMatchAll, DType.EmptyTable, regexCache)
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

        public BaseMatchFunction(string functionName, TexlStrings.StringGetter aboutGetter, DType returnType, RegexTypeCache regexCache)
            : base(functionName, aboutGetter, FunctionCategories.Text, returnType, 0, 2, 3, DType.String, BuiltInEnums.MatchEnum.FormulaType._type, BuiltInEnums.MatchOptionsEnum.FormulaType._type)
        {
            _cachePrefix = returnType.IsTable ? "tbl_" : "rec_";
            _regexTypeCache = regexCache.Cache;
            _regexCacheSize = regexCache.CacheSize;
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

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 2 || args.Length == 3);
            Contracts.AssertValue(errors);

            bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsRecord || returnType.IsTable);
            TexlNode regExNode = args[1];
            string regularExpressionOptions = string.Empty;

            if ((argTypes[1].Kind != DKind.String && argTypes[1].Kind != DKind.OptionSetValue) || !BinderUtils.TryGetConstantValue(context, regExNode, out var regularExpression))
            {
                errors.EnsureError(regExNode, TexlStrings.ErrVariableRegEx);
                return false;
            }

            if (context.Features.PowerFxV1CompatibilityRules && args.Length == 3 && 
                ((argTypes[2].Kind != DKind.String && argTypes[2].Kind != DKind.OptionSetValue) || !BinderUtils.TryGetConstantValue(context, regExNode, out regularExpressionOptions)))
            {
                errors.EnsureError(regExNode, TexlStrings.ErrVariableRegExOptions);
                return false;
            }

            return fValid && 
                    (!context.Features.PowerFxV1CompatibilityRules || IsSupportedRegularExpression(regExNode, regularExpression, regularExpressionOptions, errors)) &&
                    TryCreateReturnType(regExNode, regularExpression, errors, ref returnType);
        }

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
        private bool IsSupportedRegularExpression(TexlNode regExNode, string regexPattern, string regexOptions, IErrorContainer errors)
        {
            // Scans the regular expression for interesting constructs, ignoring other elements and constructs that are legal, such as letters and numbers.
            // Order of alternation is important. .NET regular expressions are greedy and will match the first of these that it can.
            // Many subexpressions here take advantage of this, matching something that is valid, before falling through to check for something that is invalid.
            // 
            // For example, consider testing "\\(\a)".  This will match <goodEscape> <openCapture> <badEscapeAlpha> <closeCapture>.
            // <badEscapeAlpha> will report an error and stop further processing.
            // One might think that the "\a" could have matched <goodEscape>, but it will match <badEscapeAlpha> first because it is first in the RE.
            // One might think that the "\(" could have matched <goodEscape>, but the double backslashes will be consumed first, which is why it is important
            // to gather all the matches in a linear scan from the beginning to the end.
            var groupPunctuationRE = new Regex(
                @"
                    # leading backslash, escape sequences
                    \\k<(?<goodBackRefName>\w+)>                     | # named backreference
                    (?<badOctal>\\0\d*)                              | # octal are not accepted (no XRegExp support, by design)
                    \\(?<goodBackRefNum>\d+)                         | # numeric backreference
                    (?<goodEscapeAlpha>\\
                           ([bBdDfnrsStwW]     |                       # standard regex character classes, missing from .NET are aAeGzZv (no XRegExp support), other common are u{} and o
                            [pP]\{\w+\}        |                       # unicode character classes
                            c[a-zA-Z]          |                       # Ctrl character classes
                            x[0-9a-fA-F]{2}    |                       # hex character, must be exactly 2 hex digits
                            u[0-9a-fA-F]{4}))                        | # Unicode characters, must be exactly 4 hex digits
                    (?<badEscapeAlpha>\\[a-zA-Z_])                   | # reserving all other letters and underscore for future use (consistent with .NET)
                    (?<goodEscape>\\.)                               | # any other escaped character is allowed, but must be paired so that '\\(' is seen as '\\' followed by '(' and not '\' folloed by '\(' 
                                                                    
                    # leading (?<, named captures
                    \(\?<(?<goodNamedCapture>[a-zA-Z][a-zA-Z\d]*)>   | # named capture group, can only be letters and numbers and must start with a letter
                    (?<badBalancing>\(\?<\w*-\w*>)                   | # .NET balancing captures are not supported
                    (?<badNamedCaptureName>\(\?<[^>]*>)              | # bad named capture name, didn't match goodNamedCapture
                    (?<badSingleQuoteNamedCapture>\(\?'[^']*')       | # single quoted capture names are not supported

                    # leading (?, misc
                    (?<goodNonCapture>\(\?:)                         | # non-capture group, still need to track to match with closing paren
                    \A\(\?(?<goodInlineOptions>[imsx]+)\)            |
                    (?<goodInlineComment>\(\?\#[^\)]*\))             | # inline comment
                    (?<goodLookaround>\(\?(=|!|<=|<!))               | # lookahead and lookbehind
                    (?<badInlineOptions>\(\?(\w+|\w*-\w+)[\:\)]?)      | # inline options, including disable of options
                    (?<badConditional>\(\?\()                        | # .NET conditional alternations are not supported
                    (?<badParen>\([\?\+\*\|])                        | # everything else unsupported that could start with a (, includes atomic groups, recursion, subroutines, branch reset, and future features

                    # leading ?\*\+, quantifiers
                    (?<goodQuantifiers>[\?\*\+]\??)                  | # greedy and lazy quantifiers
                    (?<badQuantifiers>[\?\*\+][\+\*])                | # possessive and useless quantifiers

                    # leading {, limited quantifiers
                    (?<badLimited>{\d+(,\d*)?}(\?|\+|\*))            | # possessive and useless quantifiers
                    (?<goodLimited>{\d+(,\d*)?})                     | # standard limited quantifiers
                    (?<badCurly>[{}])                                | # more constrained, blocks {,3} and Java/Rust semantics that does not treat this as a literal

                    # open and close regions
                    (?<badCharacterClassEmpty>\[\])                  | # disallow empty chararcter class (supported by XRegExp) and literal ] at front of character class (supported by .NET)
                    (?<openCapture>\()                               |
                    (?<closeCapture>\))                              |
                    (?<openCharacterClass>\[)                        |
                    (?<closeCharacterClass>\])                       |
                    (?<poundComment>\#)                              |
                    (?<newline>[\r\n])                               |
                    (?<remainingChars>.)
                ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            int groupNumber = 0;
            var groupStack = new Stack<string>();                 // stack of open groups, null is used for non capturing groups, for detecting if a named group is closed
            var groupNames = new List<string>();                  // list of seen group names
            var openComment = false;                              // there is an open end-of-line pound comment, only in freeFormMode
            var freeSpacing = regexOptions.Contains("x");         // can also be set with inline mode modifier
            var numberedCpature = regexOptions.Contains("N");     

            var openCharacterClass = false;                       // are we defining a character class?
            List<char> characterClassRepeat = new List<char>();

            foreach (Match groupMatch in groupPunctuationRE.Matches(regexPattern))
            {
                if (groupMatch.Groups["newline"].Success)
                {
                    openComment = false;
                }
                else if (groupMatch.Groups["poundComment"].Success)
                {
                    openComment = freeSpacing;
                }
                else if (!openComment)
                {
                    // ordered from most common/good to least common/bad, for fewer tests
                    if (groupMatch.Groups["goodEscape"].Success || groupMatch.Groups["goodEscapeAlpha"].Success ||
                        groupMatch.Groups["goodLookaround"].Success || groupMatch.Groups["goodInlineComment"].Success ||
                        groupMatch.Groups["goodLimited"].Success)
                    {
                        // all is well, nothing to do
                    }
                    else if (groupMatch.Groups["openCharacterClass"].Success)
                    {
                        if (openCharacterClass)
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadCharacterClassLiteralSquareBracket);
                            return false;
                        }
                        else
                        {
                            openCharacterClass = true;
                            characterClassRepeat = new List<char>();
                        }
                    }
                    else if (groupMatch.Groups["closeCharacterClass"].Success)
                    {
                        openCharacterClass = false;
                    }
                    else if (groupMatch.Groups["goodNamedCapture"].Success)
                    {
                        // parens do not need to be escaped within square brackets
                        if (!openCharacterClass)
                        {
                            if (numberedCpature)
                            {
                                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExMixingNamedAndNumberedCaptures, groupMatch.Value);
                                return false;
                            }

                            if (groupNames.Contains(groupMatch.Groups["goodNamedCapture"].Value))
                            {
                                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadNamedCaptureAlreadyExists, groupMatch.Value);
                                return false;
                            }

                            groupStack.Push(groupMatch.Groups["goodNamedCapture"].Value);
                            groupNames.Add(groupMatch.Groups["goodNamedCapture"].Value);
                        }
                    }
                    else if (groupMatch.Groups["goodNonCapture"].Success)
                    {
                        // parens do not need to be escaped within square brackets
                        if (!openCharacterClass)
                        {
                            groupStack.Push(null);
                        }
                    }
                    else if (groupMatch.Groups["openCapture"].Success)
                    {
                        // parens do not need to be escaped within square brackets
                        if (!openCharacterClass)
                        {
                            if (!numberedCpature)
                            {
                                var groupName = (++groupNumber).ToString(CultureInfo.InvariantCulture);

                                groupStack.Push(groupName);
                                groupNames.Add(groupName);
                            }
                        }
                    }
                    else if (groupMatch.Groups["closeCapture"].Success)
                    {
                        // parens do not need to be escaped within square brackets
                        if (!openCharacterClass)
                        {
                            if (groupStack.Count == 0)
                            {
                                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExUnopenedCaptureGroups);
                                return false;
                            }
                            else
                            {
                                groupStack.Pop();
                            }
                        }
                    }
                    else if (groupMatch.Groups["goodBackRefName"].Success)
                    {
                        var backRefName = groupMatch.Groups["goodBackRefName"].Value;

                        if (numberedCpature)
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExMixingNamedAndNumberedCaptures, groupMatch.Value);
                            return false;
                        }

                        // group isn't defined, or not defined yet
                        if (!groupNames.Contains(backRefName))
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadBackRefNotDefined, groupMatch.Value);
                            return false;
                        }

                        // group is not closed and thus self referencing
                        if (groupStack.Contains(backRefName))
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadBackRefSelfReferencing, groupMatch.Value);
                            return false;
                        }
                    }
                    else if (groupMatch.Groups["goodBackRefNumber"].Success)
                    {
                        var backRefNumber = groupMatch.Groups["goodBackRefNumber"].Value;

                        if (!numberedCpature)
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExNumberedCaptureDisabled, groupMatch.Value);
                            return false;
                        }

                        // group isn't defined, or not defined yet
                        if (!groupNames.Contains(backRefNumber))
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadBackRefNotDefined, groupMatch.Value);
                            return false;
                        }

                        // group is not closed and thus self referencing
                        if (groupStack.Contains(backRefNumber))
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadBackRefSelfReferencing, groupMatch.Value);
                            return false;
                        }
                    }
                    else if (groupMatch.Groups["goodInlineOptions"].Success)
                    {
                        if (groupMatch.Groups["goodInlineOptions"].Value.Contains("x"))
                        {
                            freeSpacing = true;
                        }
                    }
                    else if (groupMatch.Groups["goodQuantifiers"].Success || groupMatch.Groups["remainingChars"].Success)
                    {
                        if (openCharacterClass)
                        {
                            foreach (char singleChar in groupMatch.Value)
                            {
                                if (characterClassRepeat.Contains(singleChar))
                                {
                                    errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExRepeatInCharClass, singleChar);
                                }
                                else
                                {
                                    characterClassRepeat.Add(singleChar);
                                }
                            }
                        }
                    }
                    else if (groupMatch.Groups["badBackRefNum"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadBackRefNumber, groupMatch.Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badNamedCaptureName"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadNamedCaptureName, groupMatch.Groups["badNamedCaptureName"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badOctal"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadOctal, groupMatch.Groups["badOctal"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badBalancing"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadBalancing, groupMatch.Groups["badBalancing"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badInlineOptions"].Success)
                    {
                        errors.EnsureError(regExNode, groupMatch.Groups["badInlineOptions"].Index > 0 ? TexlStrings.ErrInvalidRegExInlineOptionNotAtStart : TexlStrings.ErrInvalidRegExBadInlineOptions, groupMatch.Groups["badInlineOptions"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badSingleQuoteNamedCapture"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadSingleQuoteNamedCapture, groupMatch.Groups["badSingleQuoteNamedCapture"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badConditional"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadConditional, groupMatch.Groups["badConditional"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badCharacterClassEmpty"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadCharacterClassLiteralSquareBracket, groupMatch.Groups["badCharacterClassEmpty"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badEscapeAlpha"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadEscape, groupMatch.Groups["badEscapeAlpha"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badQuantifier"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadQuantifier, groupMatch.Groups["badQuantifier"].Value);
                        return false;
                    }
                    else if (groupMatch.Groups["badCurly"].Success)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadCurly, groupMatch.Groups["badCurly"].Value);
                        return false;
                    }
                    else
                    {
                        // This should never be hit. Good to have here in case one of the group names checked doesn't match the RE, in which case running tests would hit this.
                        throw new NotImplementedException("Unknown regular expression match");
                    }
                }
            }

            if (groupStack.Count > 0)
            {
                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExUnclosedCaptureGroups);
                return false;
            }

            if (openCharacterClass)
            {
                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExUnclosedCharacterClass);
                return false;
            }

            return true;
        }

        // Creates a typed result: [Match:s, Captures:*[Value:s], NamedCaptures:r[<namedCaptures>:s]]
        private bool TryCreateReturnType(TexlNode regExNode, string regexPattern, IErrorContainer errors, ref DType returnType)
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
                var regex = new Regex(regexPattern);

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

                if (!subMatchesHidden)
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
