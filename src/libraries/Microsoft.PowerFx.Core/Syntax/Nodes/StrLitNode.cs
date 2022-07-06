// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// String literal parse node. Example:
    /// 
    /// <code>"Hello world"</code>
    /// </summary>
    public sealed class StrLitNode : TexlNode
    {
        /// <summary>
        /// The string value of the literal.
        /// </summary>
        public string Value { get; }

        internal StrLitNode(ref int idNext, StrLitToken tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            Value = tok.Value;
            Contracts.AssertValue(Value);
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new StrLitNode(ref idNext, Token.Clone(ts).As<StrLitToken>());
        }

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
        public override NodeKind Kind => NodeKind.StrLit;

        internal override StrLitNode AsStrLit()
        {
            return this;
        }
    }
}
