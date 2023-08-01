// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Base class to represent the request params.
    /// </summary>
    public class LanguageServerRequestBaseParams
    {
        public LanguageServerRequestBaseParams()
        {
            TextDocument = new TextDocumentIdentifier();
            Text = null;
        }

        /// <summary>
        /// The text document.
        /// </summary>
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Text.expression in the document.
        /// </summary>
        public string Text { get; set; }
    }
}
