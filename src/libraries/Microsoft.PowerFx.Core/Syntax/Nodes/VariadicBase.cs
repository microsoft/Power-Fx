// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    /// <summary>
    /// Base class for all variadic (i.e., with variable number of children) parse nodes.
    /// </summary>
    public abstract class VariadicBase : TexlNode
    {
        /// <summary>
        /// The list of child nodes.
        /// </summary>
        public IReadOnlyList<TexlNode> ChildNodes => Children;

        internal readonly TexlNode[] Children;

        // Takes ownership of the array.
        private protected VariadicBase(ref int idNext, Token primaryToken, SourceList sourceList, TexlNode[] children)
            : base(ref idNext, primaryToken, sourceList)
        {
            Contracts.AssertValue(children);
            Children = children;

            var maxDepth = 0;

            foreach (var child in children)
            {
                Contracts.AssertValue(child);
                child.Parent = this;
                if (maxDepth < child.Depth)
                {
                    maxDepth = child.Depth;
                }

                if (MinChildID > child.MinChildID)
                {
                    MinChildID = child.MinChildID;
                }
            }

            _depth = maxDepth + 1;
        }

        internal TexlNode[] CloneChildren(ref int idNext, Span ts)
        {
            var clones = new TexlNode[Children.Length];
            for (var x = 0; x < clones.Length; x++)
            {
                clones[x] = Children[x].Clone(ref idNext, ts);
            }

            return clones;
        }

        internal static Token[] Clone(Token[] toks, Span ts)
        {
            Contracts.AssertValueOrNull(toks);
            if (toks == null)
            {
                return null;
            }

            var newToks = new Token[toks.Length];
            for (var x = 0; x < toks.Length; x++)
            {
                newToks[x] = toks[x].Clone(ts);
            }

            return newToks;
        }

        public int Count => Children.Length;

        public void AcceptChildren(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            foreach (var child in Children)
            {
                Contracts.AssertValue(child);
                child.Accept(visitor);
            }
        }

        /// <inheritdoc />
        public override Span GetCompleteSpan()
        {
            if (Children.Count() == 0)
            {
                return new Span(Token.VerifyValue().Span.Min, Token.VerifyValue().Span.Lim);
            }

            return new Span(Children.VerifyValue().First().VerifyValue().GetCompleteSpan().Min, Children.VerifyValue().Last().VerifyValue().GetCompleteSpan().Lim);
        }
    }
}
