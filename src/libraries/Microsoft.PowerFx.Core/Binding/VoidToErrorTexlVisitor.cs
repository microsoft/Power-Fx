// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    /// <summary>
    /// Detects void expressions and converts them to error expressions.
    /// </summary>
    internal class VoidToErrorTexlVisitor : TexlVisitor
    {
        private readonly TexlBinding _txb;
        private readonly Stack<bool> _subtreeErrored = new Stack<bool>();

        public VoidToErrorTexlVisitor(TexlBinding txb)
        {
            _txb = txb ?? throw new ArgumentNullException(nameof(txb));
        }

        /// <summary>
        /// Kick off the walk.  We seed the stack with a dummy "false" so
        /// that the root's parent slot always exists.
        /// </summary>
        internal void Run()
        {
            _subtreeErrored.Clear();

            // seed a dummy flag for the "parent of root"
            _subtreeErrored.Push(false);
            _txb.Top.Accept(this);
            _subtreeErrored.Pop();
        }

        // Called at every node entry (both leaf & non‐leaf)
        private bool PreProcessNode()
        {
            _subtreeErrored.Push(false);
            return true;
        }

        /// <summary>
        /// Common logic for when we're done visiting *any* node.
        /// </summary>
        private void PostProcessNode(TexlNode node)
        {
            // Pop this node's "did-anything-errored-in-subtree" flag (dummy false for all leaf nodes).
            var erroredInSubtree = _subtreeErrored.Pop();

            // If this node is a child of another node, pop the parent's flag and assign again the latest result to the parent.
            var parentErrored = _subtreeErrored.Pop();

            if (!erroredInSubtree && _txb.GetTypeAllowInvalid(node) == DType.Void)
            {
                _txb.ErrorContainer.EnsureError(
                    node,
                    TexlStrings.ErrBadType_NonBehavioralVoidExpression);
                erroredInSubtree = true;
            }

            _subtreeErrored.Push(parentErrored || erroredInSubtree);
        }

        //––– Non-leaf nodes: push on PreVisit, pop+check in PostVisit –––

        public override bool PreVisit(StrInterpNode node) => PreProcessNode();

        public override void PostVisit(StrInterpNode node) => PostProcessNode(node);

        public override bool PreVisit(DottedNameNode node) => PreProcessNode();

        public override void PostVisit(DottedNameNode node) => PostProcessNode(node);

        public override bool PreVisit(UnaryOpNode node) => PreProcessNode();

        public override void PostVisit(UnaryOpNode node) => PostProcessNode(node);

        public override bool PreVisit(BinaryOpNode node) => PreProcessNode();

        public override void PostVisit(BinaryOpNode node) => PostProcessNode(node);

        public override bool PreVisit(VariadicOpNode node) => PreProcessNode();

        public override void PostVisit(VariadicOpNode node) => PostProcessNode(node);

        public override bool PreVisit(CallNode node) => PreProcessNode();

        public override void PostVisit(CallNode node) => PostProcessNode(node);

        public override bool PreVisit(ListNode node) => PreProcessNode();

        public override void PostVisit(ListNode node) => PostProcessNode(node);

        public override bool PreVisit(RecordNode node) => PreProcessNode();

        public override void PostVisit(RecordNode node) => PostProcessNode(node);

        public override bool PreVisit(TableNode node) => PreProcessNode();

        public override void PostVisit(TableNode node) => PostProcessNode(node);

        public override bool PreVisit(AsNode node) => PreProcessNode();

        public override void PostVisit(AsNode node) => PostProcessNode(node);

        //––– Leaf nodes: treat them as mini-subtrees –––

        public override void Visit(TypeLiteralNode node) => VisitLeaf(node);

        public override void Visit(ErrorNode node) => VisitLeaf(node);

        public override void Visit(BlankNode node) => VisitLeaf(node);

        public override void Visit(BoolLitNode node) => VisitLeaf(node);

        public override void Visit(StrLitNode node) => VisitLeaf(node);

        public override void Visit(NumLitNode node) => VisitLeaf(node);

        public override void Visit(DecLitNode node) => VisitLeaf(node);

        public override void Visit(FirstNameNode node) => VisitLeaf(node);

        public override void Visit(ParentNode node) => VisitLeaf(node);

        public override void Visit(SelfNode node) => VisitLeaf(node);

        private void VisitLeaf(TexlNode node)
        {
            // start a fresh subtree-error flag
            _subtreeErrored.Push(false);

            // combine ensure+propagate in one shot
            PostProcessNode(node);
        }
    }
}
