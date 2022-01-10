// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    /// Base class for all parse nodes.
    internal abstract class TexlNode
    {
        private TexlNode _parent;
        private bool? _usesChains;
        protected int _depth;

        public readonly int Id;
        public int MinChildID { get; protected set; }

        public readonly Token Token;

        public SourceList SourceList { get; private set; }

        public int Depth => _depth;

        public TexlNode Parent
        {
            get => _parent;
            set
            {
                Contracts.Assert(_parent == null);
                _parent = value;
            }
        }

        public bool UsesChains
        {
            get
            {
                if (_usesChains.HasValue)
                    return _usesChains.Value;

                _usesChains = ChainTrackerVisitor.Run(this);
                return _usesChains.Value;
            }
        }


        protected TexlNode(ref int idNext, Token primaryToken, SourceList sourceList)
        {
            Contracts.Assert(idNext >= 0);
            Contracts.AssertValue(primaryToken);
            Contracts.AssertValue(sourceList);

            Id = idNext++;
            MinChildID = Id;
            Token = primaryToken;
            SourceList = sourceList;
            _depth = 1;
        }

        public abstract TexlNode Clone(ref int idNext, Span ts);

        public abstract void Accept(TexlVisitor visitor);

        public abstract Result Accept<Result, Context>(TexlFunctionalVisitor<Result, Context> visitor, Context context);

        public abstract NodeKind Kind { get; }

        public void Parser_SetSourceList(SourceList sources)
        {
            Contracts.AssertValue(sources);
            SourceList = sources;
        }

        public virtual Span GetTextSpan()
        {
            return new Span(Token.VerifyValue().Span.Min, Token.VerifyValue().Span.Lim);
        }

        public virtual Span GetCompleteSpan()
        {
            return new Span(GetTextSpan());
        }

        public Span GetSourceBasedSpan()
        {
            if (SourceList.Tokens.Count() == 0)
                return GetCompleteSpan();

            var start = SourceList.Tokens.First().Span.Min;
            var end = SourceList.Tokens.Last().Span.Lim;
            return new Span(start, end);
        }

        public virtual ErrorNode AsError()
        {
            return null;
        }

        public virtual FirstNameNode CastFirstName()
        {
            Contracts.Assert(false);
            return (FirstNameNode)this;
        }

        public virtual FirstNameNode AsFirstName()
        {
            return null;
        }

        public virtual ParentNode AsParent()
        {
            return null;
        }

        public virtual ReplaceableNode AsReplaceable()
        {
            return null;
        }

        public virtual SelfNode AsSelf()
        {
            return null;
        }

        public virtual DottedNameNode CastDottedName()
        {
            Contracts.Assert(false);
            return (DottedNameNode)this;
        }

        public virtual DottedNameNode AsDottedName()
        {
            return null;
        }

        public virtual NumLitNode AsNumLit()
        {
            return null;
        }

        public virtual StrLitNode AsStrLit()
        {
            return null;
        }

        public virtual BoolLitNode CastBoolLit()
        {
            Contracts.Assert(false);
            return (BoolLitNode)this;
        }

        public virtual BoolLitNode AsBoolLit()
        {
            return null;
        }

        public virtual UnaryOpNode CastUnaryOp()
        {
            Contracts.Assert(false);
            return (UnaryOpNode)this;
        }

        public virtual UnaryOpNode AsUnaryOpLit()
        {
            return null;
        }

        public virtual BinaryOpNode CastBinaryOp()
        {
            Contracts.Assert(false);
            return (BinaryOpNode)this;
        }

        public virtual BinaryOpNode AsBinaryOp()
        {
            return null;
        }

        public virtual VariadicOpNode AsVariadicOp()
        {
            return null;
        }

        public virtual ListNode CastList()
        {
            Contracts.Assert(false);
            return (ListNode)this;
        }

        public virtual ListNode AsList()
        {
            return null;
        }

        public virtual CallNode CastCall()
        {
            Contracts.Assert(false);
            return (CallNode)this;
        }

        public virtual CallNode AsCall()
        {
            return null;
        }

        public virtual RecordNode CastRecord()
        {
            Contracts.Assert(false);
            return (RecordNode)this;
        }

        public virtual RecordNode AsRecord()
        {
            return null;
        }

        public virtual TableNode AsTable()
        {
            return null;
        }

        public virtual BlankNode AsBlank()
        {
            return null;
        }

        public virtual AsNode AsAsNode()
        {
            return null;
        }

        public bool InTree(TexlNode root)
        {
            Contracts.AssertValue(root);
            return root.MinChildID <= Id && Id <= root.Id;
        }

        public override string ToString()
        {
            return TexlPretty.PrettyPrint(this);
        }

        // Returns the nearest node to the cursor position. If the node has child nodes returns the nearest child node.
        public static TexlNode FindNode(TexlNode rootNode, int cursorPosition)
        {
            Contracts.AssertValue(rootNode);
            Contracts.Assert(cursorPosition >= 0);

            return FindNodeVisitor.Run(rootNode, cursorPosition);
        }

        internal TexlNode FindTopMostDottedParentOrSelf()
        {
            var parent = this;

            while (parent != null && parent.Parent != null && parent.Parent.Kind == NodeKind.DottedName)
            {
                parent = parent.Parent;
            }

            return parent;
        }
   }
}