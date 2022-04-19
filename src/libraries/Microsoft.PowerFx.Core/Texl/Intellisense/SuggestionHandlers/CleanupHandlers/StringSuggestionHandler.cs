// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal sealed class StringSuggestionHandler : ISpecialCaseHandler
    {
        private readonly int _tokenStartIndex;
        private readonly bool _requireTokenStartWithQuote;

        public StringSuggestionHandler(int startIndex, bool requireTokenStartWithQuote = true)
        {
            Contracts.Assert(startIndex >= 0);

            _tokenStartIndex = startIndex;
            _requireTokenStartWithQuote = requireTokenStartWithQuote;
        }

        public bool Run(IIntellisenseContext context, IntellisenseData.IntellisenseData intellisenseData, List<IntellisenseSuggestion> suggestions)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(context.InputText);
            Contracts.AssertValue(intellisenseData);
            Contracts.AssertValue(suggestions);

            var script = context.InputText;
            Contracts.Assert(_tokenStartIndex < script.Length);

            if (_requireTokenStartWithQuote && script[_tokenStartIndex] != '"')
            {
                return false;
            }

            var matchEndIndex = -1;
            var foundAny = false;
            var iterateSuggestions = suggestions.ToArray();

            foreach (var suggestion in iterateSuggestions)
            {
                int i, j;
                var found = false;

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
                        var curChar = script.Substring(i, 1);
                        if (curChar != TexlLexer.PunctuatorParenClose && curChar != TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator)
                        {
                            found = false;
                        }

                        break;
                    }

                    found = true;
                }

                foundAny |= found;

                if (found && matchEndIndex < i)
                {
                    matchEndIndex = i;
                }

                if (!found && i != script.Length)
                {
                    suggestions.Remove(suggestion);
                }
            }

            if (!foundAny || matchEndIndex <= _tokenStartIndex)
            {
                return false;
            }

            intellisenseData.Suggestions.Clear();
            intellisenseData.SubstringSuggestions.Clear();
            intellisenseData.SetMatchArea(_tokenStartIndex, matchEndIndex);
            return true;
        }
    }
}
