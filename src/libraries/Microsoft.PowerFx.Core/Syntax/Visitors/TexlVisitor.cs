// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    // Abstract visitor base class
    internal abstract class TexlVisitor
    {
        // Visit methods for leaf node types.
        public abstract void Visit(ErrorNode node);

        public abstract void Visit(BlankNode node);

        public abstract void Visit(BoolLitNode node);

        public abstract void Visit(StrLitNode node);

        public abstract void Visit(NumLitNode node);

        public abstract void Visit(FirstNameNode node);

        public abstract void Visit(ParentNode node);

        public abstract void Visit(SelfNode node);

        public abstract void Visit(ReplaceableNode node);

        // Visit methods for non-leaf node types.
        // If PreVisit returns true, the children are visited and PostVisit is called.
        public virtual bool PreVisit(StrInterpNode node)
        {
            return true;
        }

        public virtual bool PreVisit(DottedNameNode node)
        {
            return true;
        }

        public virtual bool PreVisit(UnaryOpNode node)
        {
            return true;
        }

        public virtual bool PreVisit(BinaryOpNode node)
        {
            return true;
        }

        public virtual bool PreVisit(VariadicOpNode node)
        {
            return true;
        }

        public virtual bool PreVisit(CallNode node)
        {
            return true;
        }

        public virtual bool PreVisit(ListNode node)
        {
            return true;
        }

        public virtual bool PreVisit(RecordNode node)
        {
            return true;
        }

        public virtual bool PreVisit(TableNode node)
        {
            return true;
        }

        public virtual bool PreVisit(AsNode node)
        {
            return true;
        }

        public abstract void PostVisit(StrInterpNode node);

        public abstract void PostVisit(DottedNameNode node);

        public abstract void PostVisit(UnaryOpNode node);

        public abstract void PostVisit(BinaryOpNode node);

        public abstract void PostVisit(VariadicOpNode node);

        public abstract void PostVisit(CallNode node);

        public abstract void PostVisit(ListNode node);

        public abstract void PostVisit(RecordNode node);

        public abstract void PostVisit(TableNode node);

        public abstract void PostVisit(AsNode node);
    }
}
