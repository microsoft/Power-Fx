// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Base class to represent the request params.
    /// </summary>
    public class LanguageServerRequestBaseParams
    {
        /// <summary>
        /// The text document.
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }
    }
}
