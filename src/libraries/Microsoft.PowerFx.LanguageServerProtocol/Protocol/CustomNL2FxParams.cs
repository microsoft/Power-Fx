// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Incoming LSP payload for a NL request. See <see cref="CustomProtocolNames.NL2FX"/>.
    /// </summary>
    public class CustomNL2FxParams : IHasTextDocument
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
    /// Response for a <see cref="CustomNL2FxParams"/> event.
    /// </summary>
    public class CustomNL2FxResult
    {
        /// <summary>
        /// Possible expression.
        /// Maybe 0 length. 
        /// </summary>
        public CustomNL2FxResultItem[] Expressions { get; set; }        
    }

    /// <summary>
    /// Result of an NL2Fx opereation. 
    /// This is an object so we can include future information like ranking, model version, etc. 
    /// </summary>
    public class CustomNL2FxResultItem
    {
        /// <summary>
        /// A power fx expression.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Identification (such as name/version) for model that produced the result. 
        /// </summary>
        public string ModelId { get; set; }
    }
}
