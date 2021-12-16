// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Texl.Intellisense.IntellisenseData;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal delegate bool IsValidSuggestion(IntellisenseData.IntellisenseData intellisenseData, IntellisenseSuggestion suggestion);

    internal partial class Intellisense : IIntellisense
    {
        protected readonly IReadOnlyList<ISuggestionHandler> _suggestionHandlers;
        protected readonly PowerFxConfig _powerFxConfig;

        public Intellisense(PowerFxConfig powerFxConfig, IReadOnlyList<ISuggestionHandler> suggestionHandlers)
        {
            Contracts.AssertValue(suggestionHandlers);

            _powerFxConfig = powerFxConfig;
            _suggestionHandlers = suggestionHandlers;
        }

        public IIntellisenseResult Suggest(IntellisenseContext context, TexlBinding binding, Formula formula)
        {
            Contracts.CheckValue(context, "context");
            Contracts.CheckValue(binding, "binding");
            Contracts.CheckValue(formula, "formula");

            // TODO: Hoist scenario tracking out of language module.
            // Guid suggestScenarioGuid = Common.Telemetry.Log.Instance.StartScenario("IntellisenseSuggest");

            try
            {
                IntellisenseData.IntellisenseData intellisenseData;
                if (!TryInitializeIntellisenseContext(context, binding, formula, out intellisenseData))
                {
                    return new IntellisenseResult(new DefaultIntellisenseData(), new List<IntellisenseSuggestion>());
                }

                foreach (var handler in _suggestionHandlers)
                    if (handler.Run(intellisenseData))
                        break;

                return Finalize(context, intellisenseData);
            }
            catch (Exception ex)
            {
                // If there is any exception, we don't need to crash. Instead, Suggest() will simply 
                // return an empty result set along with exception for client use.
                return new IntellisenseResult(new DefaultIntellisenseData(), new List<IntellisenseSuggestion>(), ex);
            }
            // TODO: Hoist scenario tracking out of language module.
            // finally
            // {
            //     Common.Telemetry.Log.Instance.EndScenario(suggestScenarioGuid);
            // }
        }

        public static bool TryGetExpectedTypeForBinaryOp(TexlBinding binding, TexlNode curNode, int cursorPos, out DType expectedType)
        {
            // If we are in a binary operation context, the expected type is relative to the binary operation.
            if (curNode != null && curNode.Parent != null && curNode.Parent.Kind == NodeKind.BinaryOp)
            {
                BinaryOpNode binaryOpNode = curNode.Parent.CastBinaryOp();
                DType coercedType;
                TexlNode expectedNode = null;
                if (cursorPos < binaryOpNode.Token.Span.Min)
                    // Cursor is before the binary operator. Expected type is equal to the type of right side.
                    expectedNode = binaryOpNode.Right;
                else if (cursorPos > binaryOpNode.Token.Span.Lim)
                    // Cursor is after the binary operator. Expected type is equal to the type of left side.
                    expectedNode = binaryOpNode.Left;

                if (expectedNode != null)
                {
                    expectedType = binding.TryGetCoercedType(expectedNode, out coercedType) ? coercedType : binding.GetType(expectedNode);
                    return true;
                }
            }

            expectedType = DType.Error;
            return false;
        }

        public static bool FindCurFuncAndArgs(TexlNode curNode, int cursorPos, TexlBinding binding, out TexlFunction curFunc, out int argIndex, out int argCount, out DType expectedType)
        {
            Contracts.AssertValue(curNode);
            Contracts.AssertValue(binding);

            if (curNode.Kind == NodeKind.Call)
            {
                CallNode callNode = curNode.CastCall();
                if (callNode.Token.Span.Lim <= cursorPos && callNode.ParenClose != null && cursorPos <= callNode.ParenClose.Span.Min)
                {
                    CallInfo info = binding.GetInfo(callNode);

                    if (info.Function != null)
                    {
                        curFunc = info.Function;
                        argIndex = 0;
                        argCount = callNode.Args.Count;
                        expectedType = curFunc.ParamTypes.Length > 0 ? curFunc.ParamTypes[0] : DType.Error;

                        return true;
                    }
                }
            }

            if (IntellisenseHelper.TryGetInnerMostFunction(curNode, binding, out curFunc, out argIndex, out argCount))
            {
                expectedType = curFunc.ParamTypes.Length > argIndex ? curFunc.ParamTypes[argIndex] : DType.Error;
                return true;
            }

            expectedType = DType.Error;
            return false;
        }

        // Filters out all the types from the suggestions that are not accepted by the given type.
        public static void TypeFilter(DType type, string matchingStr, ref List<IntellisenseSuggestion> suggestions)
        {
            Contracts.Assert(type.IsValid);
            Contracts.AssertValue(suggestions);

            if (string.IsNullOrEmpty(matchingStr) && type != DType.Error && type != DType.Unknown)
            {
                // Determine a safe start for suggestions whose types match the specified type.
                // Non-zero sort priorities take precedence over type filtering.
                int j = 0;
                while (j < suggestions.Count && suggestions[j].SortPriority > 0)
                    j++;

                IntellisenseSuggestion temp;
                for (int i = j; i < suggestions.Count; i++)
                {
                    if (suggestions[i].Type.Equals(type))
                    {
                        int k = i;
                        temp = suggestions[k];
                        temp.SetTypematch();
                        while (k > j)
                        {
                            suggestions[k] = suggestions[k - 1];
                            k--;
                        }
                        suggestions[j++] = temp;
                    }
                }
            }
        }

        protected static bool TryGetFunctionCategory(string category, out FunctionCategories mask)
        {
            Contracts.AssertNonEmpty(category);

            foreach (var cat in Enum.GetValues(typeof(FunctionCategories)).Cast<FunctionCategories>())
            {
                if (category.Equals(cat.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mask = cat;
                    return true;
                }
            }

            mask = default(FunctionCategories);
            return false;
        }

        protected static void TypeMatchPriority(DType type, IList<IntellisenseSuggestion> suggestions)
        {
            Contracts.Assert(type.IsValid);
            Contracts.AssertValue(suggestions);

            // The string type is too nebulous to push all matching string values to the top of the suggestion list
            if (type == DType.Unknown || type == DType.Error || type == DType.String)
                return;

            foreach (var suggestion in suggestions)
                if (!suggestion.Type.IsUnknown && type.Accepts(suggestion.Type))
                    suggestion.SortPriority++;
        }

        private bool TryInitializeIntellisenseContext(IIntellisenseContext context, TexlBinding binding, Formula formula, out IntellisenseData.IntellisenseData data)
        {
            Contracts.AssertValue(context);

            TexlNode currentNode = TexlNode.FindNode(formula.ParseTree, context.CursorPosition);
            TexlFunction curFunc;
            int argIndex, argCount;
            DType expectedType;
            IsValidSuggestion isValidSuggestionFunc;

            GetFunctionAndTypeInformation(context, currentNode, binding, out curFunc, out argIndex, out argCount, out expectedType, out isValidSuggestionFunc);
            data = CreateData(context, expectedType, binding, curFunc, currentNode, argIndex, argCount, isValidSuggestionFunc, binding.GetExpandEntitiesMissingMetadata(), formula.Comments);
            return true;
        }

        protected internal virtual IntellisenseData.IntellisenseData CreateData(IIntellisenseContext context, DType expectedType, TexlBinding binding, TexlFunction curFunc, TexlNode curNode, int argIndex, int argCount, IsValidSuggestion isValidSuggestionFunc, IList<DType> missingTypes, List<CommentToken> comments)
        {
            return new IntellisenseData.IntellisenseData(_powerFxConfig, context, expectedType, binding, curFunc, curNode, argIndex, argCount, isValidSuggestionFunc, missingTypes, comments);
        }

        private void GetFunctionAndTypeInformation(IIntellisenseContext context, TexlNode curNode, TexlBinding binding, out TexlFunction curFunc, out int argIndex, out int argCount, out DType expectedType, out IsValidSuggestion isValidSuggestionFunc)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(curNode);
            Contracts.AssertValue(binding);

            if (!FindCurFuncAndArgs(curNode, context.CursorPosition, binding, out curFunc, out argIndex, out argCount, out expectedType))
            {
                curFunc = null;
                argIndex = 0;
                argCount = 0;
                expectedType = DType.Unknown;
            }

            DType binaryOpExpectedType;
            if (TryGetExpectedTypeForBinaryOp(binding, curNode, context.CursorPosition, out binaryOpExpectedType))
                expectedType = binaryOpExpectedType;

            if (curFunc != null)
                isValidSuggestionFunc = (intellisenseData, suggestion) => intellisenseData.CurFunc.IsSuggestionTypeValid(intellisenseData.ArgIndex, suggestion.Type);
            else
                isValidSuggestionFunc = Helper.DefaultIsValidSuggestionFunc;
        }

        private IIntellisenseResult Finalize(IIntellisenseContext context, IntellisenseData.IntellisenseData intellisenseData)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(intellisenseData);

            DType expectedType = intellisenseData.ExpectedType;

            TypeMatchPriority(expectedType, intellisenseData.Suggestions);
            TypeMatchPriority(expectedType, intellisenseData.SubstringSuggestions);
            intellisenseData.Suggestions.Sort();
            intellisenseData.SubstringSuggestions.Sort();
            List<IntellisenseSuggestion> resultSuggestions = intellisenseData.Suggestions.Distinct().ToList();
            IEnumerable<IntellisenseSuggestion> resultSubstringSuggestions = intellisenseData.SubstringSuggestions.Distinct();
            resultSuggestions.AddRange(resultSubstringSuggestions);

            TypeFilter(expectedType, intellisenseData.MatchingStr, ref resultSuggestions);

            foreach (var handler in intellisenseData.CleanupHandlers)
                handler.Run(context, intellisenseData, resultSuggestions);

            return new IntellisenseResult(intellisenseData, resultSuggestions);
        }
    }

    internal static class Helper
    {
        internal static bool DefaultIsValidSuggestionFunc(IntellisenseData.IntellisenseData intellisenseData, IntellisenseSuggestion suggestion)
        {
            //return intellisenseData.ExpectedType.Accepts(suggestion.Type);
            return true;
        }
    }
}
