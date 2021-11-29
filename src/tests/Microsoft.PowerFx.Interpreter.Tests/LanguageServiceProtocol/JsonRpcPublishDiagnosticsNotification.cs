// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class JsonRpcPublishDiagnosticsNotification
    {
        public string Jsonrpc { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public PublishDiagnosticsParams Params { get; set; } = new PublishDiagnosticsParams();
    }
}
