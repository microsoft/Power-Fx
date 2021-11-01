// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class JsonRpcInitialFixupResponse
    {
        public string Jsonrpc { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public TextDocumentItem Result { get; set; } = new TextDocumentItem();
    }
}
