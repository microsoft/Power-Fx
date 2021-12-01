﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense{
    internal sealed class StringSuggestionHandler : ISpecialCaseHandler
    {
        private readonly int _tokenStartIndex;
        private readonly bool _requireTokenStartWithQuote;

        public StringSuggestionHandler(int startIndex, bool requireTokenStartWithQuote = true)
        {
            Contracts.Assert(0 <= startIndex);

            _tokenStartIndex = startIndex;
            _requireTokenStartWithQuote = requireTokenStartWithQuote;
        }

        public bool Run(IIntellisenseContext context, IntellisenseData.IntellisenseData intellisenseData, List<IntellisenseSuggestion> suggestions)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(context.InputText);
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(suggestions);

            string script = context.InputText;
            Contracts.Assert(_tokenStartIndex < script.Length);

            if (_requireTokenStartWithQuote && script[_tokenStartIndex] != '"')
                return false;

            int matchEndIndex = -1;
            bool foundAny = false;
            var iterateSuggestions = suggestions.ToArray();

            foreach (var suggestion in iterateSuggestions)
            {
                int i, j;
                bool found = false;

                for (i = _tokenStartIndex, j = 0; i < script.Length; i++, j++)
                {
                    if (j >= suggestion.Text.Length)
                    {
                        // The input text for this parameter has exceeded the suggestion and we should filter it out
                        found = false;
                        break;
                    }

                    if (script[i] != suggestion.Text[j])
                    {
                        string curChar = script.Substring(i, 1);
                        if (curChar != TexlLexer.PunctuatorParenClose && curChar != TexlLexer.LocalizedInstance.LocalizedPunctuatorListSeparator)
                            found = false;
                        break;
                    }

                    found = true;
                }

                foundAny |= found;

                if (found && matchEndIndex < i)
                    matchEndIndex = i;

                if (!found && i != script.Length)
                    suggestions.Remove(suggestion);
            }

            if (!foundAny || matchEndIndex <= _tokenStartIndex)
                return false;

            intellisenseData.Suggestions.Clear();
            intellisenseData.SubstringSuggestions.Clear();
            intellisenseData.SetMatchArea(_tokenStartIndex, matchEndIndex);
            return true;
        }
    }
}
