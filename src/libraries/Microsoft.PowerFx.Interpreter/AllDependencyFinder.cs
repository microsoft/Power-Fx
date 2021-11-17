// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Syntax.Visitors;

namespace Microsoft.PowerFx
{
    internal class AllDependencyFinder : IdentityTexlVisitor
    {
        public HashSet<Span> _spans = new();

        public static HashSet<Span> FindAllDependencies(TexlNode node)
        {
            var v = new AllDependencyFinder();
            node.Accept(v);

            return v._spans;
        }

        public override void Visit(FirstNameNode node)
        {
            if (node.Parent?.Kind == Core.Syntax.NodeKind.DottedName)
            {
                TexlNode nodeT = node;
                while (nodeT.Parent?.Kind == Core.Syntax.NodeKind.DottedName)
                {
                    nodeT = nodeT.Parent;
                }
                _spans.Add(nodeT.GetCompleteSpan());
            }
            else
            {
                _spans.Add(node.Token.Span);
            }

            base.Visit(node);
        }
    }
}
