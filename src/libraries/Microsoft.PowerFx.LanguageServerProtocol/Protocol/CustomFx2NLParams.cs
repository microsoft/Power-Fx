// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Incoming LSP payload for a NL request. See <see cref="CustomProtocolNames.FX2NL"/>.
    /// </summary>
    public class CustomFx2NLParams : BaseNLParams, IHasTextDocument
    {
        /// <summary>
        /// The document that was opened. Just need Uri. 
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }

        /// <summary>
        /// Existing Power Fx Expression.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Optional range within <see cref="Expression"></see> to explain.
        /// If missing, then explain the whole expression.
        /// Eg: [0,0,0,6] >> start line 0, start character 0, end line 0, end character 6.
        /// </summary>
        public uint[] Range { get; set; }
    }

    /// <summary>
    /// Response for a <see cref="CustomNL2FxParams"/> event.
    /// </summary>
    public class CustomFx2NLResult : BaseNLResult
    {
        /// <summary>
        /// A natural-language sentence explaining the incoming expression.
        /// </summary>
        public string Explanation { get; set; }

        /// <summary>
        /// Identification (such as name/version) for model that produced the result. 
        /// </summary>
        public string ModelId { get; set; }
    }
}
