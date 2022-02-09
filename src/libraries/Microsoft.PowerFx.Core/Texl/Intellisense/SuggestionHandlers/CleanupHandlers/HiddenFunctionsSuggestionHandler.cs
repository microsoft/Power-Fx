// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal sealed class HiddenFunctionsSuggestionHandler : ISpecialCaseHandler
    {
        public bool Run(IIntellisenseContext context, IntellisenseData.IntellisenseData intellisenseData, List<IntellisenseSuggestion> suggestions)
        {
            IntellisenseSuggestion toRemove = null;

            foreach (var suggestion in suggestions)
            {
                if (suggestion.FunctionName == IdentToken.StrInterpIdent)
                {
                    toRemove = suggestion;
                    break;
                }
            }

            if (toRemove != null)
            {
                suggestions.Remove(toRemove);
            }

            return true;
        }
    }
}
