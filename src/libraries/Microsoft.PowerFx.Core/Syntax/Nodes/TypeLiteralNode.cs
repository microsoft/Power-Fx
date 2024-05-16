// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    // CallNode :: Type ( TypeLiteralNode )
    // CallNode :: IsType ( Expr, TypeLiteralNode )
    // CallNode :: AsType ( Expr, TypeLiteralNode )
    // TypeLiteralNode will allow us to store information about a new kind of syntax, that being the Type Literal syntax, and manipulate it.
    // TypeLiteral syntax needs to *only* work for these partial-compile time functions that evaluate the Type Literal syntax into some sort of 
    // Representation so we can evaluate and compare it. We want to make sure that this Type Literal information isn't able to be
    // Passed around in undesirable ways be users of PowerFx. A TypeLiteralNode allows us to strictly enforce requirements.

    public sealed class TypeLiteralNode : TexlNode
    {
        private IEnumerable<TexlError> _errors;

        internal TexlNode TypeRoot { get; }

        internal TypeLiteralNode(ref int idNext, Token firstToken, TexlNode type, SourceList sources)
            : base(ref idNext, firstToken, sources)
        {
            TypeRoot = type;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new TypeLiteralNode(ref idNext, Token.Clone(ts).As<Token>(), TypeRoot, this.SourceList.Clone(ts, new Dictionary<TexlNode, TexlNode>()));
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.TypeLiteral;

        internal bool IsValid(out IEnumerable<TexlError> errors)
        {
            if (_errors == null)
            {
                var validator = new Validator();
                this.TypeRoot.Accept(validator);
                this._errors = validator.Errors;
            }

            errors = _errors;
            return !_errors.Any();
        }

        private class Validator : TexlVisitor
        {
            private readonly List<TexlError> _errors;

            internal IEnumerable<TexlError> Errors => _errors;

            public Validator()
            {
                _errors = new List<TexlError>();
            }

            // Valid Nodes
            public override void Visit(FirstNameNode node)
            {
            }

            public override bool PreVisit(RecordNode node)
            {
                return true;
            }

            public override void PostVisit(RecordNode node)
            {
            }

            public override bool PreVisit(TableNode node)
            {
                if (node.ChildNodes.Count > 1)
                {
                    _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                    return false;
                }

                return true;
            }

            public override void PostVisit(TableNode node)
            {
            }

            // Invalid nodes
            public override void Visit(ErrorNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override void Visit(BlankNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override void Visit(BoolLitNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override void Visit(StrLitNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override void Visit(NumLitNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override void Visit(DecLitNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override void Visit(ParentNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override void Visit(SelfNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override void Visit(TypeLiteralNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
            }

            public override bool PreVisit(StrInterpNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                return false;
            }

            public override bool PreVisit(DottedNameNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                return false;
            }

            public override bool PreVisit(UnaryOpNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                return false;
            }

            public override bool PreVisit(BinaryOpNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                return false;
            }

            public override bool PreVisit(VariadicOpNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                return false;
            }

            public override bool PreVisit(CallNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                return false;
            }

            public override bool PreVisit(ListNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                return false;
            }

            public override bool PreVisit(AsNode node)
            {
                _errors.Add(new TexlError(node, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, node.ToString()));
                return false;
            }

            // Do nothing in PostVisit for these nodes as we fail and add errors in PreVisit for these nodes
            public override void PostVisit(StrInterpNode node)
            {
            }

            public override void PostVisit(DottedNameNode node)
            {
            }

            public override void PostVisit(UnaryOpNode node)
            {
            }

            public override void PostVisit(BinaryOpNode node)
            {
            }

            public override void PostVisit(VariadicOpNode node)
            {
            }

            public override void PostVisit(CallNode node)
            {
            }

            public override void PostVisit(ListNode node)
            {
            }

            public override void PostVisit(AsNode node)
            {
            }
        }
    }
}
