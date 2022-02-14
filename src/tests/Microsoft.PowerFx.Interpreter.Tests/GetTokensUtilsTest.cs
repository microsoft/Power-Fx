// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class GetTokensUtilsTest
    {
        [Fact]
        public void GetTokensTest()
        {
            var scope = RecalcEngineScope.FromJson(new RecalcEngine(), "{\"A\":1,\"B\":[1,2,3]}");
            var checkResult = scope.Check("A+CountRows(B)");

            var result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.None);
            Assert.Equal(0, result.Count);

            result = GetTokensUtils.GetTokens(checkResult._binding, GetTokensFlags.UsedInExpression);
            Assert.Equal(3, result.Count);
            Assert.Equal(TokenResultType.Variable, result["A"]);
            Assert.Equal(TokenResultType.Variable, result["B"]);
            Assert.Equal(TokenResultType.Function, result["CountRows"]);

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
            var optionSet = new OptionSet("OptionSet", new Dictionary<string, string>() 
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            });
            var config = new PowerFxConfig(null, null);
            config.AddOptionSet(optionSet);

            var scope = RecalcEngineScope.FromJson(new RecalcEngine(config), "{\"A\":1,\"B\":[1,2,3]}");
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
            var scope = RecalcEngineScope.FromJson(new RecalcEngine(), "{\"A\":1,\"B\":[1,2,3]}");
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
