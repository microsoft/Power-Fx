// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class SemanticTokensRangeParams : SemanticTokensParams
    {
        public Range Range { get; set; }
    }
}
