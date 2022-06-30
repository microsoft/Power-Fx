// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// A base visitor for returning results that can be easily aggregated (lists, booleans, sums).
    /// </summary>
    internal abstract class AggregateVisitor<TResult, TContext> : DefaultVisitor<TResult, TContext>
    {
        public AggregateVisitor(TResult defaultValue)
            : base(defaultValue)
        {
        }

        protected abstract TResult Aggregate(IEnumerable<TResult> results);

        public override TResult Visit(DottedNameNode node, TContext context)
        {
            return node.Left.Accept(this, context);
        }

        public override TResult Visit(UnaryOpNode node, TContext context)
        {
            return node.Child.Accept(this, context);
        }

        public override TResult Visit(BinaryOpNode node, TContext context)
        {
            return Aggregate(Lazily(() => node.Left.Accept(this, context), () => node.Right.Accept(this, context)));
        }

        public override TResult Visit(VariadicOpNode node, TContext context)
        {
            return Aggregate(node.Children.Select(child => child.Accept(this, context)));
        }

        public override TResult Visit(CallNode node, TContext context)
        {
            return node.Args.Accept(this, context);
        }

        public override TResult Visit(ListNode node, TContext context)
        {
            return Aggregate(node.Children.Select(child => child.Accept(this, context)));
        }

        public override TResult Visit(RecordNode node, TContext context)
        {
            return Aggregate(node.Children.Select(child => child.Accept(this, context)));
        }

        public override TResult Visit(TableNode node, TContext context)
        {
            return Aggregate(node.Children.Select(child => child.Accept(this, context)));
        }

        public override TResult Visit(AsNode node, TContext context)
        {
            return node.Left.Accept(this, context);
        }

        private static IEnumerable<TResult> Lazily(params Func<TResult>[] actions)
        {
            foreach (var action in actions)
            {
                yield return action();
            }
        }
    }
}
