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
        /// Eg: zero-based offsets {1,6} >> 1d starting index is 1 and ending index is 6.
        /// </summary>
        public ScalarRange Range { get; set; }
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
