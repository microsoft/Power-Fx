﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    /// <summary>
    /// Wraps an child IR Node that is evaluated on-demand by the LazyEval node's parent.
    /// </summary>
    internal sealed class LazyEvalNode : IntermediateNode
    {
        public readonly IntermediateNode Child;

        public LazyEvalNode(IRContext irContext, IntermediateNode wrapped)
            : base(irContext)
        {
            Contracts.AssertValue(wrapped);

            Child = wrapped;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context, StackMarker stackMarker)
        {
            return visitor.Visit(this, context, stackMarker);
        }

        public override string ToString()
        {
            return $"Lazy({Child})";
        }
    }
}
