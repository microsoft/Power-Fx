// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax.SourceInformation;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Syntax
{
    public sealed class UnitsLitNode : TexlNode
    {
        internal UnitInfo UnitInfo;

        internal UnitsLitNode(ref int idNext, Token tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            UnitInfo = null;
        }

        internal UnitsLitNode(ref int idNext, Token tok, SourceList sourceList, UnitInfo unitInfo)
            : base(ref idNext, tok, sourceList)
        {
            UnitInfo = unitInfo;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new UnitsLitNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, null), UnitInfo);
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
        public override NodeKind Kind => NodeKind.Units;
    }
}
