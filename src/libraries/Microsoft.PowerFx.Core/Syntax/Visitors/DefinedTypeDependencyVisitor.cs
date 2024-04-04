// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal class DefinedTypeDependencyVisitor : IdentityTexlVisitor
    {
        private readonly HashSet<string> _result;
        private readonly INameResolver _context;

        private DefinedTypeDependencyVisitor(INameResolver context)
        {
            _result = new HashSet<string>();
            _context = context;
        }

        public static HashSet<string> FindDependencies(TexlNode node, INameResolver context)
        { 
            var visitor = new DefinedTypeDependencyVisitor(context);
            node.Accept(visitor);
            return visitor._result;
        }

        public override void Visit(FirstNameNode node)
        {
            var name = node.Ident.Name.Value;

            if (_context.LookupType(new DName(name), out NameLookupInfo _))
            {
                return;
            }

            var typeFromString = FormulaType.GetFromStringOrNull(name);

            if (typeFromString == null)
            {
                _result.Add(name);
            }
        }
    }
}
