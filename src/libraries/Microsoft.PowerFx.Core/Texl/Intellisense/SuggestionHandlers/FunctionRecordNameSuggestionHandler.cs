// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class FunctionRecordNameSuggestionHandler : ISuggestionHandler
        {
            /// <summary>
            /// Adds suggestions as appropriate to the internal Suggestions and SubstringSuggestions lists of intellisenseData.
            /// Returns true if intellisenseData is handled and no more suggestions are to be found and false otherwise.
            /// </summary>
            public bool Run(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                if (!TryGetRecordNodeWithinACallNode(intellisenseData.CurNode, out var recordNode, out var callNode))
                {
                    return false;
                }

                // For the special case of an identifier of a record which is an argument of a function, we can
                // utilize the data provided to suggest relevant column names
                var cursorPos = intellisenseData.CursorPos;

                var suggestionsAdded = false;
                Contracts.AssertValue(recordNode);
                Contracts.AssertValue(callNode);

                var columnName = GetRecordIdentifierForCursorPosition(cursorPos, recordNode, intellisenseData.Script);
                if (columnName != null && columnName.Token.Span.Min <= cursorPos)
                {
                    var tokenSpan = columnName.Token.Span;
                    var replacementLength = tokenSpan.Min == cursorPos ? 0 : tokenSpan.Lim - tokenSpan.Min;
                    intellisenseData.SetMatchArea(tokenSpan.Min, cursorPos, replacementLength);
                }

                var info = intellisenseData.Binding.GetInfo(callNode);
                var func = info.Function;
                if (func == null || !intellisenseData.IsFunctionElligibleForRecordSuggestions(func))
                {
                    return false;
                }

                // Adding suggestions for callNode arguments which reference a collection's columns
                if (func.CanSuggestInputColumns)
                {
                    var aggregateType = GetAggregateType(func, callNode, intellisenseData);
                    if (aggregateType.IsError || !aggregateType.IsAggregate)
                    {
                        return false;
                    }

                    if (aggregateType.ContainsDataEntityType(DPath.Root))
                    {
                        var error = false;
                        aggregateType = aggregateType.DropAllOfTableRelationships(ref error, DPath.Root);
                        if (error)
                        {
                            return false;
                        }
                    }

                    suggestionsAdded = AddSuggestionForAggregateAndParentRecord(recordNode, aggregateType, intellisenseData);

                    return suggestionsAdded && columnName != null;
                }

                return intellisenseData.TryAddFunctionRecordSuggestions(func, callNode, columnName);
            }

            /// <summary>
            /// Adds suggestions for the field type obtained from parent record <paramref name="recordNode"/> if found.
            /// else adds fields suggestion from <paramref name="aggregateType"/>.
            /// </summary>
            /// <param name="recordNode">Parent Texl node that maybe RecordNode.</param>
            /// <param name="aggregateType">Aggregate type that will be added, if the field type for <paramref name="recordNode"/> is not found in <paramref name="aggregateType"/>. </param>
            /// <param name="intellisenseData"></param>
            /// <returns></returns>
            internal static bool AddSuggestionForAggregateAndParentRecord(TexlNode recordNode, DType aggregateType, IntellisenseData.IntellisenseData intellisenseData)
            {
                if (recordNode.Token.Kind != TokKind.CurlyOpen)
                {
                    return false;
                }

                if (TryGetParentRecordFieldType(aggregateType, recordNode, out var lastFieldType))
                {
                    aggregateType = lastFieldType;
                }
                else if (recordNode?.Parent?.Kind == NodeKind.Record &&
                    recordNode?.Parent?.Parent?.Kind != NodeKind.Table)
                {
                    // If Parent not is record node, that means it was nested field and above method should have found type, if it did not, return false. unless it was [{<cursor position>.
                    return false;
                }

                return AddAggregateSuggestions(aggregateType, intellisenseData, intellisenseData.CursorPos);
            }

            /// <summary>
            /// Recursively finds field type of parent record node's last field.
            /// e.g. *[field1: {field2: { field3: "test", field4: currentNode}}] => field4's type is returned.
            /// </summary>
            private static bool TryGetParentRecordFieldType(DType aggregateType, TexlNode currentNode, out DType fieldType)
            {
                if (currentNode == null || aggregateType == null)
                {
                    fieldType = default;
                    return false;
                }

                if (TryGetParentRecordNode(currentNode, out var parentRecord))
                {
                    var fieldName = parentRecord.Ids.LastOrDefault()?.Name;
                    if (fieldName.HasValue &&
                        aggregateType.TryGetType(fieldName.Value, out var type) &&
                        (type.IsRecord || currentNode.Parent?.Kind == NodeKind.Table))
                    {
                        fieldType = type;
                        return true;
                    }

                    if (TryGetParentRecordFieldType(aggregateType, parentRecord, out var parentFieldType))
                    {
                        return parentFieldType.TryGetType(fieldName.Value, out fieldType);
                    }
                }

                fieldType = default;
                return false;
            }

            internal static bool AddAggregateSuggestions(DType aggregateType, IntellisenseData.IntellisenseData intellisenseData, int cursorPos)
            {
                bool suggestionsAdded = false;                
                RecordNode parentRecordNode = intellisenseData.CurNode.Parent as RecordNode;

                List<DName> validFields = new List<DName>();
                string errorField = null;

                for (int i = 0; i < parentRecordNode.Ids.Count; i++)
                {
                    TexlNode child = parentRecordNode.Children[i];
                    Identifier id = parentRecordNode.Ids[i];                    

                    if (child.Kind == NodeKind.Error)
                    {
                        // if we have "aa:" in the ErrorNode, don't enumerate the record as the colon is present
                        if (parentRecordNode.Colons.Length == parentRecordNode.Ids.Count)
                        {
                            return false;
                        }

                        // Id defaults to '_' when non-existent
                        errorField = id.Span.Min == id.Span.Lim ? string.Empty : id.Name.Value;
                    }
                    else
                    {
                        validFields.Add(id.Name);
                    }
                }
               
                // exclude fields that are already used in the record
                IEnumerable<TypedName> aggregateNamesNotAlreadyUsed = aggregateType.GetNames(DPath.Root).Where(param => !param.Type.IsError && !validFields.Contains(param.Name));
                
                // if there is an errorField, filter on fields starting with that beginning
                // case insensitive as it's better for the makers
                if (!string.IsNullOrEmpty(errorField))
                {
                    aggregateNamesNotAlreadyUsed = aggregateNamesNotAlreadyUsed.Where(field => field.Name.Value.StartsWith(errorField, StringComparison.OrdinalIgnoreCase));
                }
                
                foreach (TypedName tName in aggregateNamesNotAlreadyUsed)
                {
                    DName usedName = tName.Name;

                    // use display name if available
                    if (DType.TryGetDisplayNameForColumn(aggregateType, usedName, out var maybeDisplayName))
                    {
                        usedName = new DName(maybeDisplayName);
                    }

                    var suggestion = TexlLexer.EscapeName(usedName.Value) + (IntellisenseHelper.IsPunctuatorColonNextToCursor(cursorPos, intellisenseData.Script) ? string.Empty : TexlLexer.PunctuatorColon);
                    suggestionsAdded |= IntellisenseHelper.AddSuggestion(intellisenseData, suggestion, SuggestionKind.Field, SuggestionIconKind.Other, DType.String, requiresSuggestionEscaping: false);
                }

                return suggestionsAdded;
            }

            internal static DType GetAggregateType(TexlFunction func, CallNode callNode, IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(func);
                Contracts.AssertValue(callNode);
                Contracts.AssertValue(intellisenseData);

                if (func == null || callNode == null || intellisenseData == null)
                {
                    return DType.Error;
                }

                if (intellisenseData.TryGetSpecialFunctionType(func, callNode, out var type))
                {
                    return type;
                }
                else if (func.TryGetTypeForArgSuggestionAt(intellisenseData.ArgIndex, out type))
                {
                    return type;
                }
                else if (callNode.Args?.Count > 0)
                {
                    type = intellisenseData.Binding.GetType(callNode.Args.Children[0]);
                    if (type.IsTableNonObjNull)
                    {
                        return type.ToRecord();
                    }
                    else if (type.IsRecord)
                    {
                        return type;
                    }
                }

                return DType.Unknown;
            }

            private static bool TryGetParentRecordNode(TexlNode node, out RecordNode recordNode)
            {
                Contracts.AssertValue(node);

                var parentNode = node.Parent;
                while (parentNode != null)
                {
                    if (parentNode.Kind == NodeKind.Record)
                    {
                        recordNode = parentNode.AsRecord();
                        return true;
                    }

                    parentNode = parentNode.Parent;
                }

                recordNode = null;
                return false;
            }

            private static bool TryGetParentListNode(RecordNode node, out ListNode listNode)
            {
                Contracts.AssertValue(node);

                TexlNode parentNode = node;
                while (parentNode != null)
                {
                    if (parentNode.Kind == NodeKind.List)
                    {
                        listNode = parentNode.AsList();
                        return true;
                    }

                    parentNode = parentNode.Parent;
                }

                listNode = null;
                return false;
            }

            // 1.
            // <CallNode>
            //    |
            //  <ListNode>
            //    |
            //  <Error RecordNode>[Cursor position]

            // For example, Patch(Accounts, OldRecord, UpdateRecord, {<Cursor position>

            // 2.
            // <CallNode>
            //    |
            //  <ListNode>
            //    |
            //  <RecordNode>
            //    |
            //  <Error Node>[Cursor position]

            // For example, Patch(Accounts, OldRecord, UpdateRecord, {A:"",<cursor position>})

            // 3.
            // <CallNode>
            //    |
            //  <ListNode>
            //    |
            //  <VariadicOpNode>
            //    |
            //  <RecordNode>
            //    |
            //  <Error Identifier Node> [Cursor position]
            // For example, Patch(Accounts, OldRecord, UpdateRecord, { 'Account Name': ""});
            //              Patch(Accounts, OldRecord, UpdateRecord,{<Cursor position>);
            private static bool TryGetRecordNodeWithinACallNode(TexlNode node, out RecordNode recordNode, out CallNode callNode)
            {
                Contracts.AssertValue(node);

                if (!TryGetParentRecordNode(node, out recordNode) || !TryGetParentListNode(recordNode, out var listNode))
                {
                    callNode = null;
                    return false;
                }

                if (!(listNode.Parent is CallNode cNode))
                {
                    callNode = null;
                    return false;
                }

                callNode = cNode;
                return true;
            }

            private static Identifier GetRecordIdentifierForCursorPosition(int cursorPos, RecordNode parent, string script)
            {
                Contracts.Assert(cursorPos >= 0);
                Contracts.Assert(cursorPos <= script.Length);
                Contracts.AssertValue(parent);

                if (cursorPos > script.Length)
                {
                    return null;
                }

                foreach (var identifier in parent.Ids)
                {
                    // Handle special case for whitespaces in formula.
                    if (cursorPos < identifier.Token.Span.Min && string.IsNullOrWhiteSpace(script.Substring(cursorPos, identifier.Token.Span.Min - cursorPos)))
                    {
                        return identifier;
                    }

                    if (identifier.Token.Span.Min <= cursorPos && cursorPos <= identifier.Token.Span.Lim)
                    {
                        return identifier;
                    }
                }

                return null;
            }
        }
    }
}
