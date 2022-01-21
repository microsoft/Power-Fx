// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class SignatureHelpParams : TextDocumentPositionParams
    {
        /// <summary>
        /// The signature help context. This is only available if the client
        /// specifies to send this using the client capability
        /// `textDocument.signatureHelp.contextSupport === true`.
        /// </summary>
        public SignatureHelpContext Context { get; set; }
    }
}
