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
    // Visitor to convert AST to DType. 
    internal class DTypeVisitor : TexlFunctionalVisitor<DType, INameResolver>
    {
        private DTypeVisitor()
        {
        }

        public static DType Run(TexlNode node, INameResolver context)
        {
            return node.Accept(new DTypeVisitor(), context);
        }

        public override DType Visit(FirstNameNode node, INameResolver context)
        {
            var name = node.Ident.Name;
            if (context.LookupType(name, out FormulaType ft))
            {
                return ft._type;
            }

            return DType.Invalid;
        }

        public override DType Visit(RecordNode node, INameResolver context)
        {
            var typedNames = new List<TypedName>();
            var names = new HashSet<DName>();

            foreach (var (cNode, ident) in node.ChildNodes.Zip(node.Ids, (a, b) => (a, b)))
            {
                var name = ident.Name;

                // Invalid if Record fields repeat.
                if (!names.Add(name))
                {
                    return DType.Invalid;
                }

                var ty = cNode.Accept(this, context);
                if (ty == DType.Invalid)
                {
                    return DType.Invalid;
                }

                typedNames.Add(new TypedName(ty, name));
            }

            return DType.CreateRecord(typedNames);
        }

        public override DType Visit(TableNode node, INameResolver context)
        {
            var childNode = node.ChildNodes.First();
            var ty = childNode.Accept(this, context);

            if (ty == DType.Invalid)
            {
                return DType.Invalid;
            }

            if (ty.IsRecord || ty.IsTable)
            {
                return ty.ToTable();
            }

            // Single column table syntax
            var rowType = DType.EmptyRecord.Add(new TypedName(ty, TableValue.ValueDName));

            return rowType.ToTable();
        }

        public override DType Visit(ErrorNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(BlankNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(BoolLitNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(StrLitNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(NumLitNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(DecLitNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(ParentNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(SelfNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(StrInterpNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(DottedNameNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(UnaryOpNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(BinaryOpNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(VariadicOpNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(CallNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(ListNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(AsNode node, INameResolver context)
        {
            return DType.Invalid;
        }
    }
}
