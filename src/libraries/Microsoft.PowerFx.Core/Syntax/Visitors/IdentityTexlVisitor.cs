// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    // Identity visitor
    internal abstract class IdentityTexlVisitor : TexlVisitor
    {
        public override void Visit(ErrorNode node)
        {
        }

        public override void Visit(BlankNode node)
        {
        }

        public override void Visit(BoolLitNode node)
        {
        }

        public override void Visit(StrLitNode node)
        {
        }

        public override void Visit(NumLitNode node)
        {
        }

        public override void Visit(FirstNameNode node)
        {
        }

        public override void Visit(ParentNode node)
        {
        }

        public override void Visit(SelfNode node)
        {
        }

        public override void Visit(ReplaceableNode node)
        {
        }

        public override void PostVisit(DottedNameNode node)
        {
        }

        public override void PostVisit(UnaryOpNode node)
        {
        }

        public override void PostVisit(BinaryOpNode node)
        {
        }

        public override void PostVisit(VariadicOpNode node)
        {
        }

        public override void PostVisit(CallNode node)
        {
        }

        public override void PostVisit(ListNode node)
        {
        }

        public override void PostVisit(RecordNode node)
        {
        }

        public override void PostVisit(TableNode node)
        {
        }

        public override void PostVisit(AsNode node)
        {
        }
    }
}