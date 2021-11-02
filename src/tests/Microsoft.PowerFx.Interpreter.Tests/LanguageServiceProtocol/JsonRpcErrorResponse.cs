// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class JsonRpcErrorResponse
    {
        public string Jsonrpc { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public JsonRpcError Error { get; set; } = new JsonRpcError();
    }
}
