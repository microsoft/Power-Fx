// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Incoming LSP payload for a NL request. See <see cref="CustomProtocolNames.FX2NL"/>.
    /// </summary>
    public class CustomFx2NLParams : IHasTextDocument
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
