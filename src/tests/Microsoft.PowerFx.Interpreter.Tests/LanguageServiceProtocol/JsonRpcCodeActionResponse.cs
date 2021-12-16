// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class JsonRpcCodeActionResponse
    {
        public string Jsonrpc { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public Dictionary<string, CodeAction[]> Result { get; set; } = new Dictionary<string, CodeAction[]>();
    }
}
