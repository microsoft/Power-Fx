// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class ErrorNode : TexlNode
    {
        public readonly string Message;
        public readonly object[] Args;

        public ErrorNode(ref int idNext, Token primaryToken, string msg)
            : base(ref idNext, primaryToken, new SourceList(primaryToken))
        {
            Message = msg;
            Args = null;
        }

        public ErrorNode(ref int idNext, Token primaryToken, string msg, params object[] args)
            : base(ref idNext, primaryToken, new SourceList(primaryToken))
        {
            Contracts.AssertValue(args);
            Message = msg;
            Args = args;
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            return new ErrorNode(ref idNext, Token.Clone(ts), Message, Args);
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

        public override NodeKind Kind { get { return NodeKind.Error; } }

        public override ErrorNode AsError()
        {
            return this;
        }
    }
}