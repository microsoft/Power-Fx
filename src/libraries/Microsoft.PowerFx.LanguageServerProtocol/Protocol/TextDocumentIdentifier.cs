// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class TextDocumentIdentifier
    {
        public TextDocumentIdentifier()
        {
            Uri = string.Empty;
        }

        /// <summary>
        /// The text document's URI.
        /// </summary>
        public string Uri { get; set; }
    }
}
