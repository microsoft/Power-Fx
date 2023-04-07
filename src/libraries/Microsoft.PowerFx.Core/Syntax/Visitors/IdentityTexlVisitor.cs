// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Visitor that by default does nothing. <seealso cref="TexlVisitor" />.
    /// </summary>
    public abstract class IdentityTexlVisitor : TexlVisitor
    {
        /// <inheritdoc />
        public override void Visit(ErrorNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(BlankNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(BoolLitNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(StrLitNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(NumLitNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(DecLitNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(FirstNameNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(ParentNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(SelfNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(DottedNameNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(UnaryOpNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(BinaryOpNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(VariadicOpNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(StrInterpNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(CallNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(ListNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(RecordNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(TableNode node)
        {
        }

        /// <inheritdoc />
        public override void PostVisit(AsNode node)
        {
        }
    }
}
