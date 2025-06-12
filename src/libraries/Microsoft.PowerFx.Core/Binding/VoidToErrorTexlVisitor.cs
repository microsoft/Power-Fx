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

        public VoidToErrorTexlVisitor(TexlBinding txb)
        {
            _txb = txb ?? throw new ArgumentNullException(nameof(txb));
        }

        private void EnsureNotVoid(TexlNode node)
        {
            if (_txb.GetTypeAllowInvalid(node) == DType.Void)
            {
                _txb.ErrorContainer.EnsureError(node, TexlStrings.ErrBadType_VoidExpression);
            }
        }

        public override void PostVisit(StrInterpNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(DottedNameNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(UnaryOpNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(BinaryOpNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(VariadicOpNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(CallNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(ListNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(RecordNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(TableNode node)
        {
            EnsureNotVoid(node);
        }

        public override void PostVisit(AsNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(TypeLiteralNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(ErrorNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(BlankNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(BoolLitNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(StrLitNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(NumLitNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(DecLitNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(FirstNameNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(ParentNode node)
        {
            EnsureNotVoid(node);
        }

        public override void Visit(SelfNode node)
        {
            EnsureNotVoid(node);
        }

        internal void Run()
        {
            _txb.Top.Accept(this);
        }
    }
}
