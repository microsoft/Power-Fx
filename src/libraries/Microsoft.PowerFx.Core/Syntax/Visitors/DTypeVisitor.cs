// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal class DTypeVisitor : TexlFunctionalVisitor<DType, DefinedTypeSymbolTable>
    {
        public override DType Visit(ErrorNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(BlankNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(BoolLitNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(StrLitNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(NumLitNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(DecLitNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(FirstNameNode node, DefinedTypeSymbolTable context)
        {
            var name = node.Ident.Name.Value;
            if (context.TryLookup(new DName(name), out NameLookupInfo nameInfo))
            {
                return nameInfo.Type;
            }

            return FormulaType.GetFromStringOrNull(name)._type;
        }

        public override DType Visit(ParentNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(SelfNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(StrInterpNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(DottedNameNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(UnaryOpNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(BinaryOpNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(VariadicOpNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(CallNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(ListNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }

        public override DType Visit(RecordNode node, DefinedTypeSymbolTable context)
        {
            var list = new List<TypedName>();
            foreach (var (cNode, ident) in node.ChildNodes.Zip(node.Ids, (a, b) => (a, b)))
            {
                var ty = cNode.Accept(this, context);
                if (ty == null)
                {
                    return null;
                }

                list.Add(new TypedName(ty, new DName(ident.Name.Value)));
            }

            return DType.CreateRecord(list);
        }

        public override DType Visit(TableNode node, DefinedTypeSymbolTable context)
        {
            var childNode = node.ChildNodes.First();
            var ty = childNode.Accept(this, context);
            if (ty == null)
            {
                return null;
            }

            return ty.ToTable();
        }

        public override DType Visit(AsNode node, DefinedTypeSymbolTable context)
        {
            throw new NotImplementedException();
        }
    }
}
