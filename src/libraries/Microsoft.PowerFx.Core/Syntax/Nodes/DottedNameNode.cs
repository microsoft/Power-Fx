// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class DottedNameNode : NameNode
    {
        public readonly TexlNode Left;
        public readonly Identifier Right;

        // Can be null
        public readonly TexlNode RightNode;
        public readonly bool HasOnlyIdentifiers;
        public readonly bool HasPossibleNamespaceQualifier;

        public override NodeKind Kind => NodeKind.DottedName;

        // True if the name uses dots, e.g. A.B.C
        public bool UsesDot => Token.Kind == TokKind.Dot;

        // True if the name uses bangs, e.g. A!B!C
        public bool UsesBang => Token.Kind == TokKind.Bang;

        // True if the name uses brackets, e.g. A[B]
        public bool UsesBracket => Token.Kind == TokKind.BracketOpen;

        public DottedNameNode(ref int idNext, Token primaryToken, SourceList sourceList, TexlNode left, Identifier right, TexlNode rightNode)
            : base(ref idNext, primaryToken, sourceList)
        {
            Contracts.AssertValue(primaryToken);
            Contracts.Assert(primaryToken.IsDottedNamePunctuator);
            Contracts.AssertValue(left);
            Contracts.AssertValue(right);

            // The LHS of a [] can only be a first name node. E.g. foo[bar] is valid, but foo!bar[car] is not.
            // Also, dotted names can't mix tokens, except for []. E.g. foo[bar]!car is valid, but foo.bar!car is not.
            Contracts.Assert(primaryToken.Kind == TokKind.BracketOpen ?
                left is FirstNameNode :
                !(left is DottedNameNode) || left.AsDottedName().Token.Kind == TokKind.BracketOpen || left.AsDottedName().Token.Kind == primaryToken.Kind);

            Left = left;
            Left.Parent = this;
            Right = right;
            RightNode = rightNode;
            HasOnlyIdentifiers = left is FirstNameNode || (left is DottedNameNode && left.AsDottedName().HasOnlyIdentifiers);
            HasPossibleNamespaceQualifier = HasOnlyIdentifiers || left is ParentNode || left is SelfNode;
            _depth = left.Depth + 1;

            MinChildID = Math.Min(left.MinChildID, rightNode?.MinChildID ?? MinChildID);
        }

        public bool Matches(DName leftIdentifier, DName rightIdentifier)
        {
            Contracts.AssertValid(leftIdentifier);
            Contracts.AssertValid(rightIdentifier);

            return Left is FirstNameNode leftName && leftName.Ident.Name == leftIdentifier && Right.Name == rightIdentifier;
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            var left = Left.Clone(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>
            {
                { Left, left },
            };
            var rightNode = RightNode?.Clone(ref idNext, ts);
            if (rightNode != null)
            {
                newNodes.Add(RightNode, rightNode);
            }

            var clonedNode = new DottedNameNode(
                ref idNext,
                Token.Clone(ts),
                SourceList.Clone(ts, newNodes),
                left,
                Right.Clone(ts),
                rightNode);

            return clonedNode;
        }

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                Left.Accept(visitor);
                visitor.PostVisit(this);
            }
        }

        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override DottedNameNode CastDottedName()
        {
            return this;
        }

        public override DottedNameNode AsDottedName()
        {
            return this;
        }

        public DPath ToDPath()
        {
            Contracts.Assert(HasPossibleNamespaceQualifier);

            var names = new Stack<DName>(2);
            names.Push(Right.Name);

            // Traverse the DottedNameNode structure non-recursively, to account for the possibility
            // that it may be very deep. Accumulate all encountered names onto a stack.
            var pointer = this;
            var reachedLeft = false;
            while (pointer != null)
            {
                var left = pointer.Left;

                switch (left)
                {
                    case FirstNameNode firstNameNode:
                        names.Push(firstNameNode.Ident.Name);
                        reachedLeft = true;
                        break;
                    case ParentNode parentNode:
                        names.Push(new DName(TexlLexer.KeywordParent));
                        reachedLeft = true;
                        break;
                    case SelfNode selfNode:
                        names.Push(new DName(TexlLexer.KeywordSelf));
                        reachedLeft = true;
                        break;
                }

                if (reachedLeft)
                {
                    break;
                }

                pointer = left as DottedNameNode;
                if (pointer != null)
                {
                    names.Push(pointer.Right.Name);
                }
                else
                {
                    Contracts.Assert(false, "Can only do this for dotted names consisting of identifiers");
                }
            }

            // For the DPath by unwinding the names stack
            var path = DPath.Root;
            while (names.Count > 0)
            {
                path = path.Append(names.Pop());
            }

            return path;
        }

        public override Span GetTextSpan()
        {
            return new Span(Token.VerifyValue().Span.Min, Right.VerifyValue().Token.VerifyValue().Span.Lim);
        }

        public override Span GetCompleteSpan()
        {
            var min = Token.Span.Min;
            var leftNode = Left;
            while (leftNode != null)
            {
                DottedNameNode dottedLeft;
                if ((dottedLeft = leftNode.AsDottedName()) != null)
                {
                    leftNode = dottedLeft.Left;
                }
                else
                {
                    min = leftNode.GetCompleteSpan().Min;
                    break;
                }
            }

            return new Span(min, Right.VerifyValue().Token.VerifyValue().Span.Lim);
        }
    }
}