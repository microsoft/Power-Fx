// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
