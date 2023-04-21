// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    internal class SemanticTokensResponse
    {
        public string ResultId { get; set; } = null;

        public ICollection<uint> Data { get; set; } = new uint[0];
    }
}
