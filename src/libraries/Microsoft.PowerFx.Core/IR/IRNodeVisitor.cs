// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Core.IR
{
    internal abstract class IRNodeVisitor<TResult, TContext>
    {
        public abstract TResult Visit(TextLiteralNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(NumberLiteralNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(BooleanLiteralNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(ColorLiteralNode node, TContext context, StackMarker stackMarker);        

        public abstract TResult Visit(RecordNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(ErrorNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(LazyEvalNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(CallNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(BinaryOpNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(UnaryOpNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(ScopeAccessNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(RecordFieldAccessNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(ResolvedObjectNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(SingleColumnTableAccessNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(ChainingNode node, TContext context, StackMarker stackMarker);

        public abstract TResult Visit(AggregateCoercionNode node, TContext context, StackMarker stackMarker);
    }
}
