// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal abstract class NameNode : TexlNode
    {
        protected NameNode(ref int idNext, Token primaryToken, SourceList sourceList)
            : base(ref idNext, primaryToken, sourceList)
        {
        }

        public override Span GetCompleteSpan()
        {
            if (SourceList.Tokens.Count() == 0)
            {
                return base.GetCompleteSpan();
            }

            var start = SourceList.Tokens.First().Span.Min;
            var end = SourceList.Tokens.Last().Span.Lim;
            return new Span(start, end);
        }
    }
}