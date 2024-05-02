// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    // Visitor to determine unresolved type dependencies in a given texl node
    internal class DefinedTypeDependencyVisitor : IdentityTexlVisitor
    {
        private readonly HashSet<string> _dependencies;
        private readonly INameResolver _context;

        private DefinedTypeDependencyVisitor(INameResolver context)
        {
            Contracts.AssertValue(context);

            _dependencies = new HashSet<string>();
            _context = context;
        }

        public static HashSet<string> FindDependencies(TexlNode node, INameResolver context)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(context);

            var visitor = new DefinedTypeDependencyVisitor(context);
            node.Accept(visitor);
            return visitor._dependencies;
        }

        public override void Visit(FirstNameNode node)
        {
            Contracts.AssertValue(node);

            var name = node.Ident.Name;

            // Node is unresolved dependency if its not found in context lookup 
            if (!_context.LookupType(name, out FormulaType _))
            {
                _dependencies.Add(name);
            }
        }
    }
}
