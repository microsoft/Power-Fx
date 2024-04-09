// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    internal class TestGetCustomCapibilitiesHandler : NLHandler
    {
        public override bool SupportsFx2NL { get; }

        public override bool SupportsNL2Fx { get; }

        public TestGetCustomCapibilitiesHandler(bool supportsFx2NL, bool supportsNL2Fx)
        {
            SupportsFx2NL = supportsFx2NL;
            SupportsNL2Fx = supportsNL2Fx;
        }
    }

    public partial class LanguageServerTestBase
    {
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, false, true)]
        public async Task TestGetCapabilities(bool supportNL2Fx, bool supportFx2NL, bool dontRegister = false)
        {
            // Arrange
            var documentUri = "powerfx://app";
            var testNLHandler = new TestGetCustomCapibilitiesHandler(supportFx2NL, supportNL2Fx);
            if (dontRegister)
            {
                testNLHandler = null;
            }

            HandlerFactory.SetHandler(CustomProtocolNames.GetCapabilities, new BackwardsCompatibleGetCustomCapabilitiesLanguageServerOperationHandler(new BackwardsCompatibleNLHandlerFactory(testNLHandler)));
            var payload = GetRequestPayload(
            new CustomGetCapabilitiesParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = documentUri,
                    LanguageId = "powerfx",
                    Version = 1
                }
            }, CustomProtocolNames.GetCapabilities);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);

            // Assert: result has expected concat with symbols. 
            var response = AssertAndGetResponsePayload<CustomGetCapabilitiesResult>(rawResponse, payload.id);
            Assert.Equal(supportNL2Fx, response.SupportsNL2Fx);
            Assert.Equal(supportFx2NL, response.SupportsFx2NL);
        }
    }
}
