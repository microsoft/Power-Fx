﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class BlankNode : TexlNode
    {
        public override NodeKind Kind => NodeKind.Blank;

        public BlankNode(ref int idNext, Token primaryToken)
            : base(ref idNext, primaryToken, new SourceList(primaryToken))
        {
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            return new BlankNode(ref idNext, Token.Clone(ts));
        }

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override BlankNode AsBlank()
        {
            return this;
        }
    }
}
