// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal class ScopePredicateVisitor : TexlVisitor
    {
        private readonly DType _typeScope;
        private readonly bool _wholeScope;

        public ScopePredicateVisitor(DType typeScope, bool wholeScope)
        {
            _typeScope = typeScope;
            _wholeScope = wholeScope;
        }

        public override void Visit(TypeLiteralNode node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(ErrorNode node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(BlankNode node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(BoolLitNode node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(StrLitNode node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(NumLitNode node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DecLitNode node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(FirstNameNode node)
        {
            if (_wholeScope)
            {
                throw new NotImplementedException();
            }
            else
            {
                var test = _typeScope.TryGetType(node.Ident.Name, out _);
            }
        }

        public override void Visit(ParentNode node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(SelfNode node)
        {
            throw new NotImplementedException();
        }

        public override void PostVisit(StrInterpNode node)
        {
            throw new NotImplementedException();
        }

        public override void PostVisit(DottedNameNode node)
        {
            return;
        }

        public override void PostVisit(UnaryOpNode node)
        {
            throw new NotImplementedException();
        }

        public override void PostVisit(BinaryOpNode node)
        {
            return;
        }

        public override void PostVisit(VariadicOpNode node)
        {
            throw new NotImplementedException();
        }

        public override void PostVisit(CallNode node)
        {
            throw new NotImplementedException();
        }

        public override void PostVisit(ListNode node)
        {
            throw new NotImplementedException();
        }

        public override void PostVisit(RecordNode node)
        {
            throw new NotImplementedException();
        }

        public override void PostVisit(TableNode node)
        {
            throw new NotImplementedException();
        }

        public override void PostVisit(AsNode node)
        {
            throw new NotImplementedException();
        }
    }
}
