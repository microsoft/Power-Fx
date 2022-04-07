// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class VersionedTextDocumentIdentifier : TextDocumentIdentifier
    {
        /// <summary>
        /// The version number of this document.
        ///
        /// The version number of a document will increase after each change,
        /// including undo/redo. The number doesn't need to be consecutive.
        /// </summary>
        public int Version { get; set; }
    }
}
