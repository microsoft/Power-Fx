﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding.BindInfo;
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
                if (columnName == null)
                {
                    return false;
                }

                if (columnName.Token.Span.Min <= cursorPos)
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

                    var parentRecord = recordNode.Parent?.AsRecord(); 

                    if (parentRecord != null)
                    {
                        var fieldName = parentRecord.Ids.FirstOrDefault()?.Name;

                        aggregateType = fieldName.HasValue && aggregateType.TryGetType(fieldName.Value, out var type) 
                            ? type 
                            : aggregateType;
                    }

                    foreach (var tName in aggregateType.GetNames(DPath.Root).Where(param => !param.Type.IsError))
                    {
                        var usedName = tName.Name;
                        if (DType.TryGetDisplayNameForColumn(aggregateType, usedName, out var maybeDisplayName))
                        {
                            usedName = new DName(maybeDisplayName);
                        }

                        var suggestion = TexlLexer.EscapeName(usedName.Value) + (IntellisenseHelper.IsPunctuatorColonNextToCursor(cursorPos, intellisenseData.Script) ? string.Empty : TexlLexer.PunctuatorColon);
                        suggestionsAdded |= IntellisenseHelper.AddSuggestion(intellisenseData, suggestion, SuggestionKind.Field, SuggestionIconKind.Other, DType.String, requiresSuggestionEscaping: false);
                    }

                    return suggestionsAdded && columnName != null;
                }

                return intellisenseData.TryAddFunctionRecordSuggestions(func, callNode, columnName);
            }

            private DType GetFieldTypeFromAggregate(DType aggregateType, string fieldName)
            {
                if (fieldName == null || aggregateType == null)
                {
                    return null;
                }

                // BFS to find the field's type.
                Queue<DType> queue = new Queue<DType>();
                queue.Enqueue(aggregateType);

                while (queue.Count > 0)
                {
                    var currentType = queue.Dequeue();
                    var fields = currentType.GetAllNames(DPath.Root);

                    foreach (var field in fields)
                    {
                        if (field.Name == fieldName)
                        {
                            return field.Type; // Return the corresponding DType
                        }

                        queue.Enqueue(field.Type);  // Enqueue the nested type for exploration
                    }
                }

                return null; // Field not found.
            }

            private DType GetAggregateType(TexlFunction func, CallNode callNode, IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(func);
                Contracts.AssertValue(callNode);
                Contracts.AssertValue(intellisenseData);

                if (intellisenseData.TryGetSpecialFunctionType(func, callNode, out var type))
                {
                    return type;
                }
                else if (func.IsInputColumnSuggestionArg0)
                {
                    return intellisenseData.Binding.GetType(callNode.Args.Children[0]);
                }
                else
                {
                    var argIndex = intellisenseData.ArgIndex;
                    var maxArgIndex = (func?.ParamTypes?.Count() ?? 0) - 1;

                    if (argIndex <= maxArgIndex)
                    {
                        return func.ParamTypes.ElementAt(argIndex);
                    }
                }

                return DType.Error;
            }

            private static bool TryGetParentRecordNode(TexlNode node, out RecordNode recordNode)
            {
                Contracts.AssertValue(node);

                var parentNode = node;
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
