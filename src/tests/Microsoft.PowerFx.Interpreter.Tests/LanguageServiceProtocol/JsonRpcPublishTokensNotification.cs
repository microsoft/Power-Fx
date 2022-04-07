// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class JsonRpcPublishTokensNotification
    {
        public string Jsonrpc { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public PublishTokensParams Params { get; set; } = new PublishTokensParams();
    }
}
