// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Syntax.Visitors;

namespace Microsoft.PowerFx.Core.Utils.FormulaSort
{
    internal sealed class IdentifierVisitor : TexlVisitor
    {
        private readonly Stack<IdentifierVisitorScope> _scopeStack = new Stack<IdentifierVisitorScope>();
        private readonly Dictionary<string, IdentifierVisitorMode> _idents = new Dictionary<string, IdentifierVisitorMode>();
        private readonly HashSet<string> _functionsNamesWithLambdas;

        public IdentifierVisitor(HashSet<string> functionNamesWithLambdas)
        {
            _scopeStack.Push(new IdentifierVisitorScope(IdentifierVisitorMode.KnownGlobal));
            _functionsNamesWithLambdas = functionNamesWithLambdas;
        }

        public IEnumerable<string> GetKnownGlobalOrLocalIdents()
        {
            return _idents
                .Where(kvp => kvp.Value == IdentifierVisitorMode.KnownGlobal || kvp.Value == IdentifierVisitorMode.KnownLocal)
                .Select(kvp => kvp.Key);
        }

        public override void PostVisit(StrInterpNode node)
        {
            node.AcceptChildren(this);
        }

        public override void PostVisit(DottedNameNode node)
        {
            node.Left.Accept(this);
        }

        public override void PostVisit(UnaryOpNode node)
        {
            node.Child.Accept(this);
        }

        public override void PostVisit(BinaryOpNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        public override void PostVisit(VariadicOpNode node)
        {
            node.AcceptChildren(this);
        }

        public override void PostVisit(CallNode node)
        {
            var scope = _scopeStack.Peek();
            if (_functionsNamesWithLambdas.Contains(node.Head.Name.Value))
            {
                _scopeStack.Push(new IdentifierVisitorScope(IdentifierVisitorMode.Unknown, scope));
                node.Args.Accept(this);
                _scopeStack.Pop();
            }
            else
            {
                node.Args.Accept(this);
            }
        }

        public override void PostVisit(ListNode node)
        {
            node.AcceptChildren(this);
        }

        public override void PostVisit(RecordNode node)
        {
            node.AcceptChildren(this);
        }

        public override void PostVisit(TableNode node)
        {
            node.AcceptChildren(this);
        }

        public override void PostVisit(AsNode node)
        {
            node.Left.Accept(this);
            _scopeStack.Peek().Add(node.Right.Name.Value, IdentifierVisitorMode.KnownLocal);
        }

        public override void Visit(ErrorNode node)
        {
        }

        public override void Visit(BlankNode node)
        {
        }

        public override void Visit(BoolLitNode node)
        {
        }

        public override void Visit(StrLitNode node)
        {
        }

        public override void Visit(NumLitNode node)
        {
        }

        public override void Visit(FirstNameNode node)
        {
            var ident = node.Ident.Name.Value;
            var mode = _scopeStack.Peek().Get(ident);
            _idents.Add(ident, mode);
        }

        public override void Visit(ParentNode node)
        {
        }

        public override void Visit(SelfNode node)
        {
        }
    }
}
