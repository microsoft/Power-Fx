// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Tests.BindingEngineTests;

namespace Microsoft.PowerFx.Tests
{
    public class GetTokensUtilsTest : PowerFxTest
    {
        private static EditorContextScope FromJson(Engine engine, string json, ParserOptions options = null)
        {
            var context = (RecordValue)FormulaValueJSON.FromJson(json);
            var symbols = ReadOnlySymbolTable.NewFromRecord(context.Type);
            return engine.CreateEditorScope(options, symbols);
        }

        [Theory]
        [InlineData("A+CountRows(B)", false, 3)]
        [InlineData("Behavior(); A+CountRows(B)", true, 4)]
        public void GetTokensTest(string expr, bool withAllowSideEffects, int expectedCount)
        {            
            var config = new PowerFxConfig();
            config.AddFunction(new BehaviorFunction());

            var scope = FromJson(
                new RecalcEngine(config), 
                "{\"A\":1,\"B\":[1,2,3]}",
                withAllowSideEffects ? new ParserOptions() { AllowsSideEffects = true } : null);
            var checkResult = scope.Check(expr);

            var result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.None);
            Assert.Equal(0, result.Count);

            result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.UsedInExpression);
            Assert.Equal(expectedCount, result.Count);
            Assert.Equal(TokenResultType.Variable, result["A"]);
            Assert.Equal(TokenResultType.Variable, result["B"]);
            Assert.Equal(TokenResultType.Function, result["CountRows"]);

            if (expectedCount == 4)
            {
                Assert.Equal(TokenResultType.Function, result["Behavior"]);
            }

            result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.AllFunctions);
            Assert.False(result.ContainsKey("A"));
            Assert.False(result.ContainsKey("B"));
            Assert.Equal(TokenResultType.Function, result["Abs"]);
            Assert.Equal(TokenResultType.Function, result["CountRows"]);
            Assert.Equal(TokenResultType.Function, result["Year"]);

            result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.UsedInExpression | GetTokensFlags.AllFunctions);
            Assert.Equal(TokenResultType.Variable, result["A"]);
            Assert.Equal(TokenResultType.Variable, result["B"]);
            Assert.Equal(TokenResultType.Function, result["Abs"]);
            Assert.Equal(TokenResultType.Function, result["Year"]);
            Assert.Equal(TokenResultType.Function, result["CountRows"]);
        }

        [Fact]
        public void GetTokensHostSymbolTest()
        {
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));
            var config = new PowerFxConfig(null);
            config.AddOptionSet(optionSet);

            var scope = FromJson(new RecalcEngine(config), "{\"A\":1,\"B\":[1,2,3]}");
            var checkResult = scope.Check("If(OptionSet.Option2 = OptionSet.Option1, A, First(B)");

            var result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.UsedInExpression);
            Assert.Equal(5, result.Count);
            Assert.Equal(TokenResultType.Function, result["If"]);
            Assert.Equal(TokenResultType.HostSymbol, result["OptionSet"]);
            Assert.Equal(TokenResultType.Variable, result["A"]);
            Assert.Equal(TokenResultType.Function, result["First"]);
            Assert.Equal(TokenResultType.Variable, result["B"]);
        }

        [Fact]
        public void GetTokensFromBadFormulaTest()
        {
            var scope = FromJson(new RecalcEngine(), "{\"A\":1,\"B\":[1,2,3]}");
            var checkResult = scope.Check("A + CountRows(B) + C + NoFunction(123)");

            var result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.None);
            Assert.Equal(0, result.Count);

            result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.UsedInExpression);
            Assert.Equal(3, result.Count);
            Assert.Equal(TokenResultType.Variable, result["A"]);
            Assert.Equal(TokenResultType.Variable, result["B"]);
            Assert.Equal(TokenResultType.Function, result["CountRows"]);
            Assert.False(result.ContainsKey("C"));
            Assert.False(result.ContainsKey("NoFunction"));

            result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.AllFunctions);
            Assert.False(result.ContainsKey("A"));
            Assert.False(result.ContainsKey("B"));
            Assert.Equal(TokenResultType.Function, result["Abs"]);
            Assert.Equal(TokenResultType.Function, result["CountRows"]);
            Assert.Equal(TokenResultType.Function, result["Year"]);
            Assert.False(result.ContainsKey("C"));
            Assert.False(result.ContainsKey("NoFunction"));

            result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.UsedInExpression | GetTokensFlags.AllFunctions);
            Assert.Equal(TokenResultType.Variable, result["A"]);
            Assert.Equal(TokenResultType.Variable, result["B"]);
            Assert.Equal(TokenResultType.Function, result["Abs"]);
            Assert.Equal(TokenResultType.Function, result["Year"]);
            Assert.Equal(TokenResultType.Function, result["CountRows"]);
            Assert.False(result.ContainsKey("C"));
            Assert.False(result.ContainsKey("NoFunction"));
        }
    }
}
