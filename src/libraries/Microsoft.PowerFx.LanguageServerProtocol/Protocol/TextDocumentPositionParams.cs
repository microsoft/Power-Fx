﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class TextDocumentPositionParams : BaseParams
    {
        public TextDocumentPositionParams()
        {
            TextDocument = new TextDocumentItem();
            Position = new Position();
        }

        /// <summary>
        /// The position inside the text document.
        /// </summary>
        public Position Position { get; set; }
    }
}
