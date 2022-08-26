// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal static class IntellisenseHelper
    {
        private static readonly AddSuggestionHelper _addSuggestionHelper = new AddSuggestionHelper();
        private static readonly AddSuggestionDryRunHelper _addSuggestionDryRunHelper = new AddSuggestionDryRunHelper();

        // Gets the inner most function and the current arg index from the current node, if any.
        // If there is no inner most function, current arg index will be -1
        // and argument count will be -1.
        internal static bool TryGetInnerMostFunction(TexlNode nodeCur, TexlBinding bind, out TexlFunction funcCur, out int iarg, out int carg)
        {
            Contracts.AssertValue(nodeCur);
            Contracts.AssertValue(bind);

            var nodeParent = nodeCur.Parent;
            var nodeCallArg = nodeCur;
            CallNode callNode = null;

            while (nodeParent != null)
            {
                if (nodeParent.Kind == NodeKind.Call)
                {
                    callNode = nodeParent.AsCall();
                    break;
                }

                // The last node before a call's list node is the call arg.
                if (nodeParent.Kind != NodeKind.List)
                {
                    nodeCallArg = nodeParent;
                }

                nodeParent = nodeParent.Parent;
            }

            if (callNode == null)
            {
                iarg = -1;
                carg = -1;
                funcCur = null;
                return false;
            }

            Contracts.AssertValue(nodeCallArg);

            var info = bind.GetInfo(callNode);
            if (info.Function != null)
            {
                carg = callNode.Args.Count;
                for (iarg = 0; iarg < carg; iarg++)
                {
                    if (callNode.Args.Children[iarg] == nodeCallArg)
                    {
                        break;
                    }
                }

                Contracts.Assert(iarg < carg);
                funcCur = (TexlFunction)info.Function;
                return true;
            }

            iarg = -1;
            carg = -1;
            funcCur = null;
            return false;
        }

        internal static bool CanSuggestAfterValue(int cursor, string script)
        {
            if (cursor <= 0 || cursor > script.Length)
            {
                return false;
            }

            var result = CharacterUtils.IsSpace(script[cursor - 1]);
            cursor--;

            while (cursor > 0 && CharacterUtils.IsSpace(script[cursor]))
            {
                cursor--;
            }

            return result &= script[cursor] != ',';
        }

        public static bool IsMatch(string input, string match)
        {
            Contracts.AssertValue(input);
            Contracts.AssertValue(match);

            return match == string.Empty ? true : input.StartsWith(match, StringComparison.OrdinalIgnoreCase);
        }

        public static UIString DisambiguateGlobals(IntellisenseSuggestionList curList, UIString curSuggestion, SuggestionKind suggestionKind, DType type)
        {
            Contracts.AssertValue(curList);
            Contracts.AssertValue(curSuggestion);
            Contracts.AssertValid(type);

            var retVal = curSuggestion;
            var sug = curSuggestion.Text;
            var suggestionKindIsGlobalOrScope = suggestionKind == SuggestionKind.Global || suggestionKind == SuggestionKind.ScopeVariable;

            foreach (var s in curList.SuggestionsForText(sug))
            {
                // Retrive global/appVariable suggestions which
                //   -- Don't have the same type as current suggestion (because, if it's of same type, it will be filtered out anyway)
                //   -- Match the current suggestion text (filtered in the loop definition above for efficiency)
                if ((!suggestionKindIsGlobalOrScope &&
                     s.Kind != SuggestionKind.Global &&
                     s.Kind != SuggestionKind.ScopeVariable) ||
                    (s.Type == type && !type.IsControl))
                {
                    continue;
                }

                // The retrived list represents collisions. Update the suggestion text with global disambiguation.

                var punctuatorLen = TexlLexer.PunctuatorAt.Length + TexlLexer.PunctuatorAt.Length;

                // The suggestion already in the list is global. Update it.
                if (s.Kind == SuggestionKind.Global || s.Kind == SuggestionKind.ScopeVariable)
                {
                    var index = curList.IndexOf(s);
                    Contracts.Assert(index >= 0);

                    var dispText = curList[index].DisplayText;

                    // If we are already using the global syntax, we should not add it again.
                    if (dispText.Text.StartsWith(TexlLexer.PunctuatorBracketOpen + TexlLexer.PunctuatorAt))
                    {
                        continue;
                    }

                    curList.UpdateDisplayText(index, new UIString(
                        TexlLexer.PunctuatorBracketOpen + TexlLexer.PunctuatorAt + dispText.Text + TexlLexer.PunctuatorBracketClose,
                        dispText.HighlightStart + punctuatorLen,
                        dispText.HighlightEnd + punctuatorLen));
                }

                // Current suggestion is global. Update it.
                else
                {
                    retVal = new UIString(
                        TexlLexer.PunctuatorBracketOpen + TexlLexer.PunctuatorAt + sug + TexlLexer.PunctuatorBracketClose,
                        curSuggestion.HighlightStart + punctuatorLen,
                        curSuggestion.HighlightEnd + punctuatorLen);
                }
            }

            return retVal;
        }

        public static bool CheckAndAddSuggestion(IntellisenseSuggestion suggestion, IntellisenseSuggestionList suggestionList)
        {
            if (suggestionList.ContainsSuggestion(suggestion.DisplayText.Text))
            {
                return false;
            }

            suggestionList.Add(suggestion);
            return true;
        }

        internal static bool AddSuggestion(IntellisenseData.IntellisenseData intellisenseData, string suggestion, SuggestionKind suggestionKind, SuggestionIconKind iconKind, DType type, bool requiresSuggestionEscaping, uint sortPriority = 0)
        {
            return _addSuggestionHelper.AddSuggestion(intellisenseData, suggestion, suggestionKind, iconKind, type, requiresSuggestionEscaping, sortPriority);
        }

        internal static void AddSuggestionsForMatches(IntellisenseData.IntellisenseData intellisenseData, IEnumerable<string> possibilities, SuggestionKind kind, SuggestionIconKind iconKind, bool requiresSuggestionEscaping, uint sortPriority = 0)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(possibilities);

            foreach (var possibility in possibilities)
            {
                AddSuggestion(intellisenseData, possibility, kind, iconKind, DType.Unknown, requiresSuggestionEscaping, sortPriority);
            }
        }

        internal static void AddSuggestionsForMatches(IntellisenseData.IntellisenseData intellisenseData, IEnumerable<KeyValuePair<string, SuggestionIconKind>> possibilities, SuggestionKind kind, bool requiresSuggestionEscaping, uint sortPriority = 0)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(possibilities);

            foreach (var possibility in possibilities)
            {
                AddSuggestion(intellisenseData, possibility.Key, kind, possibility.Value, DType.Unknown, requiresSuggestionEscaping, sortPriority);
            }
        }

        /// <summary>
        /// Suggest possibilities that can come after a value of a certain type.
        /// </summary>
        internal static void AddSuggestionsForAfterValue(IntellisenseData.IntellisenseData intellisenseData, DType type)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValid(type);

            intellisenseData.Suggestions.Clear();
            intellisenseData.SubstringSuggestions.Clear();
            intellisenseData.SetMatchArea(intellisenseData.ReplacementStartIndex, intellisenseData.ReplacementStartIndex);
            AddSuggestionsForMatches(intellisenseData, TexlLexer.GetOperatorKeywords(type), SuggestionKind.BinaryOperator, SuggestionIconKind.Other, requiresSuggestionEscaping: false);
        }

        /// <summary>
        /// Adds suggestions for an enum, with an optional prefix.
        /// </summary>
        /// <param name="intellisenseData"></param>
        /// <param name="enumInfo"></param>
        /// <param name="prefix"></param>
        internal static void AddSuggestionsForEnum(IntellisenseData.IntellisenseData intellisenseData, EnumSymbol enumInfo, string prefix = "")
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(enumInfo);
            Contracts.AssertValue(prefix);

            var anyCollisionExists = false;
            var locNameTypePairs = new List<Tuple<string, DType>>();

            // We do not need to get the localized names since GetNames will only return invariant.
            // Instead, we use the invariant names later with the enumInfo to retrieve the localized name.
            foreach (var typedName in enumInfo.EnumType.GetNames(DPath.Root))
            {
                enumInfo.TryGetLocValueName(typedName.Name.Value, out var locName).Verify();
                var escapedLocName = TexlLexer.EscapeName(locName);

                var collisionExists = intellisenseData.DoesNameCollide(locName);
                if (collisionExists)
                {
                    var candidate = prefix + escapedLocName;
                    var canAddSuggestion = _addSuggestionDryRunHelper.AddSuggestion(intellisenseData, candidate, SuggestionKind.Global, SuggestionIconKind.Other, typedName.Type, false);
                    anyCollisionExists = anyCollisionExists || canAddSuggestion;
                }

                locNameTypePairs.Add(new Tuple<string, DType>(escapedLocName, typedName.Type));
            }

            foreach (var locNameTypePair in locNameTypePairs)
            {
                var suggestion = anyCollisionExists || !intellisenseData.SuggestUnqualifiedEnums ? prefix + locNameTypePair.Item1 : locNameTypePair.Item1;
                AddSuggestion(intellisenseData, suggestion, SuggestionKind.Global, SuggestionIconKind.Other, locNameTypePair.Item2, false);
            }
        }

        internal static void AddSuggestionsForNamesInType(DType type, IntellisenseData.IntellisenseData data, bool createTableSuggestion)
        {
            Contracts.AssertValid(type);
            Contracts.AssertValue(data);

            foreach (var field in type.GetRootFieldNames()
                .Select(field => (Type: type.GetType(field), Name: field)))
            {
                var usedName = field.Name;
                if (DType.TryGetDisplayNameForColumn(type, usedName, out var maybeDisplayName))
                {
                    usedName = new DName(maybeDisplayName);
                }

                var suggestionType = field.Type;
                if (createTableSuggestion)
                {
                    suggestionType = DType.CreateTable(new TypedName(type, usedName));
                }

                AddSuggestion(data, usedName.Value, SuggestionKind.Field, SuggestionIconKind.Other, suggestionType, requiresSuggestionEscaping: true);
            }
        }

        internal static void AddSuggestionsForNamespace(IntellisenseData.IntellisenseData intellisenseData, IEnumerable<TexlFunction> namespaceFunctions)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(namespaceFunctions);
            Contracts.AssertAllValues(namespaceFunctions);

            foreach (var function in namespaceFunctions)
            {
                // Note we're using the unqualified name, since we're on the RHS of a "namespace." identifier.
                AddSuggestion(intellisenseData, function.Name, SuggestionKind.Function, SuggestionIconKind.Function, function.ReturnType, requiresSuggestionEscaping: true);
            }
        }

        /// <summary>
        /// Adds suggestions that start with the MatchingString from the given type.
        /// </summary>
        internal static void AddTopLevelSuggestions(IntellisenseData.IntellisenseData intellisenseData, DType type, string prefix = "")
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.Assert(type.IsValid);
            Contracts.Assert(prefix.Length == 0 || (TexlLexer.PunctuatorBang + TexlLexer.PunctuatorDot).IndexOf(prefix[prefix.Length - 1]) >= 0);

            foreach (var tName in type.GetRootFieldNames().Select(name => (Type: type.GetType(name), Name: name)))
            {
                if (!intellisenseData.TryAddCustomColumnTypeSuggestions(tName.Type))
                {
                    var usedName = tName.Name;
                    if (DType.TryGetDisplayNameForColumn(type, usedName, out var maybeDisplayName))
                    {
                        usedName = new DName(maybeDisplayName);
                    }

                    AddSuggestion(intellisenseData, prefix + TexlLexer.EscapeName(usedName.Value), SuggestionKind.Global, SuggestionIconKind.Other, tName.Type, requiresSuggestionEscaping: false);
                }
            }
        }

        /// <summary>
        /// Adds suggestions for type scoped at current cursor position.
        /// </summary>
        public static void AddTopLevelSuggestionsForCursorType(IntellisenseData.IntellisenseData intellisenseData, CallNode callNode, int argPosition)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(callNode);
            Contracts.Assert(argPosition >= 0);

            var info = intellisenseData.Binding.GetInfo(callNode);
            Contracts.AssertValue(info);
            var type = info.CursorType;

            // Suggestions are added for the error nodes in the next step.
            if (info.Function != null &&
                argPosition <= info.Function.MaxArity &&
                info.Function.IsLambdaParam(argPosition) &&
                !info.Function.HasSuggestionsForParam(argPosition) &&
                type.IsValid)
            {
                if (info.Function.IsLambdaParam(argPosition) && type.ContainsDataEntityType(DPath.Root))
                {
                    var error = false;
                    type = type.DropAllOfTableRelationships(ref error, DPath.Root);
                    if (error)
                    {
                        return;
                    }
                }

                if (info.ScopeIdentifier != default)
                {
                    AddSuggestion(intellisenseData, info.ScopeIdentifier, SuggestionKind.Global, SuggestionIconKind.Other, type, requiresSuggestionEscaping: false);
                }

                if (!info.RequiresScopeIdentifier)
                {
                    AddTopLevelSuggestions(intellisenseData, type);
                }
            }
        }

        internal static DType GetEnumType(IntellisenseData.IntellisenseData intellisenseData, TexlNode node)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(node);

            DottedNameNode dottedNode;
            FirstNameNode firstNameNode;
            if ((dottedNode = node.AsDottedName()) != null)
            {
                return intellisenseData.Binding.GetType(dottedNode.Left);
            }

            if ((firstNameNode = node.AsFirstName()) != null)
            {
                var firstNameInfo = intellisenseData.Binding.GetInfo(firstNameNode).VerifyValue();

                var parent = firstNameInfo.Path.Parent;
                if (!parent.Name.IsValid)
                {
                    return DType.Unknown;
                }

                if (intellisenseData.TryGetEnumSymbol(parent.Name, intellisenseData.Binding, out var enumSymbol))
                {
                    return enumSymbol.EnumType;
                }
            }

            return DType.Unknown;
        }

        internal static DType ClosestParentScopeTypeForSuggestions(CallNode callNode, IntellisenseData.IntellisenseData intellisenseData)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(intellisenseData);

            TexlNode currentNode = callNode;
            while (currentNode.Parent != null)
            {
                currentNode = currentNode.Parent;
                if (currentNode is CallNode callNodeCurr)
                {
                    var parentScopeType = ScopeTypeForArgumentSuggestions(callNodeCurr, intellisenseData);
                    if (parentScopeType != null && parentScopeType != DType.Unknown)
                    {
                        return parentScopeType;
                    }
                }
            }

            return DType.Unknown;
        }

        internal static IEnumerable<KeyValuePair<string, DType>> GetColumnNameStringSuggestions(DType scopeType)
        {
            Contracts.AssertValid(scopeType);

            foreach (var name in scopeType.GetRootFieldNames()
                .Select(field => (Type: scopeType.GetType(field), Name: field)))
            {
                yield return new KeyValuePair<string, DType>("\"" + CharacterUtils.ExcelEscapeString(name.Name.Value) + "\"", name.Type);
            }
        }

        internal static IEnumerable<KeyValuePair<string, DType>> GetSuggestionsFromType(DType typeToSuggestFrom, DType suggestionType)
        {
            Contracts.AssertValid(typeToSuggestFrom);
            Contracts.AssertValid(suggestionType);

            // If no suggestion type provided, accept all suggestions.
            if (suggestionType == DType.Invalid)
            {
                suggestionType = DType.Error;
            }

            var suggestions = new List<KeyValuePair<string, DType>>();
            foreach (var tName in typeToSuggestFrom.GetRootFieldNames()
                .Select(field => (Type: typeToSuggestFrom.GetType(field), Name: field)))
            {
                if (suggestionType.Accepts(tName.Type))
                {
                    var usedName = tName.Name.Value;
                    if (DType.TryGetDisplayNameForColumn(typeToSuggestFrom, usedName, out var maybeDisplayName))
                    {
                        usedName = maybeDisplayName;
                    }

                    suggestions.Add(new KeyValuePair<string, DType>(usedName, tName.Type));
                }
            }

            return suggestions;
        }

        public static DType ScopeTypeForArgumentSuggestions(CallNode callNode, IntellisenseData.IntellisenseData intellisenseData)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(intellisenseData);

            var info = intellisenseData.Binding.GetInfo(callNode);
            if (info.Function.UseParentScopeForArgumentSuggestions)
            {
                return ClosestParentScopeTypeForSuggestions(callNode, intellisenseData);
            }

            if (callNode.Args.Count <= info.Function.SuggestionTypeReferenceParamIndex)
            {
                return DType.Unknown;
            }

            var referenceArg = callNode.Args.Children[info.Function.SuggestionTypeReferenceParamIndex];
            return info.Function.UsesEnumNamespace ? GetEnumType(intellisenseData, referenceArg) : intellisenseData.Binding.GetType(referenceArg);
        }

        /// <summary>
        /// Adds suggestions for given argument position.
        /// Returns true if any function specific suggestions are added to the list. Otherwise false.
        /// </summary>
        /// <returns></returns>
        public static bool TryAddSpecificSuggestionsForGivenArgPosition(IntellisenseData.IntellisenseData intellisenseData, CallNode callNode, int argumentIndex)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(callNode);
            Contracts.Assert(argumentIndex >= 0);

            if (callNode.Args.Count < 0 ||
                intellisenseData.CurFunc == null ||
                !intellisenseData.CurFunc.HasSuggestionsForParam(argumentIndex))
            {
                return false;
            }

            // Adding suggestions for callNode arguments from the Function's GetArgumentSuggestions method
            // Keep track of the previous suggestion counts so we can tell whether or not we have added any
            var countSuggestionsBefore = intellisenseData.Suggestions.Count;
            var countSubstringSuggestionsBefore = intellisenseData.SubstringSuggestions.Count;

            // If the function has specific suggestions for the argument,
            // indicate it to the caller.
            var function = intellisenseData.CurFunc;
            var suggestionKind = intellisenseData.GetFunctionSuggestionKind(function, argumentIndex);
            ErrorNode err;

            if (argumentIndex >= 0 && argumentIndex < callNode.Args.Count && ((err = callNode.Args.Children[argumentIndex].AsError()) != null)
                && err.Token.Span.StartsWith(intellisenseData.Script, "\""))
            {
                intellisenseData.CleanupHandlers.Add(new StringSuggestionHandler(err.Token.Span.Min));
            }

            var scopeType = ScopeTypeForArgumentSuggestions(callNode, intellisenseData);

            var argSuggestions = intellisenseData.GetArgumentSuggestions(function, scopeType, argumentIndex, callNode.Args.Children, out var requiresSuggestionEscaping);

            foreach (var suggestion in argSuggestions)
            {
                AddSuggestion(intellisenseData, suggestion.Key, suggestionKind, SuggestionIconKind.Function, suggestion.Value, requiresSuggestionEscaping);
            }

            return intellisenseData.Suggestions.Count > countSuggestionsBefore ||
                    intellisenseData.SubstringSuggestions.Count > countSubstringSuggestionsBefore;
        }

        /// <summary>
        /// Adds suggestions for a given node.
        /// </summary>
        /// <param name="intellisenseData"></param>
        /// <param name="node">Node for which suggestions are needed.</param>
        /// <param name="currentNode">Current node in the traversal.</param>
        public static bool AddTopLevelSuggestionsForGivenNode(IntellisenseData.IntellisenseData intellisenseData, TexlNode node, TexlNode currentNode)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(node);
            Contracts.AssertValue(currentNode);

            var callNode = GetNearestCallNode(currentNode);
            if (callNode == null)
            {
                return false;
            }

            if (callNode.Args.Count == 0)
            {
                AddTopLevelSuggestionsForCursorType(intellisenseData, callNode, 0);
                return TryAddSpecificSuggestionsForGivenArgPosition(intellisenseData, callNode, 0);
            }

            for (var i = 0; i < callNode.Args.Count; i++)
            {
                if (node.InTree(callNode.Args.Children[i]))
                {
                    AddTopLevelSuggestionsForCursorType(intellisenseData, callNode, i);
                    if (ReferenceEquals(node, currentNode) && TryAddSpecificSuggestionsForGivenArgPosition(intellisenseData, callNode, i))
                    {
                        return true;
                    }
                }
            }

            if (callNode.Parent != null)
            {
                return AddTopLevelSuggestionsForGivenNode(intellisenseData, node, callNode.Parent);
            }

            return false;
        }

        private static CallNode GetNearestCallNode(TexlNode node)
        {
            Contracts.AssertValue(node);
            var parent = node;

            while (parent != null)
            {
                CallNode callNode;
                if ((callNode = parent.AsCall()) != null)
                {
                    return callNode;
                }

                parent = parent.Parent;
            }

            return null;
        }

        /// <summary>
        /// Adds suggestions that start with the matchingString from the result from the types in scope.
        /// </summary>
        internal static bool AddSuggestionsForTopLevel(IntellisenseData.IntellisenseData intellisenseData, TexlNode node)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(node);

            return AddTopLevelSuggestionsForGivenNode(intellisenseData, node, node);
        }

        /// <summary>
        /// This is a Private method used only by the SuggestFunctions() method to add overload suggestions to the existing suggestions.
        /// </summary>
        public static void AddFunctionOverloads(string qualifiedName, IntellisenseSuggestionList suggestions, IntellisenseSuggestion funcSuggestion)
        {
            Contracts.AssertNonEmpty(qualifiedName);
            Contracts.AssertValue(suggestions);
            Contracts.AssertValue(funcSuggestion);

            var overloadMatch = suggestions.FindIndex(x => (x.Kind == SuggestionKind.Function && x.FunctionName == qualifiedName));

            if (overloadMatch > -1)
            {
                suggestions[overloadMatch].AddOverloads(funcSuggestion.Overloads);
            }
            else
            {
                CheckAndAddSuggestion(funcSuggestion, suggestions);
            }
        }

        internal static void AddSuggestionsForFunctions(IntellisenseData.IntellisenseData intellisenseData)
        {
            // TASK: 76039: Intellisense: Update intellisense to filter suggestions based on the expected type of the text being typed in UI
            Contracts.AssertValue(intellisenseData);

            foreach (var function in intellisenseData.Binding.NameResolver.Functions)
            {
                var qualifiedName = function.QualifiedName;
                var highlightStart = qualifiedName.IndexOf(intellisenseData.MatchingStr, StringComparison.OrdinalIgnoreCase);
                var highlightEnd = intellisenseData.MatchingStr.Length;
                if (intellisenseData.ShouldSuggestFunction(function))
                {
                    if (IsMatch(qualifiedName, intellisenseData.MatchingStr))
                    {
                        AddFunctionOverloads(qualifiedName, intellisenseData.Suggestions, new IntellisenseSuggestion(function, intellisenseData.BoundTo, new UIString(qualifiedName, 0, highlightEnd)));
                    }
                    else if (highlightStart > -1)
                    {
                        AddFunctionOverloads(qualifiedName, intellisenseData.SubstringSuggestions, new IntellisenseSuggestion(function, intellisenseData.BoundTo, new UIString(qualifiedName, highlightStart, highlightStart + highlightEnd)));
                    }
                }
            }
        }

        /// <summary>
        /// Based on our current token, determine how much of it should be replaced.
        /// </summary>
        /// <param name="intellisenseData"></param>
        /// <param name="tokenStart"></param>
        /// <param name="tokenEnd"></param>
        /// <param name="validNames"></param>
        /// <returns></returns>
        internal static int GetReplacementLength(IntellisenseData.IntellisenseData intellisenseData, int tokenStart, int tokenEnd, IEnumerable<string> validNames)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.Assert(tokenStart <= intellisenseData.CursorPos);
            Contracts.Assert(intellisenseData.CursorPos <= tokenEnd);
            Contracts.Assert(tokenEnd <= intellisenseData.Script.Length);
            Contracts.AssertAllNonEmpty(validNames);

            var cursorPos = intellisenseData.CursorPos;

            // If the cursor is at the start of a token, then do not replace anything
            if (tokenStart == intellisenseData.CursorPos)
            {
                return 0;
            }

            var forwardOverwrittenText = intellisenseData.Script.Substring(cursorPos, tokenEnd - cursorPos);
            foreach (var validName in validNames)
            {
                Contracts.AssertNonEmpty(validName);

                if (validName == forwardOverwrittenText)
                {
                    return cursorPos - tokenStart;   // If the remaining text of the token is a valid name, we shouldn't overwrite it
                }
            }

            return tokenEnd - tokenStart;
        }

        /// <summary>
        /// Adds suggestions that start with the matchingString from the top level scope of the binding.
        /// </summary>
        internal static void AddSuggestionsForRuleScope(IntellisenseData.IntellisenseData intellisenseData)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(intellisenseData.Binding);

            var scopeType = intellisenseData.ContextScope;
            if (scopeType == null)
            {
                return;
            }

            AddTopLevelSuggestions(intellisenseData, scopeType);
        }

        internal static void AddSuggestionsForGlobals(IntellisenseData.IntellisenseData intellisenseData)
        {
            Contracts.AssertValue(intellisenseData);

            intellisenseData.AddCustomSuggestionsForGlobals();

            // Suggest function namespaces
            var namespaces = intellisenseData.Binding.NameResolver.Functions.Select(func => func.Namespace).Distinct();
            foreach (var funcNamespace in namespaces)
            {
                if (funcNamespace == DPath.Root)
                {
                    continue;
                }

                AddSuggestion(intellisenseData, funcNamespace.Name, SuggestionKind.Global, SuggestionIconKind.Other, DType.Unknown, requiresSuggestionEscaping: true);
            }

            if (intellisenseData.Binding.NameResolver is IGlobalSymbolNameResolver nr2)
            {
                var globalSymbols = nr2.GlobalSymbols;
                if (globalSymbols != null)
                {
                    foreach (var symbol in globalSymbols)
                    {
                        var suggestableName = symbol.Key;
                        if (symbol.Value.DisplayName.IsValid)
                        {
                            suggestableName = symbol.Value.DisplayName.Value;
                        }

                        AddSuggestion(intellisenseData, suggestableName, SuggestionKind.Global, SuggestionIconKind.Other, symbol.Value.Type, true);
                    }
                }
            }
        }

        public static void AddSuggestionsForUnaryOperatorKeyWords(IntellisenseData.IntellisenseData intellisenseData)
        {
            Contracts.AssertValue(intellisenseData);

            // TASK: 76039: Intellisense: Update intellisense to filter suggestions based on the expected type of the text being typed in UI
            AddSuggestionsForMatches(intellisenseData, TexlLexer.GetUnaryOperatorKeywords(), SuggestionKind.KeyWord, SuggestionIconKind.Other, requiresSuggestionEscaping: false);
        }

        public static void AddSuggestionsForEnums(IntellisenseData.IntellisenseData intellisenseData)
        {
            Contracts.AssertValue(intellisenseData);

            var suggestions = intellisenseData.Suggestions;
            var substringSuggestions = intellisenseData.SubstringSuggestions;
            var countSuggBefore = suggestions.Count;
            var countSubSuggBefore = substringSuggestions.Count;

            foreach (var enumInfo in intellisenseData.EnumSymbols)
            {
                var enumType = enumInfo.EnumType;
                var enumName = enumInfo.Name;

                // TASK: 76039: Intellisense: Update intellisense to filter suggestions based on the expected type of the text being typed in UI
                AddSuggestion(intellisenseData, enumName, SuggestionKind.Enum, SuggestionIconKind.Other, enumType, requiresSuggestionEscaping: true);

                AddSuggestionsForEnum(intellisenseData, enumInfo, prefix: enumName + TexlLexer.PunctuatorDot);
            }

            if (suggestions.Count + substringSuggestions.Count == countSuggBefore + countSubSuggBefore + 1 && intellisenseData.SuggestUnqualifiedEnums)
            {
                var enumSuggestion = suggestions.Count > countSuggBefore ? suggestions[countSuggBefore].Text : substringSuggestions[countSubSuggBefore].Text;
                var dotIndex = enumSuggestion.LastIndexOf(TexlLexer.PunctuatorDot);

                // Assert '.' is not present or not at the beginning or the end of the EnumSuggestion
                Contracts.Assert(dotIndex == -1 || (dotIndex > 0 && dotIndex < enumSuggestion.Length - 1));
                var unqualifiedEnum = enumSuggestion.Substring(dotIndex + 1);

                // If the Enum we are about suggest unqualified (i.e. just 'Blue' instead of Color!Blue)
                // has a name collision with some Item already in the suggestionlist we should not continue
                // and suggest it.
                if (suggestions.Any(x => x.Text == unqualifiedEnum) || substringSuggestions.Any(x => x.Text == unqualifiedEnum))
                {
                    return;
                }

                DType enumType;
                if (suggestions.Count > countSuggBefore)
                {
                    enumType = suggestions[countSuggBefore].Type;
                    suggestions.RemoveAt(suggestions.Count - 1);
                }
                else
                {
                    Contracts.Assert(substringSuggestions.Count > countSubSuggBefore);
                    enumType = substringSuggestions[countSubSuggBefore].Type;
                    substringSuggestions.RemoveAt(substringSuggestions.Count - 1);
                }

                // Add the unqualified Enum.
                // Note: The suggestion has already been escaped when it was previously added
                AddSuggestion(intellisenseData, unqualifiedEnum, SuggestionKind.Enum, SuggestionIconKind.Other, enumType, requiresSuggestionEscaping: false);
            }
        }

        internal static bool AddSuggestionsForValuePossibilities(IntellisenseData.IntellisenseData intellisenseData, TexlNode node)
        {
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(node);

            var suggestionCount = intellisenseData.Suggestions.Count() + intellisenseData.SubstringSuggestions.Count();
            AddSuggestionsForRuleScope(intellisenseData);
            AddSuggestionsForTopLevel(intellisenseData, node);
            AddSuggestionsForFunctions(intellisenseData);
            intellisenseData.AddSuggestionsForConstantKeywords();
            AddSuggestionsForGlobals(intellisenseData);
            intellisenseData.AfterAddSuggestionsForGlobals();
            AddSuggestionsForUnaryOperatorKeyWords(intellisenseData);
            intellisenseData.AfterAddSuggestionsForUnaryOperatorKeywords();
            AddSuggestionsForEnums(intellisenseData);

            intellisenseData.AddCustomSuggestionsForValuePossibilities();

            return suggestionCount < (intellisenseData.Suggestions.Count() + intellisenseData.SubstringSuggestions.Count());
        }

        internal static bool IsPunctuatorColonNextToCursor(int cursorPos, string script)
        {
            Contracts.Assert(cursorPos >= 0 && cursorPos <= script.Length);

            var colonLen = TexlLexer.PunctuatorColon.Length;
            return script.Length >= cursorPos + colonLen && script.Substring(cursorPos, colonLen) == TexlLexer.PunctuatorColon;
        }
    }
}
