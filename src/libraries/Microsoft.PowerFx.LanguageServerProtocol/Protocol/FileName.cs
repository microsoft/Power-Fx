// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Incoming LSP payload for a NL request. 
    /// </summary>
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

    /// <summary>
    /// Resolved from <see cref="CustomNLParams"/>.
    /// </summary>
    // $$$ Move to other file. 
    public class CustomNLRequest
    {
        public string Sentence { get; set; }

        // Current symbols to pass into NL prompt 
        public CheckContextSummary SymbolSummary { get; set; }
    }

    public class NLHandler
    {
        public virtual Task<CustomNLResult> NL2Fx(CustomNLRequest request, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }
    }
}
