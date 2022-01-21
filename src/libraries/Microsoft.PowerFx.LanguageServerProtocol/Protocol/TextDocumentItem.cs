// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class TextDocumentItem
    {
        public TextDocumentItem()
        {
            Uri = string.Empty;
            LanguageId = string.Empty;
            Text = string.Empty;
        }

        /// <summary>
        /// The text document's URI.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The text document's language identifier.
        /// </summary>
        public string LanguageId { get; set; }

        /// <summary>
        /// The version number of this document (it will increase after each
        /// change, including undo/redo).
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The content of the opened text document.
        /// </summary>
        public string Text { get; set; }
    }
}
