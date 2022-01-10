// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class ParentNode : NameNode
    {
        public ParentNode(ref int idNext, Token tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
        }

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        public override Result Accept<Result, Context>(TexlFunctionalVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            return new ParentNode(ref idNext, Token.Clone(ts));
        }

        public override NodeKind Kind => NodeKind.Parent;

        public override ParentNode AsParent()
        {
            return this;
        }
    }
}