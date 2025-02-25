// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal class ScopePredicateVisitor : TexlVisitor
    {
        private readonly DType _typeScope;
        private readonly DName[] _idents;

        public HashSet<DName> InUsePredicates;

        public ScopePredicateVisitor(DType typeScope, DName[] indents)
        {
            _typeScope = typeScope;

            InUsePredicates = new HashSet<DName>();
            _idents = indents;
        }

        public override void Visit(TypeLiteralNode node)
        {
            return;
        }

        public override void Visit(ErrorNode node)
        {
            return;
        }

        public override void Visit(BlankNode node)
        {
            return;
        }

        public override void Visit(BoolLitNode node)
        {
            return;
        }

        public override void Visit(StrLitNode node)
        {
            return;
        }

        public override void Visit(NumLitNode node)
        {
            return;
        }

        public override void Visit(DecLitNode node)
        {
            return;
        }

        public override void Visit(FirstNameNode node)
        {
            // We should reach this block only if we are working with an implicity 'ThisRecord'.
            if (_idents.Length == 1 && _typeScope.TryGetType(node.Ident.Name, out _))
            {
                InUsePredicates.Add(_idents.First());
            }
        }

        // We can assume if visiting a DottedNameNode is always a whole scope.
        public override bool PreVisit(DottedNameNode node)
        {
            var left = node.Left.AsFirstName();

            if (_idents.Contains(left.Ident.Name))
            {
                InUsePredicates.Add(left.Ident.Name);
            }            

            // No need to visit left/right separately.
            return false;
        }

        public override void Visit(ParentNode node)
        {
            return;
        }

        public override void Visit(SelfNode node)
        {
            return;
        }

        public override void PostVisit(StrInterpNode node)
        {
            return;
        }

        public override void PostVisit(DottedNameNode node)
        {
            return;
        }

        public override void PostVisit(UnaryOpNode node)
        {
            return;
        }

        public override void PostVisit(BinaryOpNode node)
        {
            return;
        }

        public override void PostVisit(VariadicOpNode node)
        {
            return;
        }

        public override void PostVisit(CallNode node)
        {
            return;
        }

        public override void PostVisit(ListNode node)
        {
            return;
        }

        public override void PostVisit(RecordNode node)
        {
            return;
        }

        public override void PostVisit(TableNode node)
        {
            return;
        }

        public override void PostVisit(AsNode node)
        {
            return;
        }

        public override bool PreVisit(CallNode node)
        {
            return false;
        }
    }
}
