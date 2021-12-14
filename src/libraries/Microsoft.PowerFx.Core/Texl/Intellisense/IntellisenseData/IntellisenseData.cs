// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense.IntellisenseData
{
    // The IntellisenseData class contains the pre-parsed data for Intellisense to provide suggestions
    internal class IntellisenseData : IIntellisenseData
    {
        private readonly PowerFxConfig _powerFxConfig;
        private readonly DType _expectedType;
        private readonly IntellisenseSuggestionList _suggestions;
        private readonly IntellisenseSuggestionList _substringSuggestions;
        private readonly TexlBinding _binding;
        private readonly List<CommentToken> _comments;
        private readonly TexlFunction _curFunc;
        private readonly TexlNode _curNode;
        private readonly string _script;
        private readonly int _cursorPos;
        private readonly int _argIndex;
        private readonly int _argCount;
        private IsValidSuggestion _isValidSuggestionFunc;
        private string _matchingStr;
        // _matchingLength will be different from the length of _matchingStr when _matchingStr contains delimiters.
        // For matching purposes we escape the delimeters and match against the internal DName.
        private int _matchingLength;
        private int _replacementStartIndex;
        // There will be a separate replacement length when we want to replace an entire node and not just the
        // preceding portion which is used for matching.
        private int _replacementLength;
        private IList<DType> _missingTypes;
        private readonly List<ISpecialCaseHandler> _cleanupHandlers;

        public IntellisenseData(PowerFxConfig powerFxConfig, IIntellisenseContext context, DType expectedType, TexlBinding binding, TexlFunction curFunc, TexlNode curNode, int argIndex, int argCount, IsValidSuggestion isValidSuggestionFunc, IList<DType> missingTypes, List<CommentToken> comments)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValid(expectedType);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(curNode);
            Contracts.Assert(0 <= context.CursorPosition && context.CursorPosition <= context.InputText.Length);
            Contracts.AssertValue(isValidSuggestionFunc);
            Contracts.AssertValueOrNull(missingTypes);
            Contracts.AssertValueOrNull(comments);

            _powerFxConfig = powerFxConfig;
            _expectedType = expectedType;
            _suggestions = new IntellisenseSuggestionList();
            _substringSuggestions = new IntellisenseSuggestionList();
            _binding = binding;
            _comments = comments;
            _curFunc = curFunc;
            _curNode = curNode;
            _script = context.InputText;
            _cursorPos = context.CursorPosition;
            _argIndex = argIndex;
            _argCount = argCount;
            _isValidSuggestionFunc = isValidSuggestionFunc;
            _matchingStr = string.Empty;
            _matchingLength = 0;
            _replacementStartIndex = context.CursorPosition;
            _missingTypes = missingTypes;
            BoundTo = string.Empty;
            _cleanupHandlers = new List<ISpecialCaseHandler>();
        }

        internal DType ExpectedType { get { return _expectedType; } }

        internal IntellisenseSuggestionList Suggestions { get { return _suggestions; } }

        internal IntellisenseSuggestionList SubstringSuggestions { get { return _substringSuggestions; } }

        internal TexlBinding Binding { get { return _binding; } }

        internal List<CommentToken> Comments { get { return _comments; } }

        public TexlFunction CurFunc { get { return _curFunc; } }

        internal TexlNode CurNode { get { return _curNode; } }

        public string Script { get { return _script; } }

        internal int CursorPos { get { return _cursorPos; } }

        public int ArgIndex { get { return _argIndex; } }

        public int ArgCount { get { return _argCount; } }

        internal IsValidSuggestion IsValidSuggestionFunc { get { return _isValidSuggestionFunc; } }

        internal string MatchingStr { get { return _matchingStr; } }

        internal int MatchingLength { get { return _matchingLength; } }

        public int ReplacementStartIndex { get { return _replacementStartIndex; } }

        public int ReplacementLength { get { return _replacementLength; } }

        internal string BoundTo { get; set; }

        internal IList<DType> MissingTypes { get { return _missingTypes; } }

        internal List<ISpecialCaseHandler> CleanupHandlers { get { return _cleanupHandlers; } }

        /// <summary>
        /// Type that defines valid symbols in the formula
        /// </summary>
        internal virtual DType ContextScope => Binding.ContextScope;

        /// <summary>
        /// Returns true if <see cref="suggestion"/> should be added to the suggestion list based on
        /// <see cref="type"/> and false otherwise.  May be used after suggestions and node type are found.
        /// Note: The default behavior has it so that all candidates are suggestible.  This may not always be
        /// desired.
        /// </summary>
        /// <param name="suggestion">
        /// Candidate suggestion string
        /// </param>
        /// <param name="type">
        /// Type of the node at the caller's context
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
        /// <see cref="node"/> and may be used to add additional suggestions after the rest.  This method
        /// does not alter control flow.
        /// </summary>
        /// <param name="node">
        /// Node for which suggestions may be added or other actions committed.
        /// </param>
        internal virtual void OnAddedSuggestionsForLeftNodeScope(TexlNode node) { }

        /// <summary>
        /// Should return true if name collides with existing symbols and false otherwise.  Used to determine
        /// whether to prepend a prefix to an enum value.
        /// </summary>
        /// <param name="name">
        /// Name in question
        /// </param>
        /// <returns>
        /// True if the provided name collides with an existing name or identifier, false otherwise.
        /// </returns>
        internal virtual bool DoesNameCollide(string name)
        {
            return (from enumSymbol in _powerFxConfig.EnumStore.EnumSymbols
                    where (from localizedEnum in enumSymbol.LocalizedEnumValues where localizedEnum == name select localizedEnum).Any()
                    select enumSymbol).Count() > 1;
        }

        /// <summary>
        /// Should unqualified enums be suggested
        /// </summary>
        internal virtual bool SuggestUnqualifiedEnums => true;

        /// <summary>
        /// Retrieves an <see cref="EnumSymbol"/> from <see cref="binding"/> (if necessary)
        /// </summary>
        /// <param name="name">
        /// Name of the enum symbol for which to look
        /// </param>
        /// <param name="binding">
        /// Binding in which may be looked for the enum symbol
        /// </param>
        /// <param name="enumSymbol">
        /// Should be set to the symbol for <see cref="name"/> if it is found, and left null otherwise.
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
        /// A list of the enum symbols defined for intellisense
        /// </summary>
        internal virtual IEnumerable<EnumSymbol> EnumSymbols => _powerFxConfig.EnumStore.EnumSymbols;

        /// <summary>
        /// Tries to add custom suggestions for a column specified by <see cref="type"/>
        /// </summary>
        /// <param name="type">
        /// The type of the column for which suggestions may be added
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
        internal virtual void BeforeAddSuggestionsForDottedNameNode(TexlNode node) { }

        /// <summary>
        /// This method is called by <see cref="Intellisense"/> to determine whether a candidate suggestion
        /// that represents a function should be suggested
        /// </summary>
        /// <param name="suggestion">
        /// Candidate suggestion wherein the key represents the suggestion name and the value represents its
        /// type
        /// </param>
        /// <returns>
        /// True if the function may be suggested, false otherwise
        /// </returns>
        internal virtual bool ShouldSuggestFunction(TexlFunction function) => true;

        /// <summary>
        /// Returns a list of argument suggestions for a given function, scope type, and argument index
        /// </summary>
        /// <param name="function">
        /// The function for which we are producing argument suggestions
        /// </param>
        /// <param name="scopeType">
        /// The type of the scope from where intellisense is run
        /// </param>
        /// <param name="argumentIndex">
        /// The index of the current argument of <see cref="function"/>
        /// </param>
        /// <param name="argsSoFar">
        /// The arguments that are present in the formula at the time of invocation
        /// </param>
        /// <param name="requiresSuggestionEscaping">
        /// Is set to whether the characters within the returned suggestion need have its characters escaped
        /// </param>
        /// <returns>
        /// Argument suggestions for the provided context
        /// </returns>
        internal virtual IEnumerable<KeyValuePair<string, DType>> GetArgumentSuggestions(TexlFunction function, DType scopeType, int argumentIndex, TexlNode[] argsSoFar, out bool requiresSuggestionEscaping)
        {
            Contracts.AssertValue(function);
            Contracts.AssertValue(scopeType);

            return ArgumentSuggestions.GetArgumentSuggestions(TryGetEnumSymbol, SuggestUnqualifiedEnums, function, scopeType, argumentIndex, out requiresSuggestionEscaping);
        }

        /// <summary>
        /// Should return the kind of suggestion that may be recomended for the
        /// <see cref="argumentIndex"/> parameter of <see cref="function"/>
        /// </summary>
        /// <param name="function">
        /// Function that the kind of suggestion for which this function determines
        /// </param>
        /// <param name="argumentIndex">
        /// The index of the argument to which the suggestion pertains
        /// </param>
        /// <returns>
        /// The suggestion kind for the hypothetical suggestion
        /// </returns>
        internal virtual SuggestionKind GetFunctionSuggestionKind(TexlFunction function, int argumentIndex) => SuggestionKind.Global;

        /// <summary>
        /// This method is called after all default suggestions for value possibilities have been run and may be
        /// overridden to provide custom suggestions
        /// </summary>
        internal virtual void AddCustomSuggestionsForValuePossibilities() { }

        /// <summary>
        /// May be overridden to provide custom suggestions at the point in intellisense runtime when
        /// suggestions for global identifiers are added
        /// </summary>
        internal virtual void AddCustomSuggestionsForGlobals() { }

        /// <summary>
        /// May be overridden to provide custom suggestions or change other state after the point in
        /// intellisense runtime where suggestions for global identifiers are added.
        /// </summary>
        internal virtual void AfterAddSuggestionsForGlobals() { }

        /// <summary>
        /// May be overridden to provide custom suggestions or change other state after the point in
        /// intellisense runtime where suggestions for unary operator keywords are added.
        /// </summary>
        internal virtual void AfterAddSuggestionsForUnaryOperatorKeywords() { }

        /// <summary>
        /// This collection is appended to the resultant suggestion list when
        /// <see cref="Intellisense.FirstNameNodeSuggestionHandler"/> is used.  It may be overridden to provide
        /// additional first name node suggestions.  It is called when the cursor is
        /// </summary>
        /// <returns>
        /// Sequence of suggestions for first name node context.
        /// </returns>
        internal virtual IEnumerable<string> SuggestableFirstNames => Enumerable.Empty<string>();


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
        internal virtual void AddAdditionalSuggestionsForLocalSymbols() { }

        /// <summary>
        /// This method may be overriden to add additional suggestions for generic selections to the resultant
        /// suggestion list for first name nodes.
        /// </summary>
        /// <param name="currentNode">
        /// The node for which Intellisense is invoked
        /// </param>
        internal virtual void AddAdditionalSuggestionsForKeywordSymbols(TexlNode currentNode) { }

        /// <param name="function">
        /// Function whose eligibility is called into question.
        /// </param>
        /// <returns>
        /// Returns true if <see cref="Intellisense.FunctionRecordNameSuggestionHandler"/> should make suggestions
        /// for the provided function and false otherwise.
        /// </returns>
        internal virtual bool IsFunctionElligibleForRecordSuggestions(TexlFunction function) => true;

        /// <param name="function">
        /// Function in question
        /// </param>
        /// <param name="callNode">
        /// The node at the present cursor position
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
        /// suggestions pertaining to <see cref="function"/> and <see cref="argIndex"/>.  If it returns true,
        /// <see cref="Intellisense.ErrorNodeSuggestionHandlerBase"/> will return immediately and no more suggestions
        /// will be added.
        /// </summary>
        /// <param name="function">
        /// Function for which additional suggestions may be added
        /// </param>
        /// <param name="argIndex">
        /// Index of the argument on which the cursor is positioned
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
        internal virtual void AddAlternativeTopLevelSuggestionsForErrorNode() { }

        /// <summary>
        /// This method is called by <see cref="Intellisense.ErrorNodeSuggestionHandlerBase"/> after it has added all
        /// its suggestions to <see cref="Suggestions"/>
        /// </summary>
        internal virtual void AddSuggestionsAfterTopLevelErrorNodeSuggestions() { }

        public virtual bool TryAugmentSignature(TexlFunction func, int argIndex, string paramName, int highlightStart, out int newHighlightStart, out int newHighlightEnd, out string newParamName, out string newInvariantParamName) =>
            DefaultIntellisenseData.DefaultTryAugmentSignature(func, argIndex, paramName, highlightStart, out newHighlightStart, out newHighlightEnd, out newParamName, out newInvariantParamName);

        public virtual string GenerateParameterDescriptionSuffix(TexlFunction function, string paramName) =>
            DefaultIntellisenseData.GenerateDefaultParameterDescriptionSuffix(function, paramName);

        internal bool SetMatchArea(int startIndex, int endIndex, int replacementLength = -1)
        {
            Contracts.Assert(0 <= startIndex && startIndex <= endIndex && endIndex <= _script.Length);

            // If we have already provided suggestions, we can't set the match area
            if (Suggestions.Count > 0 || SubstringSuggestions.Count > 0)
                return false;

            // Trim leading whitespace as there is no point to matching it
            while (startIndex < endIndex && string.IsNullOrWhiteSpace(Script.Substring(startIndex, 1)))
                startIndex++;

            _replacementStartIndex = startIndex;
            _matchingLength = endIndex - startIndex;
            _replacementLength = replacementLength < 0 ? _matchingLength : replacementLength;
            _matchingStr = TexlLexer.UnescapeName(_script.Substring(startIndex, _matchingLength));

            return true;
        }
    }
}
