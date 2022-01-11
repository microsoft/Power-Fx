// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Core.IR
{
    internal abstract class IRNodeVisitor<TResult, TContext>
    {
        public abstract TResult Visit(TextLiteralNode node, TContext context);

        public abstract TResult Visit(NumberLiteralNode node, TContext context);

        public abstract TResult Visit(BooleanLiteralNode node, TContext context);

        public abstract TResult Visit(ColorLiteralNode node, TContext context);

        public abstract TResult Visit(TableNode node, TContext context);

        public abstract TResult Visit(RecordNode node, TContext context);

        public abstract TResult Visit(ErrorNode node, TContext context);

        public abstract TResult Visit(LazyEvalNode node, TContext context);

        public abstract TResult Visit(CallNode node, TContext context);

        public abstract TResult Visit(BinaryOpNode node, TContext context);

        public abstract TResult Visit(UnaryOpNode node, TContext context);

        public abstract TResult Visit(ScopeAccessNode node, TContext context);

        public abstract TResult Visit(RecordFieldAccessNode node, TContext context);

        public abstract TResult Visit(ResolvedObjectNode node, TContext context);

        public abstract TResult Visit(SingleColumnTableAccessNode node, TContext context);

        public abstract TResult Visit(ChainingNode node, TContext context);

        public abstract TResult Visit(AggregateCoercionNode node, TContext context);
    }
}
