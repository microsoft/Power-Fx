// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Texl.Intellisense;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests
{
    public class MockSqlEngine : IPowerFxScope, IPowerFxScopeDisplayName
    {
        public CheckResult Check(string expression)
        {
            throw new System.NotImplementedException();
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition)
        {
            throw new System.NotImplementedException();
        }

        public CodeActionResult[] QuickFix(string expression)
        {
            throw new System.NotImplementedException();
        }

        public string TranslateToDisplayName(string expression)
        {
            return expression.Replace("new_price", "Price").Replace("new_quantity", "Quantity");
        }
    }
}
