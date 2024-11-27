// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.Shared
{
    public class EnumTests : PowerFxTest
    {
        [Fact]
        public void EnumShadowingTest()
        {
            var placeholder = "With({{{0}:{{{1}:{2}}}}},{0}.{1})";
            var fixedString = "**********";
            var fixedNumber = 1_000_000;
            var engine = new RecalcEngine();

            foreach (var enumSymbol in EnumStoreBuilder.DefaultEnumSymbols)
            {
                Assert.True(true);

                foreach (var name in enumSymbol.Value.OptionNames)
                {
                    var expression = string.Empty;

                    switch (enumSymbol.Value.BackingKind)
                    {
                        case DKind.String:
                        case DKind.Color:
                            expression = string.Format(placeholder, enumSymbol.Key, name.Value, $"\"{fixedString}\"");
                            break;

                        case DKind.Number:
                            expression = string.Format(placeholder, enumSymbol.Key, name.Value, fixedNumber);
                            break;

                        default:
                            Assert.Fail($"DKind '{enumSymbol.Value.BackingKind}' is not expected.");
                            break;
                    }

                    var check = engine.Check(expression);

                    Assert.True(check.IsSuccess);

                    var result = check.GetEvaluator().Eval();

                    if (result is StringValue stringValue)
                    {
                        Assert.Equal(fixedString, stringValue.Value);
                    }
                    else if (result is DecimalValue decimalValue)
                    {
                        Assert.Equal(fixedNumber, decimalValue.Value);
                    }
                    else
                    {
                        Assert.Fail($"Result of '{result.Type}' is not expected.");
                    }
                }
            }
        }
    }
}
