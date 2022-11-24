// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class DottedNameNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public DottedNameNodeSuggestionHandler()
                : base(NodeKind.DottedName)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;
                var cursorPos = intellisenseData.CursorPos;

                // Cursor position is after the dot (If it was before the dot FindNode would have returned the left node).
                Contracts.Assert(curNode.Token.IsDottedNamePunctuator);
                Contracts.Assert(curNode.Token.Span.Lim <= cursorPos);

                var dottedNameNode = curNode.CastDottedName();
                var ident = dottedNameNode.Right;
                string identName = ident.Name;
                var leftNode = dottedNameNode.Left;
                var leftType = intellisenseData.Binding.GetType(leftNode);

                intellisenseData.BeforeAddSuggestionsForDottedNameNode(leftNode);

                var isOneColumnTable = leftType.IsColumn
                                        && leftNode.Kind == NodeKind.DottedName
                                        && leftType.Accepts(intellisenseData.Binding.GetType(((DottedNameNode)leftNode).Left));

                if (cursorPos < ident.Token.Span.Min)
                {
                    // Cursor position is before the identifier starts.
                    // i.e. "this.  |  Awards"
                    AddSuggestionsForLeftNodeScope(intellisenseData, leftNode, isOneColumnTable, leftType);
                }
                else if (cursorPos <= ident.Token.Span.Lim)
                {
                    // Cursor position is in the identifier.
                    // Suggest fields that don't need to be qualified.
                    // Identifiers can include open and close brackets and the Token.Span covers them.
                    // Get the matching string as a substring from the script so that the whitespace is preserved.
                    intellisenseData.SetMatchArea(ident.Token.Span.Min, cursorPos, ident.Token.Span.Lim - ident.Token.Span.Min);

                    if (!intellisenseData.Binding.ErrorContainer.HasErrors(dottedNameNode))
                    {
                        intellisenseData.BoundTo = identName;
                    }

                    AddSuggestionsForLeftNodeScope(intellisenseData, leftNode, isOneColumnTable, leftType);
                }
                else if (IntellisenseHelper.CanSuggestAfterValue(cursorPos, intellisenseData.Script))
                {
                    // Verify that cursor is after a space after the identifier.
                    // i.e. "this.Awards   |"
                    // Suggest binary operators.
                    IntellisenseHelper.AddSuggestionsForAfterValue(intellisenseData, intellisenseData.Binding.GetType(dottedNameNode));
                }

                return true;
            }

            private void AddSuggestionsForLeftNodeScope(IntellisenseData.IntellisenseData intellisenseData, TexlNode leftNode, bool isOneColumnTable, DType leftType)
            {
                Contracts.AssertValue(intellisenseData);
                Contracts.AssertValue(leftNode);
                Contracts.AssertValue(leftType);

                if (!intellisenseData.TryAddSuggestionsForLeftNodeScope(leftNode))
                {
                    if (TryGetEnumInfo(intellisenseData, leftNode, intellisenseData.Binding, out var enumInfo))
                    {
                        IntellisenseHelper.AddSuggestionsForEnum(intellisenseData, enumInfo);
                    }
                    else if (TryGetNamespaceFunctions(leftNode, intellisenseData.Binding, out var namespaceFunctions))
                    {
                        AddSuggestionsForNamespace(intellisenseData, namespaceFunctions);
                    }
                    else if (TryGetLocalScopeInfo(leftNode, intellisenseData.Binding, out var info))
                    {
                        IntellisenseHelper.AddTopLevelSuggestions(intellisenseData, info.Type);
                    }
                    else if (!isOneColumnTable)
                    {
                        AddSuggestionsForDottedName(intellisenseData, leftType);
                    }
                }

                intellisenseData.OnAddedSuggestionsForLeftNodeScope(leftNode);
            }

            internal void AddSuggestionsForNamespace(IntellisenseData.IntellisenseData intellisenseData, IEnumerable<TexlFunction> namespaceFunctions)
            {
                Contracts.AssertValue(intellisenseData);
                Contracts.AssertValue(namespaceFunctions);
                Contracts.AssertAllValues(namespaceFunctions);

                foreach (var function in namespaceFunctions)
                {
                    // Note we're using the unqualified name, since we're on the RHS of a "namespace." identifier.
                    IntellisenseHelper.AddSuggestion(intellisenseData, function.Name, SuggestionKind.Function, SuggestionIconKind.Function, function.ReturnType, requiresSuggestionEscaping: true);
                }
            }

            private static bool TryGetLocalScopeInfo(TexlNode node, TexlBinding binding, out ScopedNameLookupInfo info)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(binding);

                if (node.Kind == NodeKind.FirstName)
                {
                    var curNode = node.CastFirstName();
                    var firstNameInfo = binding.GetInfo(curNode);
                    if (firstNameInfo.Kind == BindKind.ScopeArgument)
                    {
                        info = (ScopedNameLookupInfo)firstNameInfo.Data;
                        return true;
                    }
                }

                info = new ScopedNameLookupInfo();
                return false;
            }

            private static bool TryGetEnumInfo(IntellisenseData.IntellisenseData data, TexlNode node, TexlBinding binding, out EnumSymbol enumSymbol)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(binding);

                var curNode = node.AsFirstName();
                if (curNode == null)
                {
                    enumSymbol = null;
                    return false;
                }

                var firstNameInfo = binding.GetInfo(curNode).VerifyValue();
                if (firstNameInfo.Kind != BindKind.Enum)
                {
                    enumSymbol = null;
                    return false;
                }

                return data.TryGetEnumSymbol(firstNameInfo.Name, binding, out enumSymbol);
            }

            private static bool TryGetNamespaceFunctions(TexlNode node, TexlBinding binding, out IEnumerable<TexlFunction> functions)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(binding);

                var curNode = node.AsFirstName();
                if (curNode == null)
                {
                    functions = EmptyEnumerator<TexlFunction>.Instance;
                    return false;
                }

                var firstNameInfo = binding.GetInfo(curNode).VerifyValue();
                Contracts.AssertValid(firstNameInfo.Name);

                var namespacePath = new DPath().Append(firstNameInfo.Name);
                functions = binding.NameResolver.LookupFunctionsInNamespace(namespacePath);

                return functions.Any();
            }

            // This method has logic to create Types for the TypedNames for a given type
            // if that type is Table.
            internal static void AddSuggestionsForDottedName(IntellisenseData.IntellisenseData intellisenseData, DType type)
            {
                Contracts.AssertValue(intellisenseData);
                Contracts.AssertValid(type);

                if (intellisenseData.TryAddCustomDottedNameSuggestions(type))
                {
                    return;
                }

                if (!type.IsTable)
                {
                    IntellisenseHelper.AddTopLevelSuggestions(intellisenseData, type);
                    return;
                }
            }
        }
    }
}
