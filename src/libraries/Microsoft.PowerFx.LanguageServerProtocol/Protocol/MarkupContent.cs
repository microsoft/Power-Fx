// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Github flavored Markdown string.
    /// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#markupContent.
    /// </summary>
    public class MarkupContent
    {
        /// <summary>
        /// The type of markup.  Either 'markdown' or 'plaintext'.
        /// </summary>
        public string Kind { get; set; } = "markdown"; // "plaintext"

        /// <summary>
        /// The content itself.
        /// </summary>
        public string Value { get; set; }
    }
}
