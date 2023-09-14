// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    // This message has an TextDocumentItem, which can be used to get a IPowerFxScope. 
    internal interface IHasTextDocument
    {
        public TextDocumentItem TextDocument { get; set; }
    }

    /// <summary>
    /// Incoming LSP payload for a NL request. See <see cref="CustomProtocolNames.FX2NL"/>.
    /// </summary>
    public class CustomFx2NLParams
    {
        /// <summary>
        /// The document that was opened. Just need Uri. 
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }

        /// <summary>
        /// Existing Power Fx Expression.
        /// </summary>
        public string Expression { get; set; }
    }

    /// <summary>
    /// Response for a <see cref="CustomNL2FxParams"/> event.
    /// </summary>
    public class CustomFx2NLResult
    {
        /// <summary>
        /// A natural-language sentence explaining the incoming expression.
        /// </summary>
        public string Explanation { get; set; }
    }
}
