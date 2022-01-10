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
    /// A utility class for spreading a list of sources within a SourceList.
    /// This is immediately removed and used for its contents in the 
    /// construction of a SourceList or another SpreadSource.
    /// </summary>
    internal sealed class SpreadSource : ITexlSource
    {
        public IEnumerable<ITexlSource> Sources { get; }
        public IEnumerable<Token> Tokens => Sources.SelectMany(source => source.Tokens);

        public SpreadSource(IEnumerable<ITexlSource> sources)
        {
            Contracts.AssertValue(sources);
            Contracts.AssertAllValues(sources);
            Sources = sources.SelectMany(source => source.Sources);
        }

        public SpreadSource(params ITexlSource[] sources)
        {
            Contracts.AssertValue(sources);
            Contracts.AssertAllValues(sources);
            Sources = sources.SelectMany(source => source.Sources);
        }

        public ITexlSource Clone(Dictionary<TexlNode, TexlNode> newNodes, Span newSpan)
        {
            Contracts.AssertValue(newNodes);
            Contracts.AssertAllValues(newNodes.Values);
            Contracts.AssertAllValues(newNodes.Keys);
            var newItems = new ITexlSource[Sources.Count()];
            var i = 0;
            foreach (var source in Sources)
            {
                newItems[i] = source.Clone(newNodes, newSpan);
                i += 1; ;
            }
            return new SpreadSource(newItems);
        }

        public override string ToString()
        {
            return string.Join("", Sources);
        }
    }
}
