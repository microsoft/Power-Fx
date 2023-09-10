// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class CustomNLParams
    {
        /// <summary>
        /// The document that was opened. Just need Uri. 
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }

        /// <summary>
        /// Sentence in Natural Language.
        /// </summary>
        public string Sentence { get; set; }
    }

    /// <summary>
    /// Response for a <see cref="CustomNLParams"/> event.
    /// </summary>
    public class CustomNLResult
    {
        /// <summary>
        /// Possible expression.
        /// Maybe 0 length. 
        /// </summary>
        public string[] Expressions { get; set; }
    }

    public class CustomNLRequest
    {
        public string Sentence { get; set; }

        // Current symbols to pass into NL prompt 
        public ReadOnlySymbolTable Symbols { get; set; }
    }
}
