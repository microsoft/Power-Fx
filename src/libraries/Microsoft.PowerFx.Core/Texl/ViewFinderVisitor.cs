// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl
{
    /// <summary>
    /// This visitor is used to walkthrough the tree to check the existence of a view
    /// </summary>
    internal sealed class ViewFinderVisitor : TexlVisitor
    {
        private readonly TexlBinding _txb;
        public bool ContainsView { get; private set; }

        public ViewFinderVisitor(TexlBinding binding)
        {
            Contracts.AssertValue(binding);

            _txb = binding;
        }

        public override void PostVisit(DottedNameNode node)
        {
            var argType = _txb.GetType(node);
            if (argType.Kind == DKind.ViewValue)
            {
                ContainsView = true;
            }

        }

        public override void Visit(FirstNameNode node) { }
        public override void PostVisit(CallNode node) { }
        public override void PostVisit(VariadicOpNode node) { }
        public override void PostVisit(RecordNode node) { }
        public override void PostVisit(ListNode node) { }
        public override void PostVisit(BinaryOpNode node) { }
        public override void PostVisit(UnaryOpNode node) { }
        public override void PostVisit(TableNode node) { }
        public override void PostVisit(AsNode node) { }
        public override void Visit(ParentNode node) { }
        public override void Visit(NumLitNode node) { }
        public override void Visit(ReplaceableNode node) { }
        public override void Visit(StrLitNode node) { }
        public override void Visit(BoolLitNode node) { }
        public override void Visit(BlankNode node) { }
        public override void Visit(ErrorNode node) { }
        public override void Visit(SelfNode node) { }
    }
}