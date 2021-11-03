// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    /// <summary>
    /// A base visitor for returning results that can be easily aggregated (lists, booleans, sums)
    /// </summary>
    internal abstract class AggregateVisitor<Result, Context> : DefaultVisitor<Result, Context>
    {
        public AggregateVisitor(Result defaultValue)
            : base(defaultValue)
        {
        }

        protected abstract Result Aggregate(IEnumerable<Result> results);

        public override Result Visit(DottedNameNode node, Context context)
        {
            return node.Left.Accept(this, context);
        }

        public override Result Visit(UnaryOpNode node, Context context)
        {
            return node.Child.Accept(this, context);
        }

        public override Result Visit(BinaryOpNode node, Context context)
        {
            return Aggregate(Lazily(() => node.Left.Accept(this, context), () => node.Right.Accept(this, context)));
        }

        public override Result Visit(VariadicOpNode node, Context context)
        {
            return Aggregate(node.Children.Select(child => child.Accept(this, context)));
        }

        public override Result Visit(CallNode node, Context context)
        {
            return node.Args.Accept(this, context);
        }

        public override Result Visit(ListNode node, Context context)
        {
            return Aggregate(node.Children.Select(child => child.Accept(this, context)));
        }

        public override Result Visit(RecordNode node, Context context)
        {
            return Aggregate(node.Children.Select(child => child.Accept(this, context)));
        }

        public override Result Visit(TableNode node, Context context)
        {
            return Aggregate(node.Children.Select(child => child.Accept(this, context)));
        }

        public override Result Visit(AsNode node, Context context)
        {
            return node.Left.Accept(this, context);
        }

        private IEnumerable<Result> Lazily(params Func<Result>[] actions)
        {
            foreach (var action in actions)
            {
                yield return action();
            }
        }
    }
}