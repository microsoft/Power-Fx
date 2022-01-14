// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal abstract class TexlFunctionalVisitor<TResult, TContext>
    {
        // Visit methods for leaf node types.
        public abstract TResult Visit(ErrorNode node, TContext context);

        public abstract TResult Visit(BlankNode node, TContext context);

        public abstract TResult Visit(BoolLitNode node, TContext context);

        public abstract TResult Visit(StrLitNode node, TContext context);

        public abstract TResult Visit(NumLitNode node, TContext context);

        public abstract TResult Visit(FirstNameNode node, TContext context);

        public abstract TResult Visit(ParentNode node, TContext context);

        public abstract TResult Visit(SelfNode node, TContext context);

        public abstract TResult Visit(ReplaceableNode node, TContext context);

        // Visit methods for non-leaf node types.
        // If PreVisit returns true, the children are visited and PostVisit is called.
        public abstract TResult Visit(DottedNameNode node, TContext context);

        public abstract TResult Visit(UnaryOpNode node, TContext context);

        public abstract TResult Visit(BinaryOpNode node, TContext context);

        public abstract TResult Visit(VariadicOpNode node, TContext context);

        public abstract TResult Visit(CallNode node, TContext context);

        public abstract TResult Visit(ListNode node, TContext context);

        public abstract TResult Visit(RecordNode node, TContext context);

        public abstract TResult Visit(TableNode node, TContext context);

        public abstract TResult Visit(AsNode node, TContext context);
    }
}
