// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    /// <summary>
    /// Variadic operation node. Example:
    /// 
    /// <code>Formula1 ; Formula2 ; ...</code>
    /// </summary>
    public sealed class VariadicOpNode : VariadicBase
    {
        /// <summary>
        /// Variadic operator.
        /// </summary>
        public VariadicOp Op { get; }
        
        internal readonly Token[] OpTokens;

        // Assumes ownership of the 'children' and 'opTokens' array.
        internal VariadicOpNode(ref int idNext, VariadicOp op, TexlNode[] children, Token[] opTokens, SourceList sourceList)
            : base(ref idNext, opTokens.VerifyValue().First(), sourceList, children)
        {
            Contracts.AssertNonEmpty(opTokens);
            Contracts.AssertAllValues(opTokens);
            Op = op;
            OpTokens = opTokens;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var children = CloneChildren(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>();
            for (var i = 0; i < Children.Length; ++i)
            {
                newNodes.Add(Children[i], children[i]);
            }

            return new VariadicOpNode(ref idNext, Op, children, Clone(OpTokens, ts), SourceList.Clone(ts, newNodes));
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                AcceptChildren(visitor);
                visitor.PostVisit(this);
            }
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.VariadicOp;

        internal override VariadicOpNode AsVariadicOp()
        {
            return this;
        }
    }
}
