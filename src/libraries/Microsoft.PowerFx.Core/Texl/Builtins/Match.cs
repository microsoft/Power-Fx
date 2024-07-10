// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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

            if ((argTypes[1].Kind != DKind.String && argTypes[1].Kind != DKind.OptionSetValue) || !BinderUtils.TryGetConstantValue(context, regExNode, out var nodeValue))
            {
                errors.EnsureError(regExNode, TexlStrings.ErrVariableRegEx);
                return false;
            }

            string regularExpression = nodeValue;
            return fValid && 
                    (!context.Features.PowerFxV1CompatibilityRules || IsSupportedRegularExpression(regExNode, regularExpression, errors)) &&
                    TryCreateReturnType(regExNode, regularExpression, errors, ref returnType);
        }

        // Limit regular expressions to common features that are supported, with consistent semantics, by both canonical .NET and XRegExp.
        // It is better to disallow now and bring back with customer demand or as platforms add more support.
        //
        // Features that are disallowed:
        //     Capture groups
        //         Self-referncing groups, such as "(a\1)" (.NET different from XRegExp).
        //         Treat all escaped number sequences as a backreference number (.NET different from XRegExp).
        //         Single quoted "(?'name'..." and "\k'name'" (.NET only).
        //         Balancing capture groups (.NET only).
        //         Using named captures with back reference \number (.NET different from XRegExp).
        //         Using \k<number> notation for numeric back references (.NET different from XRegExp).
        //     Octal character codes (.NET different from XRegExp)
        //         Uuse Hex or Unicode instead.
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
        private bool IsSupportedRegularExpression(TexlNode regExNode, string regexPattern, IErrorContainer errors)
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
                    (?<badOctal>\\0[0-7]{0,3})                       | # octal are not accepted (no XRegExp support, by design)
                    (?<badBackRefNum>\\\d+)                          | # numeric backreference
                    (?<goodEscapeAlpha>\\
                           ([bBdDfnrsStvwW]    |                       # standard regex character classes, missing from .NET are aAeGzZ (no XRegExp support), other common are u{} and o
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

                    # leading (?
                    (?<goodNonCapture>\(\?:)                         | # non-capture group, still need to track to match with closing
                    (?<goodOptions>^\(\?[im]+\))                     | # inline front of expression options we do support
                    (?<badOptions>\(\?(\w*-\w*|\w+)(:|\))?)          | # inline options that we don't support, including disable of options (last ? portion makes for a better error message)
                    (?<badConditional>\(\?\()                        | # .NET conditional alternations are not supported

                    # basic open and close
                    (?<badCharacterClassEmpty>\[\])                  | # disallow empty chararcter class (supported by XRegExp) and literal ] at front of character class (supported by .NET)
                    (?<openCapture>\()                               |
                    (?<closeCapture>\))                              |
                    (?<openCharacterClass>\[)                        |
                    (?<closeCharacterClass>\])
                ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            var groupStack = new Stack<string>();                 // stack of open group numbers, null is used for non capturing groups
            var groupNames = new List<string>();                  // list of known group names

            var openCharacterClass = false;                       // are we defining a character class?

            foreach (Match groupMatch in groupPunctuationRE.Matches(regexPattern))
            {
                // ordered from most common/good to least common/bad, for fewer tests
                if (groupMatch.Groups["goodEscape"].Success || groupMatch.Groups["goodEscapeAlpha"].Success || groupMatch.Groups["goodOptions"].Success)
                {
                    // all is well, nothing to do
                }
                else if (groupMatch.Groups["openCharacterClass"].Success)
                {
                    if (openCharacterClass)
                    {
                        // character class subtraction "[a-z-[m-n]]" is not supported
                        if (regexPattern[groupMatch.Groups["openCharacterClass"].Index - 1] == '-')
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadCharacterClassSubtraction);
                            return false;
                        }
                        else
                        {
                            errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadCharacterClassLiteralSquareBracket);
                            return false;
                        }
                    }
                    else
                    {
                        openCharacterClass = true;
                    }
                }
                else if (groupMatch.Groups["closeCharacterClass"].Success)
                {
                    openCharacterClass = false;
                }
                else if (groupMatch.Groups["openCapture"].Success || groupMatch.Groups["goodNonCapture"].Success || groupMatch.Groups["goodNamedCapture"].Success)
                {
                    // parens do not need to be escaped within square brackets
                    if (!openCharacterClass)
                    {
                        // non capturing group still needs to match closing paren, but does not define a new group
                        if (groupMatch.Groups["goodNamedCapture"].Success)
                        {
                            groupStack.Push(groupMatch.Groups["goodNamedCapture"].Value);
                            if (groupNames.Contains(groupMatch.Groups["goodNamedCapture"].Value))
                            {
                                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadNamedCaptureAlreadyExists, groupMatch.Value);
                                return false;
                            }

                            groupNames.Add(groupMatch.Groups["goodNamedCapture"].Value);
                        }
                        else
                        {
                            groupStack.Push(null);
                        }
                    }
                }
                else if (groupMatch.Groups["closeCapture"].Success)
                {
                    // parens do not need to be escaped within square brackets
                    if (!openCharacterClass)
                    {
                        groupStack.Pop();
                    }
                }
                else if (groupMatch.Groups["goodBackRefName"].Success)
                {
                    var backRefName = groupMatch.Groups["goodBackRefName"].Value;

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
                else if (groupMatch.Groups["badOptions"].Success)
                {
                    if (groupMatch.Groups["badOptions"].Index > 0)
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadOptionsNotAtFront, groupMatch.Groups["badOptions"].Value);
                    }
                    else
                    {
                        errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegExBadOptions, groupMatch.Groups["badOptions"].Value);
                    }

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
                else
                {
                    // This should never be hit. Good to have here in case one of the group names checked doesn't match the RE, running tests would hit this.
                    throw new NotImplementedException("Unknown regular expression match");
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
