// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests
{
    public class MockSqlEngine : IPowerFxScope, IPowerFxScopeDisplayName, IPowerFxScopeQuickFix
    {
        public CheckResult Check(string expression)
        {
            throw new System.NotImplementedException();
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition)
        {
            throw new System.NotImplementedException();
        }

        public string TranslateToDisplayName(string expression)
        {
            return expression.Replace("new_price", "Price").Replace("new_quantity", "Quantity");
        }

        public CodeActionResult[] Suggest(string expression)
        {
            return new CodeActionResult[]
            {
                new CodeActionResult
                {
                    Text = "TestText1", Title = "TestTitle1",
                    Range = new Range
                    {
                        Start = new Position
                        {
                            Line = 0,
                            Character = 0
                        },
                        End = new Position
                        {
                            Line = 0,
                            Character = 10
                        }
                    }
                }
            };
        }
    }
}
