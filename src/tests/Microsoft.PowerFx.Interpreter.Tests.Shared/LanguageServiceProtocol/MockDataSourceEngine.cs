// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Intellisense.IntellisenseData;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests
{
    public class MockDataSourceEngine : IPowerFxScope
    {
        public CheckResult Check(string expression)
        {
            throw new System.NotImplementedException();
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition)
        {
            var intlData = new List<IntellisenseSuggestion>
                {
                    new IntellisenseSuggestion(new UIString("'Account'"), SuggestionKind.Global, SuggestionIconKind.DataSource, DType.Unknown, string.Empty, 0, string.Empty, string.Empty)
                };
            return new IntellisenseResult(new DefaultIntellisenseData(), intlData, null);
        }

        public string ConvertToDisplay(string expression)
        {
            throw new System.NotImplementedException();
        }
    }
}
