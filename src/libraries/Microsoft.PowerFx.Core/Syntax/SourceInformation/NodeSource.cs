// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.SourceInformation
{
    /// <summary>
    /// A recursive reference to another node as a piece of the source for 
    /// its parent node.
    /// </summary>
    internal class NodeSource : ITexlSource
    {
        public TexlNode Node { get; }

        public IEnumerable<Token> Tokens => Node.SourceList.Tokens;

        public IEnumerable<ITexlSource> Sources => new[] { this };

        public NodeSource(TexlNode node)
        {
            Contracts.AssertValue(node);
            Node = node;
        }

        public ITexlSource Clone(Dictionary<TexlNode, TexlNode> newNodes, Span span)
        {
            Contracts.AssertAllValues(newNodes.Keys);
            Contracts.AssertAllValues(newNodes.Values);
            Contracts.AssertValue(newNodes);

            return new NodeSource(newNodes[Node]);
        }

        public override string ToString()
        {
            return Node.ToString();
        }
    }
}
