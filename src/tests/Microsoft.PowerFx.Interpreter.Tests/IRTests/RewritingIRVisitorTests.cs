// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Logging;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Syntax;
using Xunit;
using static Microsoft.PowerFx.Core.Parser.TexlParser;

namespace Microsoft.PowerFx.Tests
{
    public sealed class RewritingIRVisitorTests
    {
        // TestWriter changes all number literals X to X *100. 
        // This lets us test many rewrite combinations. 
        [Theory]
        [InlineData("1", "100", 100.0)]
        [InlineData("-1", "NegateDecimal(100)", -100.0)]
        [InlineData("1+2", "AddDecimals(100,200)", 300.0)]
        [InlineData("{ x : 1, y:true}.x", "({x:100, y:True}).x", 100.0)]
        [InlineData("Mid(\"ABCDEF\", 2, 3) ", "Mid(ABCDEF, Float(200), Float(300))", "")]
        [InlineData("Sum(1,2,3)", "Sum(100, 200, 300)", 600.0)]
        [InlineData("true And 1", "And(True, (DecimalToBoolean(100)))", true)] // LazyEvalNode
        [InlineData("N200 = 2", "EqNumbers(N200,Float(200))", true)]
        [InlineData("1;true", "(100;True;)", true)]
        [InlineData("With( {x:1}, x*2)", "With({x:100}, (MulDecimals(x,200)))", 20000.0)]

        // Unchanged
        [InlineData("true+true", "AddDecimals(BooleanToDecimal(True),BooleanToDecimal(True))", 2.0)]
        [InlineData("true = false", "EqBoolean(True,False)", false)] // LazyEvalNode
        [InlineData("true And false", "And(True, (False))", false)] // LazyEvalNode
        [InlineData("{ x : true}.x", "({x:True}).x", true)]
        [InlineData("true;false", "(True;False;)", false)]
        public void Rewrite100x(string expr, string expectedIR, object expected)
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("N200", 200.0);
            engine.IRTransformList.Add(new Rewrite100Transform());

            var opts = new ParserOptions { AllowsSideEffects = true };
            var check = new CheckResult(engine)
                .SetText(expr, opts)
                .SetBindingInfo();

            var ir = check.ApplyIR(); // calls rewriter

            var actualIR = check.GetCompactIRString();

            Assert.Equal(expectedIR, actualIR);

            var run = check.GetEvaluator();
            var result = run.Eval();

            var objActual = result.ToObject();
            if (objActual is decimal d)
            {
                Assert.Equal((double)expected, (double)d);
            }            
            else
            {
                Assert.Equal(expected, objActual);
            }
        }

        // When we don't rewrite, preserve the same object reference identity 
        [Fact]
        public void PreserveObjectReference()
        {
            var expr = "Sum(1, { x : 4}.x)";

            var engine = new Engine();
            var check = new CheckResult(engine)
                .SetText(expr)
                .SetBindingInfo();
            var ir = check.ApplyIR();

            var errors = new List<ExpressionError>();
            var t = new NopTransform(); 
            var before = ir.TopNode;
            var after = t.Transform(before, errors);

            Assert.Same(before, after);
        }

        // Errors 
        [Theory]
        [InlineData("x", "", "Error 0-1: Name isn't valid. 'x' isn't recognized.")] // bind error, stop
        [InlineData("888", "A;B;", "Warning 0-3: Warn888")] // warning at Transform1, continue 
        [InlineData("999", "A;", "Error 0-3: Error999")] // error at Transform1, stop
        [InlineData("1", "A;B;")] // success
        public void DontCallIfErrors(string expr, string expectedLog, string expectedErrorMessage = null)
        {
            var sb = new StringBuilder();
            var engine = new RecalcEngine();
            engine.IRTransformList.Add(new LogCallTransform("A", sb));
            engine.IRTransformList.Add(new LogCallTransform("B", sb));

            var check = new CheckResult(engine)
                .SetText(expr)
                .SetBindingInfo();

            var errors = check.ApplyErrors(); // never throws

            if (expectedErrorMessage == null)
            {
                Assert.Empty(errors);
            }
            else
            {
                Assert.Single(errors);
                var err = errors.First();
                Assert.Equal(expectedErrorMessage, err.ToString());
            }

            Assert.Equal(expectedLog, sb.ToString());
        }

        // Ensure this transform is not called.
        // for example, if previous stage has errors, we shouldn't try IR transforms.
        private class LogCallTransform : IRTransform
        {
            private readonly StringBuilder _sb;
            private readonly string _marker;

            public LogCallTransform(string marker, StringBuilder sb)
            {
                _marker = marker;
                _sb = sb;
            }

            public override IntermediateNode Transform(IntermediateNode node, ICollection<ExpressionError> errors)
            {
                _sb.Append(_marker);
                var value = ((DecimalLiteralNode)node).LiteralValue;

                if (_marker == "A" && value == 888)
                {
                    errors.Add(new ExpressionError
                    {
                        Message = "Warn888",
                        Severity = ErrorSeverity.Warning,
                        Span = node.IRContext.SourceContext
                    });
                }

                if (value == 999)
                {
                    errors.Add(new ExpressionError
                    {
                        Message = "Error999",
                        Severity = ErrorSeverity.Critical,
                        Span = node.IRContext.SourceContext
                    });
                }

                _sb.Append(";");

                return node;
            }
        }

        // Nop visitor - doesn't make any changes. 
        private class NopTransform : IRTransform
        {
            public override IntermediateNode Transform(IntermediateNode node, ICollection<ExpressionError> errors)
            {
                var v = new NopVisitor();
                var ret = node.Accept(v, null);
                return ret;
            }

            private class NopVisitor : RewritingIRVisitor<IntermediateNode, object>
            {                
                public override IntermediateNode Materialize(IntermediateNode ret)
                {
                    return ret;
                }

                protected override IntermediateNode Ret(IntermediateNode node)
                {
                    return node;
                }
            }
        }

        // Trivial rewriter to change all numbers to * 100. 
        // Inject an error for 999.
        private class Rewrite100Transform : IRTransform
        {
            public Rewrite100Transform()
                : base("TestRewriter")
            {
            }

            public override IntermediateNode Transform(IntermediateNode node, ICollection<ExpressionError> errors)
            {
                var v = new TestRewrite100Visitor(errors);
                var ctx = new TestRewrite100Visitor.Context();
                var ret = node.Accept(v, ctx);
                return ret;
            }
        }

        private class TestRewrite100Visitor : RewritingIRVisitor<IntermediateNode, TestRewrite100Visitor.Context>
        {
            private readonly ICollection<ExpressionError> _errors;

            public TestRewrite100Visitor(ICollection<ExpressionError> errors)
            {
                _errors = errors;
            }

            public override IntermediateNode Materialize(IntermediateNode ret)
            {
                return ret;
            }

            protected override IntermediateNode Ret(IntermediateNode node)
            {
                return node;
            }

            public class Context
            {
            }

            public override IntermediateNode Visit(DecimalLiteralNode node, Context context)
            {
                var value = node.LiteralValue;

                return new DecimalLiteralNode(node.IRContext, value * 100);
            }
        }
    }
}
