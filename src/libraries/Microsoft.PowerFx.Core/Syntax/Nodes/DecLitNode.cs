// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Decimal literal parse node. Example:
    /// 
    /// <code>12.34</code>
    /// </summary>
    public sealed class DecLitNode : TexlNode
    {
        // If Value is non-null, then the token represents its value.
        // Otherwise, the value is in DecValue.
        internal readonly decimal DecValue;

        /// <summary>
        /// The numeric value of the node.
        /// </summary>
        public decimal ActualDecValue => Value?.Value ?? DecValue;

        internal DecLitNode(ref int idNext, DecLitToken tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            DecValue = 0;
        }

        internal DecLitNode(ref int idNext, Token tok, SourceList sourceList, decimal value)
            : base(ref idNext, tok, sourceList)
        {
            Contracts.Assert(tok.Kind != TokKind.DecLit);
            DecValue = value;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            if (Value == null)
            {
                return new DecLitNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, null), DecValue);
            }

            return new DecLitNode(ref idNext, Value.Clone(ts).As<DecLitToken>());
        }

        // This may be null, in which case, DecValue should be used.
        internal DecLitToken Value => Token as DecLitToken;

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.DecLit;

        internal override DecLitNode AsDecLit()
        {
            return this;
        }
    }
}
