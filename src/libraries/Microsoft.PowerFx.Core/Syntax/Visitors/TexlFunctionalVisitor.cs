// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal abstract class TexlFunctionalVisitor<Result, Context>
    {
        // Visit methods for leaf node types.
        public abstract Result Visit(ErrorNode node, Context context);
        public abstract Result Visit(BlankNode node, Context context);
        public abstract Result Visit(BoolLitNode node, Context context);
        public abstract Result Visit(StrLitNode node, Context context);
        public abstract Result Visit(NumLitNode node, Context context);
        public abstract Result Visit(FirstNameNode node, Context context);
        public abstract Result Visit(ParentNode node, Context context);
        public abstract Result Visit(SelfNode node, Context context);
        public abstract Result Visit(ReplaceableNode node, Context context);

        // Visit methods for non-leaf node types.
        // If PreVisit returns true, the children are visited and PostVisit is called.
        public abstract Result Visit(DottedNameNode node, Context context);
        public abstract Result Visit(UnaryOpNode node, Context context);
        public abstract Result Visit(BinaryOpNode node, Context context);
        public abstract Result Visit(VariadicOpNode node, Context context);
        public abstract Result Visit(CallNode node, Context context);
        public abstract Result Visit(ListNode node, Context context);
        public abstract Result Visit(RecordNode node, Context context);
        public abstract Result Visit(TableNode node, Context context);
        public abstract Result Visit(AsNode node, Context context);
    }
}