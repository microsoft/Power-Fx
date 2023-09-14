// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Incoming LSP payload for a capabilities request.
    /// See <see cref="CustomProtocolNames.GetCapabilities"/>.
    /// </summary>
    public class CustomGetCapabilitiesParams
    {
        /// <summary>
        /// The document that was opened. Just need Uri. 
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }        
    }

    /// <summary>
    /// Response for a <see cref="CustomGetCapabilitiesParams"/> event.
    /// Describes addtiional features available from this endpoint. 
    /// </summary>
    public class CustomGetCapabilitiesResult
    {
        /// <summary>
        /// Supports the $/nl2fx message. 
        /// This is determined by the <see cref="NLHandler"/> registered with the language server. 
        /// </summary>
        public bool SupportsNL2Fx { get; set; }

        /// <summary>
        /// Supports the $/fx2nl message. 
        /// </summary>
        public bool SupportsFx2NL { get; set; }
    }
}
