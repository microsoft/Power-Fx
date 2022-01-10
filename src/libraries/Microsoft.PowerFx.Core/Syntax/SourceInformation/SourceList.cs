// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.SourceInformation
{
    /// <summary>
    /// A complete list of the source for a given TexlNode, given by a
    /// heterogeneous list of individual pieces of source.
    /// </summary>
    internal class SourceList
    {
        /// <summary>
        /// All the pieces of source for the holding TexlNode.
        /// </summary>
        public IEnumerable<ITexlSource> Sources { get; }

        /// <summary>
        /// Every node that makes up the holding TexlNode.
        /// </summary>
        public IEnumerable<Token> Tokens => Sources.SelectMany(source => source.Tokens);

        public SourceList(params ITexlSource[] items)
        {
            Contracts.AssertValue(items);
            Contracts.AssertAllValues(items);
            Sources = items.SelectMany(item => item.Sources).ToArray();
        }

        public SourceList(IEnumerable<ITexlSource> items)
        {
            Contracts.AssertValue(items);
            Contracts.AssertAllValues(items);
            Sources = items.SelectMany(item => item.Sources).ToArray();
        }

        public SourceList(Token token)
        {
            Contracts.AssertValue(token);
            Sources = new[] { new TokenSource(token) };
        }

        public SourceList Clone(Span span, Dictionary<TexlNode, TexlNode> newNodes)
        {
            Contracts.AssertValue(newNodes);
            Contracts.AssertAllValues(newNodes.Values);
            Contracts.AssertAllValues(newNodes.Keys);
            var newItems = new ITexlSource[Sources.Count()];
            var i = 0;
            foreach (var source in Sources)
            {
                newItems[i] = source.Clone(newNodes, span);
                i += 1;
            }
            return new SourceList(newItems);
        }
    }
}
