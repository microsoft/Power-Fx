// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Syntax.SourceInformation
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
            Sources = Flatten(items);
        }

        public SourceList(IEnumerable<ITexlSource> items)
        {
            Contracts.AssertValue(items);
            Contracts.AssertAllValues(items);
            Sources = Flatten(items as IReadOnlyList<ITexlSource> ?? items.ToArray());
        }

        // Flattens one level of sources. Every source other than SpreadSource is its own
        // single source, so this avoids the per-leaf "new[] { this }" allocations and the
        // LINQ SelectMany/array-builder churn that dominated parse-time allocations.
        private static ITexlSource[] Flatten(IReadOnlyList<ITexlSource> items)
        {
            var hasSpread = false;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] is SpreadSource)
                {
                    hasSpread = true;
                    break;
                }
            }

            if (!hasSpread)
            {
                var flat = new ITexlSource[items.Count];
                for (var i = 0; i < items.Count; i++)
                {
                    flat[i] = items[i];
                }

                return flat;
            }

            var result = new List<ITexlSource>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item is SpreadSource)
                {
                    result.AddRange(item.Sources);
                }
                else
                {
                    result.Add(item);
                }
            }

            return result.ToArray();
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
