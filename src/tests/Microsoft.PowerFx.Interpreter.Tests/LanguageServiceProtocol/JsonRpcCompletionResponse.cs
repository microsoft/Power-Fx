// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class JsonRpcCompletionResponse
    {
        public string Jsonrpc { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public CompletionResult Result { get; set; } = new CompletionResult();
    }
}
