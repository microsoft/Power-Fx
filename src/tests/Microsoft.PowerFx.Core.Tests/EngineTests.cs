// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class EngineTests
    {
        [Fact]
        public void AddPostCheckErrors()
        {
            var expr = "User()";
            var engine = new Engine();
            engine.PostCheckErrorHandlers.Add(new TestPostCheckErrorHandler());

            var check = engine.Check(expr);
            Assert.Contains(check.Errors, error => error.Message == "User function is not Supported, instead use the User object (Ie User, but without parenthesis).");
        }

        private class TestPostCheckErrorHandler : IPostCheckErrorHandler
        {
            public IEnumerable<ExpressionError> Process(TexlNode root)
            {
                var visitor = new ErrorVisitor();
                root.Accept(visitor);
                return visitor.Errors;
            }
        }

        private class ErrorVisitor : TexlVisitor
        {
            private readonly IList<ExpressionError> _errors = new List<ExpressionError>();

            public IEnumerable<ExpressionError> Errors => _errors;

            public override void PostVisit(StrInterpNode node)
            {
                throw new NotImplementedException();
            }

            public override void PostVisit(DottedNameNode node)
            {
                throw new NotImplementedException();
            }

            public override void PostVisit(UnaryOpNode node)
            {
                throw new NotImplementedException();
            }

            public override void PostVisit(BinaryOpNode node)
            {
                throw new NotImplementedException();
            }

            public override void PostVisit(VariadicOpNode node)
            {
                throw new NotImplementedException();
            }

            public override void PostVisit(CallNode node)
            {
                if (node.Head.Name == "User")
                {
                    _errors.Add(new ExpressionError()
                    {
                        Message = "User function is not Supported, instead use the User object (Ie User, but without parenthesis).",
                        Span = node.GetTextSpan()
                    });
                }
            }

            public override void PostVisit(ListNode node)
            {
                return;
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
                throw new NotImplementedException();
            }

            public override void Visit(ParentNode node)
            {
                throw new NotImplementedException();
            }

            public override void Visit(SelfNode node)
            {
                throw new NotImplementedException();
            }
        }
    }
}
