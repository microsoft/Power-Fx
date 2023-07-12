// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class TextDocumentItem : TextDocumentIdentifier
    {
        public TextDocumentItem()
            : base()
        {
            LanguageId = string.Empty;
            Text = null;
        }

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
