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
    public class CustomNL2FxParams : BaseNLParams, IHasTextDocument
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
    /// Result of an NL2Fx operation. 
    /// </summary>
    public class CustomNL2FxResultItem
    {
        /// <summary>
        /// A power fx expression.
        /// This should be valid and LSP verified it has no errors. 
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// The expression from the model, prior to any client-side LSP filtering.
        /// This can be useful for diagnostics. 
        /// </summary>
        public string RawExpression { get; set; }

        /// <summary>
        /// Identification (such as name/version) for model that produced the result. 
        /// </summary>
        public string ModelId { get; set; }
    }
}
