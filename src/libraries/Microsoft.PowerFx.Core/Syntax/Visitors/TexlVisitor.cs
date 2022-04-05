// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    /// <summary>
    /// Abstract visitor base class.
    /// </summary>
    public abstract class TexlVisitor
    {
        /// <summary>
        /// Visit <see cref="ErrorNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void Visit(ErrorNode node);

        /// <summary>
        /// Visit <see cref="BlankNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void Visit(BlankNode node);

        /// <summary>
        /// Visit <see cref="BoolLitNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void Visit(BoolLitNode node);

        /// <summary>
        /// Visit <see cref="StrLitNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void Visit(StrLitNode node);

        /// <summary>
        /// Visit <see cref="NumLitNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void Visit(NumLitNode node);

        /// <summary>
        /// Visit <see cref="FirstNameNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void Visit(FirstNameNode node);

        /// <summary>
        /// Visit <see cref="ParentNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void Visit(ParentNode node);

        /// <summary>
        /// Visit <see cref="SelfNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void Visit(SelfNode node);

        /// <summary>
        /// Pre-visit <see cref="StrInterpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(StrInterpNode)"/>.</returns>
        public virtual bool PreVisit(StrInterpNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="DottedNameNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(DottedNameNode)"/>.</returns>
        public virtual bool PreVisit(DottedNameNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="UnaryOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(UnaryOpNode)"/>.</returns>
        public virtual bool PreVisit(UnaryOpNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="BinaryOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(BinaryOpNode)"/>.</returns>
        public virtual bool PreVisit(BinaryOpNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="VariadicOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(VariadicOpNode)"/>.</returns>
        public virtual bool PreVisit(VariadicOpNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="CallNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(CallNode)"/>.</returns>
        public virtual bool PreVisit(CallNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="ListNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(ListNode)"/>.</returns>
        public virtual bool PreVisit(ListNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="RecordNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(RecordNode)"/>.</returns>
        public virtual bool PreVisit(RecordNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="TableNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(TableNode)"/>.</returns>
        public virtual bool PreVisit(TableNode node)
        {
            return true;
        }

        /// <summary>
        /// Pre-visit <see cref="AsNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>Whether to visit children nodes and call <see cref="PostVisit(AsNode)"/>.</returns>
        public virtual bool PreVisit(AsNode node)
        {
            return true;
        }

        /// <summary>
        /// Post-visit <see cref="StrInterpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(StrInterpNode node);

        /// <summary>
        /// Post-visit <see cref="DottedNameNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(DottedNameNode node);

        /// <summary>
        /// Post-visit <see cref="UnaryOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(UnaryOpNode node);

        /// <summary>
        /// Post-visit <see cref="BinaryOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(BinaryOpNode node);

        /// <summary>
        /// Post-visit <see cref="VariadicOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(VariadicOpNode node);

        /// <summary>
        /// Post-visit <see cref="CallNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(CallNode node);

        /// <summary>
        /// Post-visit <see cref="ListNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(ListNode node);

        /// <summary>
        /// Post-visit <see cref="RecordNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(RecordNode node);

        /// <summary>
        /// Post-visit <see cref="TableNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(TableNode node);

        /// <summary>
        /// Post-visit <see cref="AsNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public abstract void PostVisit(AsNode node);
    }
}
