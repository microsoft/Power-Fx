// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class TextDocumentPositionParams : LanguageServerRequestBaseParams
    {
        public TextDocumentPositionParams()
            : base()
        {
            Position = new Position();
        }

        /// <summary>
        /// The position inside the text document.
        /// </summary>
        public Position Position { get; set; }
    }
}
