// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Binding
{
    internal sealed class BinderNodesVisitor : IdentityTexlVisitor
    {
        private readonly List<BinaryOpNode> _binaryOperators;
        public IEnumerable<BinaryOpNode> BinaryOperators { get { return _binaryOperators; } }

        private readonly List<VariadicOpNode> _variadicOperators;
        public IEnumerable<VariadicOpNode> VariadicOperators { get { return _variadicOperators; } }

        private readonly List<BoolLitNode> _booleanLiterals;
        public IEnumerable<BoolLitNode> BooleanLiterals { get { return _booleanLiterals; } }

        private readonly List<NumLitNode> _numericLiterals;
        public IEnumerable<NumLitNode> NumericLiterals { get { return _numericLiterals; } }

        private readonly List<StrLitNode> _stringLiterals;
        public IEnumerable<StrLitNode> StringLiterals { get { return _stringLiterals; } }

        private readonly HashSet<NodeKind> _keywords;
        // Parent, Self, and ThisItem keywords.
        public IEnumerable<NodeKind> Keywords { get { return _keywords; } }

        private readonly List<UnaryOpNode> _unaryOperators;
        public IEnumerable<UnaryOpNode> UnaryOperators { get { return _unaryOperators; } }

        private BinderNodesVisitor(TexlNode node)
        {
            Contracts.AssertValue(node);

            _binaryOperators = new List<BinaryOpNode>();
            _variadicOperators = new List<VariadicOpNode>();
            _booleanLiterals = new List<BoolLitNode>();
            _numericLiterals = new List<NumLitNode>();
            _stringLiterals = new List<StrLitNode>();
            _keywords = new HashSet<NodeKind>();
            _unaryOperators = new List<UnaryOpNode>();
        }

        public override void PostVisit(BinaryOpNode node)
        {
            Contracts.AssertValue(node);
            _binaryOperators.Add(node);
        }

        public override void PostVisit(VariadicOpNode node)
        {
            Contracts.AssertValue(node);
            _variadicOperators.Add(node);
        }

        public override void PostVisit(UnaryOpNode node)
        {
            Contracts.AssertValue(node);
            if(node.Token.Kind == TokKind.PercentSign)
                _unaryOperators.Add(node);
        }

        public override void Visit(BoolLitNode node)
        {
            Contracts.AssertValue(node);
            _booleanLiterals.Add(node);
        }

        public override void Visit(NumLitNode node)
        {
            Contracts.AssertValue(node);
            _numericLiterals.Add(node);
        }

        public override void Visit(StrLitNode node)
        {
            Contracts.AssertValue(node);
            _stringLiterals.Add(node);
        }

        public override void Visit(ParentNode node)
        {
            Contracts.AssertValue(node);
            _keywords.Add(node.Kind);
        }

        public override void Visit(SelfNode node)
        {
            Contracts.AssertValue(node);
            _keywords.Add(node.Kind);
        }

        public static BinderNodesVisitor Run(TexlNode node)
        {
            Contracts.AssertValue(node);

            var instance = new BinderNodesVisitor(node);
            node.Accept(instance);

            return instance;
        }
    }
}
