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
            fValid &= LimitRegularExpression(regExNode, regularExpression, errors);
            fValid &= TryCreateReturnType(regExNode, regularExpression, errors, ref returnType);
            return fValid;
        }

        // Disallow self referncing groups and consitently treat all "\[1-9][0-9]*" as a backreference.
        // This avoids inconsistencies between .net and JavaScript regular expression behavior
        // while maintaining Unicode definition of words, by avoiding the use of RegexOptions.ECMAScript.
        // See https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options#ecmascript-matching-behavior
        private bool LimitRegularExpression(TexlNode regExNode, string regexPattern, IErrorContainer errors)
        {
            // scans the regular expression, counting capture groups and comparing with backreference numbers.
            var groupPunctuationRE = new Regex(
                @"(
                    \\(\d+)|  # valid backreference or octal character is accepted, others are an error
                    \\[^\d]|  # any other escaped character is ignored, but must be paired so that '\\(' is seen as '\\' followed by '('
                    \(|\)|    # parens that aren't escaped that could start/end a group, provided they are outside a character class
                    \[|\]     # character class start/end
                )", RegexOptions.IgnorePatternWhitespace);
            var groupStack = new Stack<int>(); // int is the group number, -1 is used for non capturing groups
            var groupCounter = 0; // last group number defined
            var openCharacterClass = false; // are we inside square brackets where parens do not need to be escaped?

            foreach (Match groupMatch in groupPunctuationRE.Matches(regexPattern))
            {
                switch (groupMatch.Value[0])
                {
                    case '\\':
                        // backslash with anything but digits is always accepted
                        // octal characters are accepted that start with a '0'
                        if (groupMatch.Groups[2].Value != string.Empty && groupMatch.Groups[2].Value[0] != '0')
                        {
                            int backslashNum = Convert.ToInt32(groupMatch.Groups[2].Value, CultureInfo.InvariantCulture);

                            // group isn't defined, or not defined yet
                            if (backslashNum > groupCounter)
                            {
                                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegEx);
                                return false;
                            }

                            // group is not closed and thus self referencing
                            if (groupStack.Contains(backslashNum))
                            {
                                errors.EnsureError(regExNode, TexlStrings.ErrInvalidRegEx);
                                return false;
                            }
                        }

                        break;

                    case '(':
                        // parens do not need to be escaped within square brackets
                        if (!openCharacterClass)
                        {
                            // non capturing group still needs to match closing paren, but does not define a new group
                            groupStack.Push(groupMatch.Value.StartsWith("(?:", StringComparison.InvariantCulture) ? -1 : ++groupCounter);
                        }

                        break;

                    case ')':
                        // parens do not need to be escaped within square brackets
                        if (!openCharacterClass)
                        {
                            groupStack.Pop();
                        }

                        break;

                    case '[':
                        // note that square brackets do not nest, "[[]]" is "[[]" followed by the character "]"
                        openCharacterClass = true;
                        break;

                    case ']':
                        openCharacterClass = false;
                        break;
                }
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
