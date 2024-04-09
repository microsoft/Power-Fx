// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public partial class LanguageServerTestBase
    {
        [Fact]
        public async Task TestInitialFixup()
        {
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => new TestAsyncPowerFxScope()
            {
                ConvertToDisplayAsyncCallback = async (string expr, CancellationToken token) =>
                {
                    await Task.Delay(150, token).ConfigureAwait(false);
                    return new MockSqlEngine().ConvertToDisplay(expr);
                }
            });

            Init(new InitParams(scopeFactory: scopeFactory));
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}";
            var payload = GetRequestPayload(
            new InitialFixupParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = documentUri,
                    LanguageId = "powerfx",
                    Version = 1,
                    Text = "new_price * new_quantity"
                }
            }, CustomProtocolNames.InitialFixup);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            var response = AssertAndGetResponsePayload<TextDocumentItem>(rawResponse, payload.id);

            Assert.Equal(documentUri, response.Uri);
            Assert.Equal("Price * Quantity", response.Text);

            // no change
            payload = GetRequestPayload(
            new InitialFixupParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = documentUri,
                    LanguageId = "powerfx",
                    Version = 1,
                    Text = "Price * Quantity"
                }
            }, CustomProtocolNames.InitialFixup);
            rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            response = AssertAndGetResponsePayload<TextDocumentItem>(rawResponse, payload.id);
            Assert.Equal(documentUri, response.Uri);
            Assert.Equal("Price * Quantity", response.Text);
        }
    }
}
