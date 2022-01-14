// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    /// <summary>
    /// A base visitor for when you want a default result for most nodes.
    /// </summary>
    internal abstract class DefaultVisitor<TResult, TContext> : TexlFunctionalVisitor<TResult, TContext>
    {
        public virtual TResult Default { get; }

        public DefaultVisitor(TResult defaultValue)
        {
            Default = defaultValue;
        }

        public override TResult Visit(ErrorNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(BlankNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(BoolLitNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(StrLitNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(NumLitNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(FirstNameNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(ParentNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(SelfNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(ReplaceableNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(DottedNameNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(UnaryOpNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(BinaryOpNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(VariadicOpNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(CallNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(ListNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(RecordNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(TableNode node, TContext context)
        {
            return Default;
        }

        public override TResult Visit(AsNode node, TContext context)
        {
            return Default;
        }
    }
}
