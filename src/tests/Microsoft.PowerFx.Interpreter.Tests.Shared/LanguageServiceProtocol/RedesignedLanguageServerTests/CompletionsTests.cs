// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public partial class LanguageServerTestBase
    {
        [Theory]
        [InlineData("Color.AliceBl", 13, false)]
        [InlineData("Behavior(); Color.AliceBl", 25, true)]

        // $$$ This test generates an internal error as we use an behavior function but we have no way to check its presence
        [InlineData("Behavior(); Color.AliceBl", 25, false)]
        public async Task TestCompletionWithExpressionInUriOrNotInUri(string text, int offset, bool withAllowSideEffects)
        {
            foreach (var addExprToUri in new bool[] { true, false })
            {
                var params1 = new CompletionParams()
                {
                    TextDocument = addExprToUri ? GetTextDocument(GetUri("expression=" + text)) : GetTextDocument(),
                    Text = addExprToUri ? null : text,
                    Position = GetPosition(offset),
                    Context = GetCompletionContext()
                };
                var params2 = new CompletionParams()
                {
                    TextDocument = addExprToUri ?
                                   GetTextDocument(GetUri("expression=Color.&context={\"A\":\"ABC\",\"B\":{\"Inner\":123}}")) :
                                   GetTextDocument(GetUri("context={\"A\":\"ABC\",\"B\":{\"Inner\":123}}")),
                    Text = addExprToUri ? null : "Color.",
                    Position = GetPosition(7),
                    Context = GetCompletionContext()
                };
                var params3 = new CompletionParams()
                {
                    TextDocument = addExprToUri ? GetTextDocument(GetUri("expression={a:{},b:{},c:{}}.")) : GetTextDocument(),
                    Text = addExprToUri ? null : "{a:{},b:{},c:{}}.",
                    Position = GetPosition(17),
                    Context = GetCompletionContext()
                };
                await TestCompletionCore(params1, params2, params3, withAllowSideEffects);
            }
        }

        [Fact]
        public async Task TestCompletionWithExpressionBothInUriAndTextDocument()
        {
            int offset = 25;
            string text = "Behavior(); Color.AliceBl";
            string uriText = "Max(1,";
            bool withAllowSideEffects = false;

            var params1 = new CompletionParams()
            {
                TextDocument = GetTextDocument(GetUri("expression=" + uriText)),
                Text = text,
                Position = GetPosition(offset),
                Context = GetCompletionContext()
            };
            var params2 = new CompletionParams()
            {
                TextDocument = GetTextDocument(GetUri("context={\"A\":\"ABC\",\"B\":{\"Inner\":123}}&expression=1+1")),
                Text = "Color.",
                Position = GetPosition(7),
                Context = GetCompletionContext()
            };
            var params3 = new CompletionParams()
            {
                TextDocument = GetTextDocument(GetUri("expression=Color.")),
                Text = "{a:{},b:{},c:{}}.",
                Position = GetPosition(17),
                Context = GetCompletionContext()
            };
            await TestCompletionCore(params1, params2, params3, withAllowSideEffects);
        }

        private async Task TestCompletionCore(CompletionParams params1, CompletionParams params2, CompletionParams params3, bool withAllowSideEffects)
        {
            Init(new InitParams(options: GetParserOptions(withAllowSideEffects)));

            // test good formula
            var payload = GetCompletionPayload(params1);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            var response = AssertAndGetResponsePayload<CompletionResult>(rawResponse, payload.id);
            var foundItems = response.Items.Where(item => item.Label == "AliceBlue");
            Assert.True(Enumerable.Count(foundItems) == 1, "AliceBlue should be found from suggestion result");
            Assert.Equal("AliceBlue", foundItems.First().InsertText);
            Assert.Equal("000", foundItems.First().SortText);

            payload = GetCompletionPayload(params2);
            rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            response = AssertAndGetResponsePayload<CompletionResult>(rawResponse, payload.id);
            foundItems = response.Items.Where(item => item.Label == "AliceBlue");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.True(Enumerable.Count(foundItems) == 1, "AliceBlue should be found from suggestion result");
            Assert.Equal("AliceBlue", foundItems.First().InsertText);
            Assert.Equal("000", foundItems.First().SortText);

            payload = GetCompletionPayload(params3);
            rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            response = AssertAndGetResponsePayload<CompletionResult>(rawResponse, payload.id);
            foundItems = response.Items.Where(item => item.Label == "a");
            Assert.True(Enumerable.Count(foundItems) == 1, "'a' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.Equal("a", foundItems.First().InsertText);
            Assert.Equal("000", foundItems.First().SortText);

            foundItems = response.Items.Where(item => item.Label == "b");
            Assert.True(Enumerable.Count(foundItems) == 1, "'b' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.Equal("b", foundItems.First().InsertText);
            Assert.Equal("001", foundItems.First().SortText);

            foundItems = response.Items.Where(item => item.Label == "c");
            Assert.True(Enumerable.Count(foundItems) == 1, "'c' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.Equal("c", foundItems.First().InsertText);
            Assert.Equal("002", foundItems.First().SortText);

            // missing 'expression' in documentUri
            payload = GetCompletionPayload(new CompletionParams()
            {
                TextDocument = GetTextDocument("powerfx://test"),
                Position = GetPosition(1),
                Context = GetCompletionContext()
            });
            var errorResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            AssertErrorPayload(errorResponse, payload.id, JsonRpcHelper.ErrorCode.InvalidParams);
        }

        [Theory]
        [InlineData("'A", 1)]
        [InlineData("'Acc", 1)]
        public async Task TestCompletionWithIdentifierDelimiter(string text, int offset)
        {
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => new MockDataSourceEngine());
            Init(new InitParams(scopeFactory: scopeFactory));
            var params1 = new CompletionParams()
            {
                TextDocument = GetTextDocument(GetUri("expression=" + text)),
                Text = text,
                Position = GetPosition(offset),
                Context = GetCompletionContext()
            };
            var payload = GetCompletionPayload(params1);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            var response = AssertAndGetResponsePayload<CompletionResult>(rawResponse, payload.id);
            var foundItems = response.Items.Where(item => item.Label == "'Account'");
            Assert.True(Enumerable.Count(foundItems) == 1, "'Account' should be found from suggestion result");

            // Test that the Identifier delimiter is ignored in case of insertText,
            // when preceding character is also the same identifier delimiter
            Assert.Equal("Account'", foundItems.First().InsertText);
            Assert.Equal("000", foundItems.First().SortText);
        }

        private static (string payload, string id) GetCompletionPayload(CompletionParams completionParams)
        {
            return GetRequestPayload(completionParams, TextDocumentNames.Completion);
        }

        private static CompletionContext GetCompletionContext()
        {
            return new CompletionContext();
        }
    }
}
