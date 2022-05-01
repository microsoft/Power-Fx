﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Error parse node.
    /// </summary>
    public sealed class ErrorNode : TexlNode
    {
        /// <summary>
        /// The error message of the node.
        /// </summary>
        public string Message { get; }

        internal readonly object[] Args;

        internal ErrorNode(ref int idNext, Token primaryToken, string msg)
            : base(ref idNext, primaryToken, new SourceList(primaryToken))
        {
            Message = msg;
            Args = null;
        }

        internal ErrorNode(ref int idNext, Token primaryToken, string msg, params object[] args)
            : base(ref idNext, primaryToken, new SourceList(primaryToken))
        {
            Message = msg;
            Args = args;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new ErrorNode(ref idNext, Token.Clone(ts), Message, Args);
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
        public override NodeKind Kind => NodeKind.Error;

        internal override ErrorNode AsError()
        {
            return this;
        }
    }
}
