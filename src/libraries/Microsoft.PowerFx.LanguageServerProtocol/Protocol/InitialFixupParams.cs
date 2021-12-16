// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class InitialFixupParams
    {
        /// <summary>
        /// The document that was opened.
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }
    }
}
