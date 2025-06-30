// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
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

        internal readonly IReadOnlyList<TexlNode> Children;

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
                if (child.Parent == null)
                {
                    child.Parent = this;
                }

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
            var clones = new TexlNode[Children.Count];
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

        /// <summary>
        /// Gets the number of child elements in the collection.
        /// </summary>
        public int Count => Children.Count;

        /// <summary>
        /// Invokes the specified <see cref="TexlVisitor"/> on each child node in the collection.
        /// </summary>
        /// <remarks>This method iterates through all child nodes in the <c>Children</c> collection and
        /// calls the <c>Accept</c> method on each child, passing the provided <paramref name="visitor"/>.</remarks>
        /// <param name="visitor">The <see cref="TexlVisitor"/> instance to apply to each child node. This parameter cannot be <see
        /// langword="null"/>.</param>
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
