// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    internal sealed class FindNodeVisitor : IdentityTexlVisitor
    {
        private readonly int _cursorPosition;
        private TexlNode _result;

        private FindNodeVisitor(int cursorPosition)
        {
            Contracts.Assert(cursorPosition >= 0);

            _cursorPosition = cursorPosition;
        }

        private void SetCurrentNodeAsResult(TexlNode node)
        {
            Contracts.AssertValue(node);

            _result = node;
        }

        public static TexlNode Run(TexlNode node, int cursorPosition)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(cursorPosition >= 0);

            var visitor = new FindNodeVisitor(cursorPosition);
            node.Accept(visitor);
            return visitor._result;
        }

        public override bool PreVisit(VariadicOpNode node)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Children.Length > 0);

            var numTokens = CollectionUtils.Size(node.OpTokens);

            Contracts.Assert(node.Children.Length == numTokens + 1 || node.Children.Length == numTokens);

            for (var i = 0; i < numTokens; i++)
            {
                var token = node.OpTokens[i];

                // Cursor position is inside ith child.

                if (_cursorPosition <= token.Span.Min)
                {
                    node.Children[i].Accept(this);
                    return false;
                }

                // Cursor is on one of the operator tokens

                if (_cursorPosition <= token.Span.Lim)
                {
                    _result = node;
                    return false;
                }
            }

            // If we got here the cursor should be in the last child.
            node.Children[node.Children.Length - 1].Accept(this);

            return false;
        }

        public override bool PreVisit(StrInterpNode node)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Token.Kind == TokKind.StrInterpStart);

            if (_cursorPosition <= node.Token.Span.Min // Cursor position is before the $"
                || (node.StrInterpEnd != null && node.StrInterpEnd is StrInterpEndToken && node.StrInterpEnd.Span.Lim <= _cursorPosition) // Cursor is after the close quote.
                || node.Children.Count() == 0) //// Cursor is inside empty string interpolation.
            {
                _result = node;
                return false;
            }

            for (var i = 0; i < node.Children.Length; i++)
            {
                var child = node.Children[i];

                // Cursor position is inside ith child.
                if (_cursorPosition <= child.GetCompleteSpan().Lim)
                {
                    child.Accept(this);
                    return false;
                }
            }

            // If we got here we could be inside an empty island
            // i.e. $"Hello {|}"
            // Just visit the last child
            node.Children[node.Children.Length - 1].Accept(this);

            return false;
        }

        public override bool PreVisit(BinaryOpNode node)
        {
            Contracts.AssertValue(node);

            // Cursor is in the left node.
            if (_cursorPosition <= node.Token.Span.Min)
            {
                node.Left.Accept(this);
                return false;
            }

            // Cursor is inside the operation token.
            if (_cursorPosition <= node.Token.Span.Lim)
            {
                _result = node;
                return false;
            }

            node.Right.Accept(this);
            return false;
        }

        public override bool PreVisit(UnaryOpNode node)
        {
            Contracts.AssertValue(node);

            // Cursor is inside the operation token.
            if (node.Token.Kind == TokKind.PercentSign)
            {
                var span = node.GetSourceBasedSpan();

                if ((node.Token.Span.Min <= _cursorPosition && _cursorPosition <= node.Token.Span.Lim) || _cursorPosition <= span.Min || _cursorPosition >= span.Lim)
                {
                    _result = node;
                    return false;
                }
            }
            else
            {
                if (_cursorPosition <= node.Token.Span.Lim)
                {
                    _result = node;
                    return false;
                }
            }

            // Cursor is inside the child.
            node.Child.Accept(this);
            return false;
        }

        public override bool PreVisit(CallNode node)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Token.Kind == TokKind.ParenOpen || node.Token.Kind == TokKind.StrInterpStart);

            if (_cursorPosition <= node.Token.Span.Min // Cursor position is before the open paren.
                || (node.ParenClose != null && node.ParenClose.Span.Lim <= _cursorPosition) // Cursor is after the closed paren.
                || node.Args.Count == 0) //// Cursor is between the open and closed paren.
            {
                _result = node;
                return false;
            }

            // Cursor is in one of the args.
            node.Args.Accept(this);
            return false;
        }

        public override bool PreVisit(ListNode node)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Children.Length > 0);
            Contracts.Assert(node.Children.Length == CollectionUtils.Size(node.Delimiters) + 1);

            for (var i = 0; i < CollectionUtils.Size(node.Delimiters); i++)
            {
                var tokDel = node.Delimiters[i];

                // Cursor position is inside ith child.
                if (_cursorPosition <= tokDel.Span.Min)
                {
                    node.Children[i].Accept(this);
                    return false;
                }
            }

            // If we got here the cursor should be in the last child.
            node.Children[node.Children.Length - 1].Accept(this);
            return false;
        }

        public override bool PreVisit(DottedNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Token.IsDottedNamePunctuator);

            // Cursor is before the dot.
            if (_cursorPosition <= node.Token.Span.Min)
            {
                node.Left.Accept(this);
                return false;
            }

            // Cursor is in the dot or the right identifier.
            _result = node;
            return false;
        }

        public override bool PreVisit(RecordNode node)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Token.Kind == TokKind.CurlyOpen || node.Token.Kind == TokKind.Ident);

            if (_cursorPosition <= node.Token.Span.Min || // If cursor position is before the open curly return the record node.
                node.Count == 0 || // Or if the record node is empty, return the record node.
                (node.CurlyClose != null && node.CurlyClose.Span.Lim <= _cursorPosition)) //// Cursor is after the closed curly.
            {
                _result = node;
                return false;
            }

            // Cursor is between the open and closed curly.
            var length = CollectionUtils.Size(node.Commas);

            for (var i = 0; i < length; i++)
            {
                var tokComma = node.Commas[i];

                // Cursor position is inside ith child.
                if (_cursorPosition <= tokComma.Span.Min)
                {
                    node.Children[i].Accept(this);
                    return false;
                }
            }

            if (node.CurlyClose == null || _cursorPosition <= node.CurlyClose.Span.Min)
            {
                // Cursor is within the last child.
                node.Children[node.Children.Length - 1].Accept(this);
                return false;
            }

            // Cursor is after the closing curly.
            _result = node;
            return false;
        }

        public override bool PreVisit(TableNode node)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Token.Kind == TokKind.BracketOpen);

            if (_cursorPosition <= node.Token.Span.Min // If cursor position is before the open Bracket return the table node.
                || node.Count == 0 // Or if the table node is empty, return the table node.
                || (node.BracketClose != null && node.BracketClose.Span.Lim <= _cursorPosition)) //// Cursor is after the closed bracket.
            {
                _result = node;
                return false;
            }

            // Cursor is between the open and closed bracket.
            for (var i = 0; i < node.Commas.Length; i++)
            {
                // Cursor position is inside ith child.
                if (_cursorPosition <= node.Commas[i].Span.Lim)
                {
                    node.Children[i].Accept(this);
                    return false;
                }
            }

            // If we got here the cursor should be in the last child.
            node.Children[node.Children.Length - 1].Accept(this);
            return false;
        }

        public override void Visit(BlankNode node)
        {
            SetCurrentNodeAsResult(node);
        }

        public override void Visit(BoolLitNode node)
        {
            SetCurrentNodeAsResult(node);
        }

        public override void Visit(ErrorNode node)
        {
            SetCurrentNodeAsResult(node);
        }

        public override void Visit(StrLitNode node)
        {
            SetCurrentNodeAsResult(node);
        }

        public override void Visit(NumLitNode node)
        {
            SetCurrentNodeAsResult(node);
        }

        public override void Visit(FirstNameNode node)
        {
            SetCurrentNodeAsResult(node);
        }

        public override void Visit(ParentNode node)
        {
            SetCurrentNodeAsResult(node);
        }

        public override void Visit(SelfNode node)
        {
            SetCurrentNodeAsResult(node);
        }
    }
}
