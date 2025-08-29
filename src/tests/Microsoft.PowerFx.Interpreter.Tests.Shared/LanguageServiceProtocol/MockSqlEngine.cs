// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests
{
    public class MockSqlEngine : IPowerFxScope
    {
        public CheckResult Check(string expression)
        {
            throw new System.NotImplementedException();
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition)
        {
            throw new System.NotImplementedException();
        }

        public string ConvertToDisplay(string expression)
        {
            return expression.Replace("new_price", "Price").Replace("new_quantity", "Quantity");
        }
    }
}
