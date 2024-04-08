// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    public class RangeSemanticTokensLanguageServerOperationHandler : BaseSemanticTokensLanguageServerOperationHandler
    {
        public override string LspMethod => TextDocumentNames.RangeDocumentSemanticTokens;
    }
}
