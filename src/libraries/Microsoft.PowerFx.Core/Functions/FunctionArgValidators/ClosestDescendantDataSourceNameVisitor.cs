// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.AppMagic.Authoring.Publish
{
    /// <summary>
    /// A visitor that finds the all descendant first name nodes which may correspond to
    /// data sources.
    /// </summary>
    internal class ClosestDescendantDataSourceNameVisitor : TexlFunctionalVisitor<IEnumerable<FirstNameNode>?, bool>
    {
        private readonly TexlBinding _binding;

        private ClosestDescendantDataSourceNameVisitor(TexlBinding binding)
        {
            _binding = binding;
        }

        /// <summary>
        /// Provides a basic search that attempts to find the first name node representing
        /// the a datasource descendant of the provided node.
        /// </summary>
        /// <param name="root">
        ///  Root node where the search will begin.
        /// </param>
        /// <param name="binding">
        /// If is true is returned, dataSourceNode will be set to the found node.
        /// </param>
        /// <param name="nodes">
        /// If is true is returned, dataSourceNode will be set to the found node.
        /// </param>
        /// <returns>
        /// True if a node was found that may correspond to a datasource first name,
        /// false otherwise.
        /// </returns>
        internal static bool TryGetFirstNameNodes(TexlNode root, TexlBinding binding, out IEnumerable<FirstNameNode> nodes)
        {
            nodes = root.Accept(new ClosestDescendantDataSourceNameVisitor(binding), false) ?? Enumerable.Empty<FirstNameNode>();
            return nodes.Any();
        }

        /// <summary>
        /// Finds the nearest descendant first name node that oculd be a data source, and
        /// filters any that do not satisfy basic data source criteria (type is table,
        /// entity exists, info indicates datasource).
        /// </summary>
        /// <param name="root">
        ///  Root node where the search will begin.
        /// </param>
        /// <param name="binding">
        /// If is true is returned, dataSourceNode will be set to the found node.
        /// </param>
        /// <param name="nodes">
        /// If is true is returned, dataSourceNode will be set to the found node.
        /// </param>
        /// <returns>
        /// True if a node was found that corresponds to a datasource first name, false
        /// otherwise.
        /// </returns>
        internal static bool TryGetDataSourceFirstNameNodes(TexlNode root, TexlBinding binding, out IEnumerable<FirstNameNode> nodes)
        {
            if (TryGetFirstNameNodes(root, binding, out var firstNameNodes))
            {
                nodes = firstNameNodes.Where(node =>
                    binding.GetType(node) is { IsTable: true } &&
                    binding.GetInfo(node) is { Kind: BindKind.Data, Name: { } entityName } &&
                    (binding.EntityScope?.TryGetEntity(entityName, out IExternalDataSource _) ?? true));
            }
            else
            {
                nodes = Enumerable.Empty<FirstNameNode>();
            }

            return nodes.Any();
        }

        public override IEnumerable<FirstNameNode> Visit(FirstNameNode node, bool context) => new List<FirstNameNode>() { node };

        public override IEnumerable<FirstNameNode> Visit(DottedNameNode node, bool context) => node.Left?.Accept(this, context) ?? Enumerable.Empty<FirstNameNode>();

        /// <summary>
        /// Visits all arguments of the provided call node accordinging to 
        /// <see cref="TexlFunction.TryGetDataSourceArgumentIndices"/> on the related
        /// instance.
        /// </summary>
        /// <param name="node">Node from which to begin search.</param>
        /// <param name="context">Unused.</param>
        /// <returns>List of first name nodes that might be data sources.</returns>
        public override IEnumerable<FirstNameNode> Visit(CallNode node, bool context)
        {
            if (_binding.GetType(node).IsAggregate &&
                _binding.TryGetInfo(node.Id, out CallInfo info) &&
                info.Function.TryGetDataSourceArgumentIndices(node.Args.Count, out var dataSourceArgIndices))
            {
                return dataSourceArgIndices
                    .Select(node.Args.Children.ElementAtOrDefault)
                    .Where(argNode => argNode is not null)
                    .SelectMany(argNode => argNode.Accept(this, context));
            }

            return Enumerable.Empty<FirstNameNode>();
        }

        public override IEnumerable<FirstNameNode> Visit(AsNode node, bool context) => node.Left.Accept(this, context) ?? Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(ErrorNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(BlankNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(BoolLitNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(StrLitNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(NumLitNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(DecLitNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(ParentNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(SelfNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(StrInterpNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(UnaryOpNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(BinaryOpNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(VariadicOpNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(ListNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(RecordNode node, bool context) => Enumerable.Empty<FirstNameNode>();

        public override IEnumerable<FirstNameNode> Visit(TableNode node, bool context) => Enumerable.Empty<FirstNameNode>();
    }
}
