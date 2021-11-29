// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class TextDocumentContentChangeEvent
    {
        public TextDocumentContentChangeEvent()
        {
            Text = string.Empty;
        }

        /// <summary>
        /// The new text of the whole document.
        /// </summary>
        public string Text { get; set; }
    }
}
