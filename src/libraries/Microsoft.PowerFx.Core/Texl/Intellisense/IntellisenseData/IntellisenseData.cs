// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense.IntellisenseData
{
    // The IntellisenseData class contains the pre-parsed data for Intellisense to provide suggestions
    internal class IntellisenseData : IIntellisenseData
    {
        private readonly PowerFxConfig _powerFxConfig;
        private readonly IEnumStore _enumStore;

        public IntellisenseData(PowerFxConfig powerFxConfig, IEnumStore enumStore, IIntellisenseContext context, DType expectedType, TexlBinding binding, TexlFunction curFunc, TexlNode curNode, int argIndex, int argCount, IsValidSuggestion isValidSuggestionFunc, IList<DType> missingTypes, List<CommentToken> comments)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValid(expectedType);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(curNode);
            Contracts.Assert(context.CursorPosition >= 0 && context.CursorPosition <= context.InputText.Length);
            Contracts.AssertValue(isValidSuggestionFunc);
            Contracts.AssertValueOrNull(missingTypes);
            Contracts.AssertValueOrNull(comments);

            _powerFxConfig = powerFxConfig;
            _enumStore = enumStore;
            ExpectedType = expectedType;
            Suggestions = new IntellisenseSuggestionList();
            SubstringSuggestions = new IntellisenseSuggestionList();
            Binding = binding;
            Comments = comments;
            CurFunc = curFunc;
            CurNode = curNode;
            Script = context.InputText;
            CursorPos = context.CursorPosition;
            ArgIndex = argIndex;
            ArgCount = argCount;
            IsValidSuggestionFunc = isValidSuggestionFunc;
            MatchingStr = string.Empty;
            MatchingLength = 0;
            ReplacementStartIndex = context.CursorPosition;
            MissingTypes = missingTypes;
            BoundTo = string.Empty;
            CleanupHandlers = new List<ISpecialCaseHandler>();
        }

        internal DType ExpectedType { get; }

        internal IntellisenseSuggestionList Suggestions { get; }

        internal IntellisenseSuggestionList SubstringSuggestions { get; }

        internal TexlBinding Binding { get; }

        internal List<CommentToken> Comments { get; }

        public TexlFunction CurFunc { get; }

        internal TexlNode CurNode { get; }

        public string Script { get; }

        internal int CursorPos { get; }

        public int ArgIndex { get; }

        public int ArgCount { get; }

        internal IsValidSuggestion IsValidSuggestionFunc { get; }

        internal string MatchingStr { get; private set; }

        internal int MatchingLength { get; private set; }

        public int ReplacementStartIndex { get; private set; }

        public int ReplacementLength { get; private set; }

        internal string BoundTo { get; set; }

        internal IList<DType> MissingTypes { get; }

        internal List<ISpecialCaseHandler> CleanupHandlers { get; }

        /// <summary>
        /// Type that defines valid symbols in the formula.
        /// </summary>
        internal virtual DType ContextScope => Binding.ContextScope;

        /// <summary>
        /// Returns true if <paramref name="suggestion"/> should be added to the suggestion list based on
        /// <paramref name="type"/> and false otherwise.  May be used after suggestions and node type are found.
        /// Note: The default behavior has it so that all candidates are suggestible.  This may not always be
        /// desired.
        /// </summary>
        /// <param name="suggestion">
        /// Candidate suggestion string.
        /// </param>
        /// <param name="type">
        /// Type of the node at the caller's context.
        /// </param>
        /// <returns>
        /// Whether the provided candidate suggestion is valid per the provided type.
        /// </returns>
        internal virtual bool DetermineSuggestibility(string suggestion, DType type) => true;

        /// <summary>
        /// This method is executed by <see cref="Intellisense"/> when it is run for a formula whose cursor
        /// is positioned to the right of a <see cref="DottedNameNode"/>, but not after the right node of the
        /// <see cref="DottedNameNode"/>.  If it returns true, further calculations will cease and the
        /// Intellisense handler will complete.  If it returns false, then fallback suggestion calculations will be run.
        /// </summary>
        /// <param name="leftNode">
        /// Left node in the <see cref="DottedNameNode"/> at which the cursor is pointed at the time of
        /// invocation.
        /// </param>
        /// <returns>
        /// True iff a suggestion was added, false if no suggestion was added (default suggestion behavior will
        /// run).
        /// </returns>
        internal virtual bool TryAddSuggestionsForLeftNodeScope(TexlNode leftNode) => false;

        /// <summary>
        /// This method is executed by <see cref="Intellisense"/> when it is run for a formula whose cursor
        /// is positioned to the right of a <see cref="DottedNameNode"/>, but not after the right node of the
        /// <see cref="DottedNameNode"/>.  It is run after all suggestions have been added for
        /// <paramref name="node"/> and may be used to add additional suggestions after the rest.  This method
        /// does not alter control flow.
        /// </summary>
        /// <param name="node">
        /// Node for which suggestions may be added or other actions committed.
        /// </param>
        internal virtual void OnAddedSuggestionsForLeftNodeScope(TexlNode node)
        {
        }

        /// <summary>
        /// Should return true if name collides with existing symbols and false otherwise.  Used to determine
        /// whether to prepend a prefix to an enum value.
        /// </summary>
        /// <param name="name">
        /// Name in question.
        /// </param>
        /// <returns>
        /// True if the provided name collides with an existing name or identifier, false otherwise.
        /// </returns>
        internal virtual bool DoesNameCollide(string name)
        {
            return (from enumSymbol in _enumStore.EnumSymbols
                    where (from localizedEnum in enumSymbol.LocalizedEnumValues where localizedEnum == name select localizedEnum).Any()
                    select enumSymbol).Count() > 1;
        }

        /// <summary>
        /// Should unqualified enums be suggested.
        /// </summary>
        internal virtual bool SuggestUnqualifiedEnums => true;

        /// <summary>
        /// Retrieves an <see cref="EnumSymbol"/> from <paramref name="binding"/> (if necessary).
        /// </summary>
        /// <param name="name">
        /// Name of the enum symbol for which to look.
        /// </param>
        /// <param name="binding">
        /// Binding in which may be looked for the enum symbol.
        /// </param>
        /// <param name="enumSymbol">
        /// Should be set to the symbol for <paramref name="name"/> if it is found, and left null otherwise.
        /// </param>
        /// <returns>
        /// True if the enum symbol was found, false otherwise.
        /// </returns>
        internal virtual bool TryGetEnumSymbol(string name, TexlBinding binding, out EnumSymbol enumSymbol) =>
            TryGetEnumSymbol(name, out enumSymbol);

        private bool TryGetEnumSymbol(string name, out EnumSymbol symbol)
        {
            Contracts.AssertValue(name);

            symbol = EnumSymbols.Where(symbol => symbol.Name == name).FirstOrDefault();
            return symbol != null;
        }

        /// <summary>
        /// A list of the enum symbols defined for intellisense.
        /// </summary>
        internal virtual IEnumerable<EnumSymbol> EnumSymbols => _enumStore.EnumSymbols;

        /// <summary>
        /// Tries to add custom suggestions for a column specified by <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type of the column for which suggestions may be added.
        /// </param>
        /// <returns>
        /// True if suggestions were added and default column suggestion behavior should not be executed,
        /// false otherwise.
        /// </returns>
        internal virtual bool TryAddCustomColumnTypeSuggestions(DType type) => false;

        /// <summary>
        /// This method is executed by <see cref="Intellisense"/> when it is run for a formula whose cursor
        /// is positioned to the right of a <see cref="DottedNameNode"/>.
        /// Tries to add custom dotten name suggestions by a provided type for the left node to the
        /// <see cref="DottedNameNode"/>.
        /// </summary>
        /// <param name="type">
        /// Type of the lhs of a <see cref="DottedNameNode"/> for which suggestions may be added to
        /// this.
        /// </param>
        /// <returns>
        /// True if operation was successful and default suggestion behavior should be short circuited and
        /// false otherwise.
        /// </returns>
        internal virtual bool TryAddCustomDottedNameSuggestions(DType type) => false;

        /// <summary>
        /// This method is executed by <see cref="Intellisense"/> when it is run for a formula whose cursor
        /// is positioned to the right of a <see cref="DottedNameNode"/>, regardless as to whether the
        /// position is before, amidst, or after the left node of the <see cref="DottedNameNode"/>.  It is run
        /// before any suggestions have been added and is not intended to change control flow.
        /// </summary>
        /// <param name="node">
        /// Left node in the <see cref="DottedNameNode"/> at which the cursor is pointed at the time of
        /// invocation.
        /// </param>
        internal virtual void BeforeAddSuggestionsForDottedNameNode(TexlNode node)
        {
        }

        /// <summary>
        /// This method is called by <see cref="Intellisense"/> to determine whether a candidate suggestion
        /// that represents a function should be suggested.
        /// </summary>
        /// <param name="function">
        /// Candidate suggestion wherein the key represents the suggestion name and the value represents its
        /// type.
        /// </param>
        /// <returns>
        /// True if the function may be suggested, false otherwise.
        /// </returns>
        internal virtual bool ShouldSuggestFunction(TexlFunction function) => true;

        /// <summary>
        /// Returns a list of argument suggestions for a given function, scope type, and argument index.
        /// </summary>
        /// <param name="function">
        /// The function for which we are producing argument suggestions.
        /// </param>
        /// <param name="scopeType">
        /// The type of the scope from where intellisense is run.
        /// </param>
        /// <param name="argumentIndex">
        /// The index of the current argument of <paramref name="function"/>.
        /// </param>
        /// <param name="argsSoFar">
        /// The arguments that are present in the formula at the time of invocation.
        /// </param>
        /// <param name="requiresSuggestionEscaping">
        /// Is set to whether the characters within the returned suggestion need have its characters escaped.
        /// </param>
        /// <returns>
        /// Argument suggestions for the provided context.
        /// </returns>
        internal virtual IEnumerable<KeyValuePair<string, DType>> GetArgumentSuggestions(TexlFunction function, DType scopeType, int argumentIndex, TexlNode[] argsSoFar, out bool requiresSuggestionEscaping)
        {
            Contracts.AssertValue(function);
            Contracts.AssertValue(scopeType);

            return ArgumentSuggestions.GetArgumentSuggestions(TryGetEnumSymbol, SuggestUnqualifiedEnums, function, scopeType, argumentIndex, out requiresSuggestionEscaping);
        }

        /// <summary>
        /// Should return the kind of suggestion that may be recomended for the
        /// <paramref name="argumentIndex"/> parameter of <paramref name="function"/>.
        /// </summary>
        /// <param name="function">
        /// Function that the kind of suggestion for which this function determines.
        /// </param>
        /// <param name="argumentIndex">
        /// The index of the argument to which the suggestion pertains.
        /// </param>
        /// <returns>
        /// The suggestion kind for the hypothetical suggestion.
        /// </returns>
        internal virtual SuggestionKind GetFunctionSuggestionKind(TexlFunction function, int argumentIndex) => SuggestionKind.Global;

        /// <summary>
        /// This method is called after all default suggestions for value possibilities have been run and may be
        /// overridden to provide custom suggestions.
        /// </summary>
        internal virtual void AddCustomSuggestionsForValuePossibilities()
        {
        }

        /// <summary>
        /// May be overridden to provide custom suggestions at the point in intellisense runtime when
        /// suggestions for global identifiers are added.
        /// </summary>
        internal virtual void AddCustomSuggestionsForGlobals()
        {
            foreach (var global in _powerFxConfig.GetSymbols())
            {
                IntellisenseHelper.AddSuggestion(this, _powerFxConfig.GetSuggestableSymbolName(global), SuggestionKind.Global, SuggestionIconKind.Other, global.Type, requiresSuggestionEscaping: true);
            }
        }

        /// <summary>
        /// May be overridden to provide custom suggestions or change other state after the point in
        /// intellisense runtime where suggestions for global identifiers are added.
        /// </summary>
        internal virtual void AfterAddSuggestionsForGlobals()
        {
        }

        /// <summary>
        /// May be overridden to provide custom suggestions or change other state after the point in
        /// intellisense runtime where suggestions for unary operator keywords are added.
        /// </summary>
        internal virtual void AfterAddSuggestionsForUnaryOperatorKeywords()
        {
        }

        /// <summary>
        /// This collection is appended to the resultant suggestion list when
        /// <see cref="Intellisense.FirstNameNodeSuggestionHandler"/> is used.  It may be overridden to provide
        /// additional first name node suggestions.  It is called when the cursor is.
        /// </summary>
        /// <returns>
        /// Sequence of suggestions for first name node context.
        /// </returns>
        internal virtual IEnumerable<string> SuggestableFirstNames => _powerFxConfig.GetSymbols().Select(_powerFxConfig.GetSuggestableSymbolName);

        /// <summary>
        /// Invokes <see cref="AddSuggestionsForConstantKeywords"/> to supply suggestions for constant
        /// keywords.  May be overridden to supply additional suggestions or to change the set of acceptable
        /// keywords.
        /// </summary>
        public virtual void AddSuggestionsForConstantKeywords() =>
            IntellisenseHelper.AddSuggestionsForMatches(
                this,
                TexlLexer.LocalizedInstance.GetConstantKeywords(false),
                SuggestionKind.KeyWord,
                SuggestionIconKind.Other,
                requiresSuggestionEscaping: false);

        /// <summary>
        /// List of additional variable suggestions that may be provided in an overridden method.
        /// Here, the key is the suggestion text and the value is the kind of desired icon.
        /// </summary>
        internal virtual IEnumerable<KeyValuePair<string, SuggestionIconKind>> AdditionalGlobalSuggestions => Enumerable.Empty<KeyValuePair<string, SuggestionIconKind>>();

        /// <summary>
        /// This method may be overriden to add additional suggestions for local selections to the resultant
        /// suggestion list for first name nodes.
        /// </summary>
        internal virtual void AddAdditionalSuggestionsForLocalSymbols()
        {
        }

        /// <summary>
        /// This method may be overriden to add additional suggestions for generic selections to the resultant
        /// suggestion list for first name nodes.
        /// </summary>
        /// <param name="currentNode">
        /// The node for which Intellisense is invoked.
        /// </param>
        internal virtual void AddAdditionalSuggestionsForKeywordSymbols(TexlNode currentNode)
        {
        }

        /// <param name="function">
        /// Function whose eligibility is called into question.
        /// </param>
        /// <returns>
        /// Returns true if <see cref="Intellisense.FunctionRecordNameSuggestionHandler"/> should make suggestions
        /// for the provided function and false otherwise.
        /// </returns>
        internal virtual bool IsFunctionElligibleForRecordSuggestions(TexlFunction function) => true;

        /// <param name="function">
        /// Function in question.
        /// </param>
        /// <param name="callNode">
        /// The node at the present cursor position.
        /// </param>
        /// <param name="type">
        /// If overridden, may be set to a custom function type when returns.
        /// </param>
        /// <returns>
        /// True if a special type was found and type is set, false otherwise.
        /// </returns>
        internal virtual bool TryGetSpecialFunctionType(TexlFunction function, CallNode callNode, out DType type)
        {
            type = null;
            return false;
        }

        /// <summary>
        /// This method may be overridden to provide additional suggestions for function record names after
        /// the default have been added.  It should return true if intellisenseData is handled and no more
        /// suggestions are to be found and false otherwise.
        /// </summary>
        internal virtual bool TryAddFunctionRecordSuggestions(TexlFunction function, CallNode callNode, Identifier columnName) => false;

        /// <summary>
        /// This method is called by <see cref="Intellisense.ErrorNodeSuggestionHandlerBase"/> if function was
        /// discovered as a parent node to the current error node.  It may be overridden to add additional
        /// suggestions pertaining to <paramref name="function"/> and <paramref name="argIndex"/>.  If it returns true,
        /// <see cref="Intellisense.ErrorNodeSuggestionHandlerBase"/> will return immediately and no more suggestions
        /// will be added.
        /// </summary>
        /// <param name="function">
        /// Function for which additional suggestions may be added.
        /// </param>
        /// <param name="argIndex">
        /// Index of the argument on which the cursor is positioned.
        /// </param>
        /// <returns>
        /// True if all suggestions have been added and no more should be.  False otherwise.
        /// </returns>
        internal virtual bool TryAddCustomFunctionSuggestionsForErrorNode(TexlFunction function, int argIndex) => false;

        /// <summary>
        /// This method is called by <see cref="Intellisense.ErrorNodeSuggestionHandlerBase"/> before top level
        /// suggestions are added.  See <see cref="IntellisenseHelper.AddSuggestionsForTopLevel"/> for
        /// details.
        /// </summary>
        internal virtual bool AddSuggestionsBeforeTopLevelErrorNodeSuggestions() => false;

        /// <summary>
        /// This method is called by <see cref="Intellisense.ErrorNodeSuggestionHandlerBase"/> if no top level
        /// suggestions are added.  It may be overridden to supply alternative top level suggestions.
        /// </summary>
        internal virtual void AddAlternativeTopLevelSuggestionsForErrorNode()
        {
        }

        /// <summary>
        /// This method is called by <see cref="Intellisense.ErrorNodeSuggestionHandlerBase"/> after it has added all
        /// its suggestions to <see cref="Suggestions"/>.
        /// </summary>
        internal virtual void AddSuggestionsAfterTopLevelErrorNodeSuggestions()
        {
            AddCustomSuggestionsForGlobals();
        }

        public virtual bool TryAugmentSignature(TexlFunction func, int argIndex, string paramName, int highlightStart, out int newHighlightStart, out int newHighlightEnd, out string newParamName, out string newInvariantParamName) =>
            DefaultIntellisenseData.DefaultTryAugmentSignature(func, argIndex, paramName, highlightStart, out newHighlightStart, out newHighlightEnd, out newParamName, out newInvariantParamName);

        public virtual string GenerateParameterDescriptionSuffix(TexlFunction function, string paramName) =>
            DefaultIntellisenseData.GenerateDefaultParameterDescriptionSuffix(function, paramName);

        internal bool SetMatchArea(int startIndex, int endIndex, int replacementLength = -1)
        {
            Contracts.Assert(startIndex >= 0 && startIndex <= endIndex && endIndex <= Script.Length);

            // If we have already provided suggestions, we can't set the match area
            if (Suggestions.Count > 0 || SubstringSuggestions.Count > 0)
            {
                return false;
            }

            // Trim leading whitespace as there is no point to matching it
            while (startIndex < endIndex && string.IsNullOrWhiteSpace(Script.Substring(startIndex, 1)))
            {
                startIndex++;
            }

            ReplacementStartIndex = startIndex;
            MatchingLength = endIndex - startIndex;
            ReplacementLength = replacementLength < 0 ? MatchingLength : replacementLength;
            MatchingStr = TexlLexer.UnescapeName(Script.Substring(startIndex, MatchingLength));

            return true;
        }
    }
}
