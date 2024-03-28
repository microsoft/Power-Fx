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
    internal class DefinedTypeDependencyVisitor : TexlFunctionalVisitor<HashSet<string>, INameResolver>
    {
        private DefinedTypeDependencyVisitor()
        {
        }

        public static HashSet<string> Run(TexlNode node, INameResolver context)
        {
            return node.Accept(new DefinedTypeDependencyVisitor(), context);
        }

        public override HashSet<string> Visit(ErrorNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(BlankNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(BoolLitNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(StrLitNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(NumLitNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(DecLitNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(FirstNameNode node, INameResolver context)
        {
            var name = node.Ident.Name.Value;
            var result = new HashSet<string>();

            if (context.LookupType(new DName(name), out NameLookupInfo nameInfo))
            {
                return result;
            }

            var typeFromString = FormulaType.GetFromStringOrNull(name);

            if (typeFromString == null) 
            { 
                result.Add(name);
            }

            return result;
        }

        public override HashSet<string> Visit(ParentNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(SelfNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(StrInterpNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(DottedNameNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(UnaryOpNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(BinaryOpNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(VariadicOpNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(CallNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(ListNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> Visit(RecordNode node, INameResolver context)
        {
            var result = new HashSet<string>();
            foreach (var (cNode, ident) in node.ChildNodes.Zip(node.Ids, (a, b) => (a, b)))
            {
                var deps = cNode.Accept(this, context);
                if (deps.Any())
                {
                    result.UnionWith(deps);
                }
            }

            return result;
        }

        public override HashSet<string> Visit(TableNode node, INameResolver context)
        {
            var childNode = node.ChildNodes.First();
            var result = childNode.Accept(this, context);

            return result;
        }

        public override HashSet<string> Visit(AsNode node, INameResolver context)
        {
            throw new NotImplementedException();
        }
    }
}
