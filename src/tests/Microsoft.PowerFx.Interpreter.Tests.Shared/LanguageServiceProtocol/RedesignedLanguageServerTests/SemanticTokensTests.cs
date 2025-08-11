// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Interpreter.Tests.LanguageServiceProtocol;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.LanguageServerProtocol.Schemas;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public partial class LanguageServerTestBase
    {
        #region Full Document Semantic Tokens Tests
        [Fact]
        public async Task TestCorrectFullSemanticTokensAreReturnedWithExpressionInUri()
        {
            await TestCorrectFullSemanticTokensAreReturned(new SemanticTokensParams
            {
                TextDocument = GetTextDocument(GetUri("expression=Max(1, 2, 3)"))
            });
        }

        [Fact]
        public async Task TestCorrectFullSemanticTokensAreReturnedWithExpressionNotInUri()
        {
            await TestCorrectFullSemanticTokensAreReturned(new SemanticTokensParams
            {
                TextDocument = GetTextDocument(),
                Text = "Max(1, 2, 3)"
            });
        }

        [Fact]
        public async Task TestCorrectFullSemanticTokensAreReturnedWithExpressionInBothUriAndTextDocument()
        {
            var expression = "Max(1, 2, 3)";
            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = GetTextDocument(GetUri("expression=Color.White")),
                Text = expression
            };
            await TestCorrectFullSemanticTokensAreReturned(semanticTokenParams);
        }

        private async Task TestCorrectFullSemanticTokensAreReturned(SemanticTokensParams semanticTokensParams)
        {
            // Arrange
            Init();
            var expression = GetExpression(semanticTokensParams);
            Assert.Equal("Max(1, 2, 3)", expression);
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokensParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(rawResponse, payload.id);
            Assert.NotEmpty(response.Data);
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(response, expression);
            Assert.Single(decodedTokens.Where(tok => tok.TokenType == TokenType.Function));
            Assert.Equal(3, decodedTokens.Where(tok => tok.TokenType == TokenType.NumLit || tok.TokenType == TokenType.DecLit).Count());
        }

        [Theory]
        [InlineData("Create", TokenType.Function, TokenType.BoolLit)]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("[2, 3")]
        [InlineData("Create", TokenType.StrInterpStart, TokenType.BinaryOp, TokenType.NumLit, TokenType.DecLit, TokenType.Control)]
        [InlineData("[]")]
        [InlineData("1,2]")]
        [InlineData("[98]")]
        [InlineData("Create", TokenType.Lim)]
        [InlineData("Create", TokenType.BoolLit, TokenType.BinaryOp, TokenType.Function, TokenType.Lim)]
        [InlineData("Create", TokenType.Lim, TokenType.BinaryOp, TokenType.BoolLit)]
        [InlineData("   ")]
        [InlineData("NotPresent")]
        internal async Task TestCorrectFullSemanticTokensAreReturnedWithCertainTokenTypesSkipped(string tokenTypesToSkipParam, params TokenType[] tokenTypesToSkip)
        {
            // Arrange
            var expression = "1+-2;true;\"String Literal\";Sum(1,2);Max(1,2,3);$\"1 + 2 = {3}\";// This is Comment";
            var expectedTypes = new List<TokenType> { TokenType.DecLit, TokenType.BoolLit, TokenType.Comment, TokenType.Function, TokenType.StrInterpStart, TokenType.IslandEnd, TokenType.IslandStart, TokenType.StrLit, TokenType.StrInterpEnd, TokenType.Delimiter, TokenType.BinaryOp };
            if (tokenTypesToSkip.Length > 0)
            {
                expectedTypes = expectedTypes.Where(expectedType => !tokenTypesToSkip.Contains(expectedType)).ToList();
            }

            if (tokenTypesToSkipParam == "Create")
            {
                tokenTypesToSkipParam = JsonSerializer.Serialize(tokenTypesToSkip.Select(tokType => (int)tokType).ToList());
            }

            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = GetTextDocument(GetUri(tokenTypesToSkipParam == "NotPresent" ? string.Empty : "tokenTypesToSkip=" + tokenTypesToSkipParam)),
                Text = expression
            };
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(rawResponse, payload.id);
            Assert.NotEmpty(response.Data);
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(response, expression);
            var actualTypes = decodedTokens.Select(tok => tok.TokenType).Distinct().ToList();
            Assert.Equal(expectedTypes.OrderBy(type => type), actualTypes.OrderBy(type => type));
        }

        [Fact]
        public async Task TestErrorResponseReturnedWhenUriIsNullForFullSemanticTokensRequest()
        {
            // Arrange
            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = new TextDocumentIdentifier() { Uri = null }
            };
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var response = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            AssertErrorPayload(response, payload.id, JsonRpcHelper.ErrorCode.ParseError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestEmptyFullSemanticTokensResponseReturnedWhenExpressionIsInvalid(bool isNotNull)
        {
            // Arrange
            string expression = string.Empty;
            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = GetTextDocument(),
                Text = isNotNull ? expression : null
            };
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(rawResponse, payload.id);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task TestFullSemanticTokensResponseReturnedWithDefaultEOL()
        {
            // Arrange
            string expression = "Sum(\n1,1\n)";
            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = GetTextDocument(),
                Text = expression
            };
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(rawResponse, payload.id);
            Assert.NotEmpty(response.Data);
            Assert.Equal(expression.Where(c => c == '\n').Count(), SemanticTokensRelatedTestsHelper.DetermineNumberOfLinesThatTokensAreSpreadAcross(response));
        }

        [Theory]
        [InlineData("9 + 9", 0)]
        [InlineData("Max(10, 20, 30)", 0)]
        [InlineData("Color.AliceBlue", 0)]
        [InlineData("9 + 9\n Max(1, 3, 19); \n Color.AliceBlue", 0)]
        [InlineData("Label2.Text", 1)]
        [InlineData("NestedLabel1", 1)]
        [InlineData("Label2.Text;\nNestedLabel1", 2)]
        public async Task TestPublishControlTokensNotification(string expression, int expectedNumberOfControlTokens)
        {
            // Arrange
            var checkResult = SemanticTokensRelatedTestsHelper.GetCheckResultWithControlSymbols(expression);
            var scopeFactory = new TestPowerFxScopeFactory(
                (string documentUri) => new EditorContextScope(
                    (expr) => checkResult));
            Init(new InitParams(scopeFactory: scopeFactory));
            var payload = GetFullDocumentSemanticTokensRequestPayload(new SemanticTokensParams
            {
                TextDocument = GetTextDocument(GetUri("&version=someVersionId")),
                Text = expression
            });

            // Act
            var response = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var notificationParams = AssertAndGetNotificationParams<PublishControlTokensParams>(response, CustomProtocolNames.PublishControlTokens);
            Assert.Equal("someVersionId", notificationParams.Version);
            var controlTokenList = notificationParams.Controls;
            Assert.Equal(expectedNumberOfControlTokens, controlTokenList.Count());
            foreach (var controlToken in controlTokenList)
            {
                Assert.Equal(typeof(ControlToken), controlToken.GetType());
            }
        }

        private static (string payload, string id) GetFullDocumentSemanticTokensRequestPayload(SemanticTokensParams semanticTokenParams, string id = null)
        {
            return GetRequestPayload(semanticTokenParams, TextDocumentNames.FullDocumentSemanticTokens, id);
        }

        #endregion

        #region Range Document Semantic Tokens Tests
        [Theory]
        [InlineData("Create", TokenType.Function, TokenType.BoolLit)]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("[2, 3")]
        [InlineData("Create", TokenType.StrInterpStart, TokenType.BinaryOp, TokenType.NumLit, TokenType.DecLit, TokenType.Control)]
        [InlineData("[]")]
        [InlineData("1,2]")]
        [InlineData("[98]")]
        [InlineData("Create", TokenType.Lim)]
        [InlineData("Create", TokenType.BoolLit, TokenType.BinaryOp, TokenType.Function, TokenType.Lim)]
        [InlineData("Create", TokenType.Lim, TokenType.BinaryOp, TokenType.BoolLit)]
        [InlineData("   ")]
        [InlineData("NotPresent")]
        internal async Task TestCorrectRangeSemanticTokensAreReturnedWithCertainTokenTypesSkipped(string tokenTypesToSkipParam, params TokenType[] tokenTypesToSkip)
        {
            // Arrange & Assert
            var expression = "1+1+1+1+1+1+1+1;Sqrt(1);1+-2;true;\n\"String Literal\";Sum(1,2);Max(1,2,3);$\"1 + 2 = {3}\";// This is Comment;//This is comment2;false";
            var range = SemanticTokensRelatedTestsHelper.CreateRange(1, 2, 3, 35);
            var expectedTypes = new List<TokenType> { TokenType.DecLit, TokenType.BoolLit, TokenType.Function, TokenType.StrLit, TokenType.Delimiter, TokenType.BinaryOp };
            if (tokenTypesToSkip.Length > 0)
            {
                expectedTypes = expectedTypes.Where(expectedType => !tokenTypesToSkip.Contains(expectedType)).ToList();
            }

            if (tokenTypesToSkipParam == "Create")
            {
                tokenTypesToSkipParam = JsonSerializer.Serialize(tokenTypesToSkip.Select(tokType => (int)tokType).ToList());
            }

            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = GetTextDocument(GetUri(tokenTypesToSkipParam == "NotPresent" ? string.Empty : "tokenTypesToSkip=" + tokenTypesToSkipParam)),
                Text = expression,
                Range = range
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(rawResponse, payload.id);
            Assert.NotEmpty(response.Data);
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(response, expression);
            var actualTypes = decodedTokens.Select(tok => tok.TokenType).Distinct().ToList();
            Assert.Equal(expectedTypes.OrderBy(type => type), actualTypes.OrderBy(type => type));
        }

        [Theory]
        [InlineData(1, 4, 1, 20, false, false)]
        [InlineData(1, 6, 1, 20, true, false)]
        [InlineData(1, 1, 1, 12, false, true)]
        [InlineData(1, 5, 1, 24, true, false)]
        [InlineData(1, 5, 1, 15, true, true)]
        [InlineData(1, 9, 1, 14, true, true)]
        [InlineData(1, 1, 3, 34, false, false)]
        [InlineData(1, 3, 2, 12, false, true)]
        [InlineData(2, 11, 3, 3, true, true)]
        [InlineData(1, 24, 3, 17, false, false)]
        public async Task TestCorrectRangeSemanticTokensAreReturned(int startLine, int startLineCol, int endLine, int endLineCol, bool tokenDoesNotAlignOnLeft, bool tokenDoesNotAlignOnRight)
        {
            // Arrange
            Init();
            var expression = "If(Len(Phone_Number) < 10,\nNotify(\"Invalid Phone\nNumber\"),Notify(\"Valid Phone No\"))";
            var eol = "\n";
            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = GetTextDocument(),
                Text = expression,
                Range = SemanticTokensRelatedTestsHelper.CreateRange(startLine, endLine, startLineCol, endLineCol)
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(rawResponse, payload.id);
            var (startIndex, endIndex) = semanticTokenParams.Range.ConvertRangeToPositions(expression, eol);
            var decodedResponse = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(response, expression, eol);

            var leftMostTok = decodedResponse.Min(tok => tok.StartIndex);
            var rightMostTok = decodedResponse.Max(tok => tok.EndIndex);

            Assert.All(decodedResponse, (tok) => Assert.False(tok.EndIndex <= leftMostTok || tok.StartIndex >= rightMostTok));
            if (tokenDoesNotAlignOnLeft)
            {
                Assert.True(leftMostTok < startIndex);
            }
            else
            {
                Assert.True(leftMostTok >= startIndex);
            }

            if (tokenDoesNotAlignOnRight)
            {
                Assert.True(rightMostTok > endIndex);
            }
            else
            {
                Assert.True(rightMostTok <= endIndex);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestEmptyRangeSemanticTokensResponseReturnedWhenExpressionIsInvalid(bool isNotNull)
        {
            // Arrange
            string expression = string.Empty;
            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = GetTextDocument(),
                Text = isNotNull ? expression : null,
                Range = SemanticTokensRelatedTestsHelper.CreateRange(1, 1, 1, 4)
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(rawResponse, payload.id);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task TestErrorResponseReturnedWhenUriIsNullForRangeSemanticTokensRequest()
        {
            // Arrange
            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = new TextDocumentIdentifier() { Uri = null },
                Range = SemanticTokensRelatedTestsHelper.CreateRange(1, 1, 1, 4)
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var response = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            AssertErrorPayload(response, payload.id, JsonRpcHelper.ErrorCode.ParseError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestEmptyRangeSemanticTokensResponseReturnedWhenRangeIsNullOrInvalid(bool isNull)
        {
            // Arrange
            var expression = "If(Len(Phone_Number) < 10,\nNotify(\"Invalid Phone\nNumber\"),Notify(\"Valid Phone No\"))";
            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = GetTextDocument(),
                Text = expression,
                Range = isNull ? null : SemanticTokensRelatedTestsHelper.CreateRange(expression.Length + 2, 2, 1, 2)
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(rawResponse, payload.id);
            Assert.Empty(response.Data);
        }

        private static (string payload, string id) GetRangeDocumentSemanticTokensRequestPayload(SemanticTokensRangeParams semanticTokenRangeParams, string id = null)
        {
            return GetRequestPayload(semanticTokenRangeParams, TextDocumentNames.RangeDocumentSemanticTokens, id);
        }
        #endregion

        private static SemanticTokensResponse AssertAndGetSemanticTokensResponse(string response, string id)
        {
            var tokensResponse = AssertAndGetResponsePayload<SemanticTokensResponse>(response, id);
            Assert.NotNull(tokensResponse);
            Assert.NotNull(tokensResponse.Data);
            return tokensResponse;
        }
    }
}
