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
        public string MarkupKind = "markdown"; // "plaintext"

        public string Value { get; set; }
    }
}
