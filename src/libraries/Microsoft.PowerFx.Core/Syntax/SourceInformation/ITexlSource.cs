// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.SourceInformation
{
    /// <summary>
    /// An individual piece of the source that should be associated with a given
    /// TexlNode.
    /// </summary>
    internal interface ITexlSource
    {
        /// <summary>
        /// Clones the contents of this piece of the source.
        /// </summary>
        /// <param name="newNodes">
        /// A mapping from the old nodes in this piece of the source to the
        /// new cloned ones. Must be complete.
        /// </param>
        /// <param name="span"></param>
        ITexlSource Clone(Dictionary<TexlNode, TexlNode> newNodes, Span span);

        /// <summary>
        /// All of the tokens within this piece of the source.
        /// </summary>
        IEnumerable<Token> Tokens { get; }

        /// <summary>
        /// All the pieces of source within this. This should only really be
        /// used by the SourceList system, as it's used to make handling the
        /// SpreadSource easier.
        /// </summary>
        IEnumerable<ITexlSource> Sources { get; }
    }
}
