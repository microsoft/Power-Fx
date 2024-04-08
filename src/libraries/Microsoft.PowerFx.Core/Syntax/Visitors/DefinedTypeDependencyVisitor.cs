// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal class DefinedTypeDependencyVisitor : IdentityTexlVisitor
    {
        private readonly HashSet<string> _dependencies;
        private readonly INameResolver _context;

        private DefinedTypeDependencyVisitor(INameResolver context)
        {
            _dependencies = new HashSet<string>();
            _context = context;
        }

        public static HashSet<string> FindDependencies(TexlNode node, INameResolver context)
        { 
            var visitor = new DefinedTypeDependencyVisitor(context);
            node.Accept(visitor);
            return visitor._dependencies;
        }

        public override void Visit(FirstNameNode node)
        {
            var name = node.Ident.Name;

            if (_context.LookupType(name, out FormulaType _))
            {
                return;
            }

            if (((INameResolver)PrimitiveTypesSymbolTable.Instance).LookupType(name, out FormulaType _))
            {
                return;
            }
            
            _dependencies.Add(name);
            return;
        }
    }
}
