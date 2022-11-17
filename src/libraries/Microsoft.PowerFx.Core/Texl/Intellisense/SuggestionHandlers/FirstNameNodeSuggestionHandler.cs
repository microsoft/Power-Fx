// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class FirstNameNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public FirstNameNodeSuggestionHandler()
                : base(NodeKind.FirstName)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;
                var cursorPos = intellisenseData.CursorPos;

                var firstNameNode = curNode.CastFirstName();
                var ident = firstNameNode.Ident;
                var min = ident.Token.Span.Min;
                var tok = ident.Token;

                if (cursorPos < min)
                {
                    // Cursor is before the beginning of the identifier or of the global token if present.
                    // Suggest possibilities that can result in a value.
                    IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, curNode);
                    intellisenseData.AddAdditionalSuggestionsForKeywordSymbols(curNode);
                }
                else if (cursorPos <= tok.Span.Lim)
                {
                    // Cursor is part of the identifier or global token if present.
                    // Get the matching string as a substring from the script so that the whitespace is preserved.
                    var possibleFirstNames = intellisenseData.Binding.GetFirstNames().Select(firstNameInfo => firstNameInfo.Name.Value)
                        .Union(intellisenseData.Binding.GetGlobalNames().Select(firstNameInfo => firstNameInfo.Name.Value))
                        .Union(intellisenseData.Binding.GetAliasNames().Select(firstNameInfo => firstNameInfo.Name.Value))
                        .Union(intellisenseData.SuggestableFirstNames);

                    var replacementLength = IntellisenseHelper.GetReplacementLength(intellisenseData, tok.Span.Min, tok.Span.Lim, possibleFirstNames);
                    intellisenseData.SetMatchArea(tok.Span.Min, cursorPos, replacementLength);
                    intellisenseData.BoundTo = intellisenseData.Binding.ErrorContainer.HasErrors(firstNameNode) ? string.Empty : ident.Name;

                    if (ident.AtToken != null || tok.Kind == TokKind.At)
                    {
                        // Suggest globals if cursor is after '@'.
                        AddSuggestionsForScopedGlobals(intellisenseData);
                    }
                    else if (tok.HasDelimiters && cursorPos > tok.Span.Min)
                    {
                        // Suggest top level fields and globals if cursor is after a opening square bracket.
                        IntellisenseHelper.AddSuggestionsForRuleScope(intellisenseData);
                        IntellisenseHelper.AddSuggestionsForTopLevel(intellisenseData, curNode);
                        IntellisenseHelper.AddSuggestionsForGlobals(intellisenseData);
                        intellisenseData.AddAdditionalSuggestionsForLocalSymbols();
                    }
                    else
                    {
                        // Suggest value posssibilities otherwise.
                        IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, curNode);
                    }

                    intellisenseData.AddAdditionalSuggestionsForKeywordSymbols(curNode);
                }
                else if (IsBracketOpen(tok.Span.Lim, cursorPos, intellisenseData.Script) && 
                    !intellisenseData.Features.HasFlag(Features.DisableRowScopeDisambiguationSyntax))
                {
                    AddSuggestionsForScopeFields(intellisenseData, intellisenseData.Binding.GetType(firstNameNode));
                }
                else if (IntellisenseHelper.CanSuggestAfterValue(cursorPos, intellisenseData.Script))
                {
                    // Verify that cursor is after a space after the identifier.
                    // Suggest binary keywords.
                    IntellisenseHelper.AddSuggestionsForAfterValue(intellisenseData, intellisenseData.Binding.GetType(firstNameNode));
                }

                return true;
            }

            // Suggest the Globals that can appear in the context of '[@____]'
            // Suggesting controls, datasources, appVariables, and enums.
            private static void AddSuggestionsForScopedGlobals(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var suggestions = intellisenseData.AdditionalGlobalSuggestions
                    .Union(intellisenseData.EnumSymbols.Select(symbol => new KeyValuePair<string, SuggestionIconKind>(symbol.Name, SuggestionIconKind.Other)));

                IntellisenseHelper.AddSuggestionsForMatches(intellisenseData, suggestions, SuggestionKind.Global, requiresSuggestionEscaping: true);
            }

            // Verify that there is only one bracketOpen and possibly multiple spaces
            // between the begin and cursor position.
            private static bool IsBracketOpen(int begin, int cursorPos, string script)
            {
                Contracts.Assert(begin <= cursorPos);
                Contracts.Assert(script.Length >= cursorPos);

                // Failsafe for index out of bounds exception.
                if (begin < 0 || script.Length < cursorPos)
                {
                    return false;
                }

                var bracketOpenCount = 0;
                for (var i = begin; i < cursorPos; i++)
                {
                    if (TexlLexer.PunctuatorBracketOpen.Equals(script[i].ToString()))
                    {
                        bracketOpenCount++;
                    }
                    else if (bracketOpenCount > 1 || !CharacterUtils.IsSpace(script[i]))
                    {
                        return false;
                    }
                }

                return bracketOpenCount == 1;
            }

            private static void AddSuggestionsForScopeFields(IntellisenseData.IntellisenseData intellisenseData, DType scope)
            {
                Contracts.AssertValue(intellisenseData);
                Contracts.Assert(scope.IsValid);

                foreach (var field in scope.GetNames(DPath.Root))
                {
                    IntellisenseHelper.AddSuggestion(intellisenseData, TexlLexer.PunctuatorAt + TexlLexer.EscapeName(field.Name.Value), SuggestionKind.Field, SuggestionIconKind.Other, field.Type, requiresSuggestionEscaping: false);
                }
            }
        }
    }
}
