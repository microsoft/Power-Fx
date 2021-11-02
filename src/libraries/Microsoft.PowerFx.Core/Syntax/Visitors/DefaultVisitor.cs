// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    /// <summary>
    /// A base visitor for when you want a default result for most nodes.
    /// </summary>
    internal abstract class DefaultVisitor<Result, Context> : TexlFunctionalVisitor<Result, Context>
    {
        public virtual Result Default { get; }

        public DefaultVisitor(Result defaultValue)
        {
            Default = defaultValue;
        }

        public override Result Visit(ErrorNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(BlankNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(BoolLitNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(StrLitNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(NumLitNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(FirstNameNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(ParentNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(SelfNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(ReplaceableNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(DottedNameNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(UnaryOpNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(BinaryOpNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(VariadicOpNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(CallNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(ListNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(RecordNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(TableNode node, Context context)
        {
            return Default;
        }

        public override Result Visit(AsNode node, Context context)
        {
            return Default;
        }
    }
}