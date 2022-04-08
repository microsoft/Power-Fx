// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    /// <summary>
    ///     A visitor that passes a context to each visit method and where each visit method returns a result.
    /// </summary>
    /// <typeparam name="TResult">The results type of the visitor.</typeparam>
    /// <typeparam name="TContext">The context type of the visitor.</typeparam>
    public abstract class TexlFunctionalVisitor<TResult, TContext>
    {
        /// <summary>
        /// Visit <see cref="ErrorNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(ErrorNode node, TContext context);

        /// <summary>
        /// Visit <see cref="BlankNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(BlankNode node, TContext context);

        /// <summary>
        /// Visit <see cref="BoolLitNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(BoolLitNode node, TContext context);

        /// <summary>
        /// Visit <see cref="StrLitNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(StrLitNode node, TContext context);

        /// <summary>
        /// Visit <see cref="NumLitNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(NumLitNode node, TContext context);

        /// <summary>
        /// Visit <see cref="FirstNameNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(FirstNameNode node, TContext context);

        /// <summary>
        /// Visit <see cref="ParentNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(ParentNode node, TContext context);

        /// <summary>
        /// Visit <see cref="SelfNode" /> leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(SelfNode node, TContext context);

        /// <summary>
        /// Visit <see cref="StrInterpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(StrInterpNode node, TContext context);

        /// <summary>
        /// Visit <see cref="DottedNameNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(DottedNameNode node, TContext context);

        /// <summary>
        /// Visit <see cref="UnaryOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(UnaryOpNode node, TContext context);

        /// <summary>
        /// Visit <see cref="BinaryOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(BinaryOpNode node, TContext context);

        /// <summary>
        /// Visit <see cref="VariadicOpNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(VariadicOpNode node, TContext context);

        /// <summary>
        /// Visit <see cref="CallNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(CallNode node, TContext context);

        /// <summary>
        /// Visit <see cref="ListNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(ListNode node, TContext context);

        /// <summary>
        /// Visit <see cref="RecordNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(RecordNode node, TContext context);

        /// <summary>
        /// Visit <see cref="TableNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(TableNode node, TContext context);

        /// <summary>
        /// Visit <see cref="AsNode" /> non-leaf node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <param name="context">The context passed to the node.</param>
        /// <returns>The node visit result.</returns>
        public abstract TResult Visit(AsNode node, TContext context);
    }
}
