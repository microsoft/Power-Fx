// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class DidChangeTextDocumentParams
    {
        /// <summary>
        /// The document that did change.The version number points
        /// to the version after all provided content changes have
        /// been applied.
        /// </summary>
        public VersionedTextDocumentIdentifier TextDocument { get; set; } = new VersionedTextDocumentIdentifier();

        /// <summary>
        /// The actual content changes.
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "n/a")]
        public TextDocumentContentChangeEvent[] ContentChanges { get; set; } = Array.Empty<TextDocumentContentChangeEvent>();
    }
}
