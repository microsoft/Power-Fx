// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public partial class LanguageServerTestBase
    {
        // Ensure the disclaimer shows up in a signature.
        [Fact]
        public async Task TestSignatureDisclaimers()
        {
            // Arrange
            var text = "AISummarize(";
            var signatureHelpParams1 = new SignatureHelpParams
            {
                TextDocument = GetTextDocument(GetUri("expression=" + text)),
                Text = text,
                Position = GetPosition(text.Length),
                Context = GetSignatureHelpContext("(")
            };

            Init(new InitParams(options: GetParserOptions(false)));

            // test good formula
            var payload = GetSignatureHelpPayload(signatureHelpParams1);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            var response = AssertAndGetResponsePayload<SignatureHelp>(rawResponse, payload.id);
            var sig = response.Signatures.Single();
            Assert.Equal("AISummarize()", sig.Label);

            var je = (JsonElement)sig.Documentation;
            var markdown = JsonSerializer.Deserialize<MarkupContent>(je.ToString(), LanguageServerHelper.DefaultJsonSerializerOptions);
            Assert.Equal("markdown", markdown.Kind);
            Assert.StartsWith("Create and set a global variable", markdown.Value); // function's normal description 
            Assert.Contains("**Disclaimer:** AI-generated content", markdown.Value); // disclaimer appended. 
        }

        [Theory]
        [InlineData("Power(", 6, "Power(2,", 8, false)]
        [InlineData("Behavior(); Power(", 18, "Behavior(); Power(2,", 20, true)]

        // This tests generates an internal error as we use an behavior function but we have no way to check its presence
        [InlineData("Behavior(); Power(", 18, "Behavior(); Power(2,", 20, false)]
        public async Task TestSignatureHelpWithExpressionInUriAndNotInUri(string text, int offset, string text2, int offset2, bool withAllowSideEffects)
        {
            foreach (var addExprToUri in new bool[] { true, false })
            {
                var signatureHelpParams1 = new SignatureHelpParams
                {
                    TextDocument = addExprToUri ? GetTextDocument(GetUri("expression=" + text)) : GetTextDocument(),
                    Text = addExprToUri ? null : text,
                    Position = GetPosition(offset),
                    Context = GetSignatureHelpContext("(")
                };
                var signatureHelpParams2 = new SignatureHelpParams
                {
                    TextDocument = addExprToUri ? GetTextDocument(GetUri("expression=" + text2)) : GetTextDocument(),
                    Text = addExprToUri ? null : text2,
                    Position = GetPosition(offset2),
                    Context = GetSignatureHelpContext(",")
                };
                await TestSignatureHelpCore(signatureHelpParams1, signatureHelpParams2, withAllowSideEffects).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task TestSignatureHelpWithExpressionInBothUriAndTextDocument()
        {
            var signatureHelpParams1 = new SignatureHelpParams
            {
                TextDocument = GetTextDocument(GetUri("expression=Max(")),
                Text = "Power(",
                Position = GetPosition(6),
                Context = GetSignatureHelpContext("(")
            };
            var signatureHelpParams2 = new SignatureHelpParams
            {
                TextDocument = GetTextDocument(GetUri("expression=Max(")),
                Text = "Behavior(); Power(2,",
                Position = GetPosition(20),
                Context = GetSignatureHelpContext(",")
            };
            await TestSignatureHelpCore(signatureHelpParams1, signatureHelpParams2, true).ConfigureAwait(false);
        }

        private async Task TestSignatureHelpCore(SignatureHelpParams signatureHelpParams1, SignatureHelpParams signatureHelpParams2, bool withAllowSideEffects)
        {
            Init(new InitParams(options: GetParserOptions(withAllowSideEffects)));

            // test good formula
            var payload = GetSignatureHelpPayload(signatureHelpParams1);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            var response = AssertAndGetResponsePayload<SignatureHelp>(rawResponse, payload.id);
            Assert.Equal(0U, response.ActiveSignature);
            Assert.Equal(0U, response.ActiveParameter);
            var foundItems = response.Signatures.Where(item => item.Label.StartsWith("Power"));
            Assert.True(Enumerable.Count(foundItems) >= 1, "Power should be found from signatures result");
            Assert.Equal(2, foundItems.First().Parameters.Length);
            Assert.Equal("base", foundItems.First().Parameters[0].Label);
            Assert.Equal("exponent", foundItems.First().Parameters[1].Label);

            payload = GetSignatureHelpPayload(signatureHelpParams2);
            rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            response = AssertAndGetResponsePayload<SignatureHelp>(rawResponse, payload.id);
            Assert.Equal(0U, response.ActiveSignature);
            Assert.Equal(1U, response.ActiveParameter);
            foundItems = response.Signatures.Where(item => item.Label.StartsWith("Power"));
            Assert.True(Enumerable.Count(foundItems) >= 1, "Power should be found from signatures result");
            Assert.Equal(2, foundItems.First().Parameters.Length);
            Assert.Equal("base", foundItems.First().Parameters[0].Label);
            Assert.Equal("exponent", foundItems.First().Parameters[1].Label);

            // missing 'expression' in documentUri
            payload = GetSignatureHelpPayload(new SignatureHelpParams()
            {
                Context = GetSignatureHelpContext("("),
                TextDocument = GetTextDocument("powerfx://test"),
                Position = GetPosition(0),
            });
            var errorResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            AssertErrorPayload(errorResponse, payload.id, JsonRpcHelper.ErrorCode.InvalidParams);
        }

        private static (string payload, string id) GetSignatureHelpPayload(SignatureHelpParams signatureHelpParams)
        {
            return GetRequestPayload(signatureHelpParams, TextDocumentNames.SignatureHelp);
        }

        private static SignatureHelpContext GetSignatureHelpContext(string triggerChar)
        {
            return new SignatureHelpContext
            {
                TriggerKind = SignatureHelpTriggerKind.TriggerCharacter,
                TriggerCharacter = triggerChar
            };
        }
    }
}
