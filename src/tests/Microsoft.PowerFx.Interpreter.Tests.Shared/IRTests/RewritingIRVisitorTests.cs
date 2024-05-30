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
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
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

        [Theory]
        [InlineData(
            "'<p>' & StringValue & '</p>'",
            "Concatenate(<p>, Substitute(Substitute(Substitute(StringValue, &, &amp;), <, &lt;), >, &gt;), </p>)",
            "<p>&lt;script&gt;alert('here');&lt;/script&gt;</p>")]
        [InlineData(
            "StringValue",
            "Substitute(Substitute(Substitute(StringValue, &, &amp;), <, &lt;), >, &gt;)",
            "&lt;script&gt;alert('here');&lt;/script&gt;")]
        [InlineData(
            "$'<ul>{Concat([1,2,3], Concatenate('<li>', StringValue, ' ', Value, '</li>'))}</ul>'",
            "Concatenate(<ul>, Concat(Table({Value:1}, {Value:2}, {Value:3}), (Concatenate(<li>, Substitute(Substitute(Substitute(StringValue, &, &amp;), <, &lt;), >, &gt;),  , DecimalToText(Value), </li>))), </ul>)",
            "<ul><li>&lt;script&gt;alert('here');&lt;/script&gt; 1</li><li>&lt;script&gt;alert('here');&lt;/script&gt; 2</li><li>&lt;script&gt;alert('here');&lt;/script&gt; 3</li></ul>")]
        [InlineData(
            "First(MyTable).Name",
            "Substitute(Substitute(Substitute((First(MyTable)).Name, &, &amp;), <, &lt;), >, &gt;)",
            "Bread")]
        [InlineData(
            "LookUp(MyTable, Name = 'Bread').Value",
            "(LookUp(MyTable, (EqText(Name,Bread)))).Value",
            "3.5")]
        [InlineData(
            "Last(MyTable).Name",
            "Substitute(Substitute(Substitute((Last(MyTable)).Name, &, &amp;), <, &lt;), >, &gt;)",
            "Mac &amp; cheese")]
        [InlineData(
            "With({a:'<p>' & StringValue & '</p>'}, a & a)",
            "With({a:Concatenate(<p>, Substitute(Substitute(Substitute(StringValue, &, &amp;), <, &lt;), >, &gt;), </p>)}, (Concatenate(a, a)))",
            "<p>&lt;script&gt;alert('here');&lt;/script&gt;</p><p>&lt;script&gt;alert('here');&lt;/script&gt;</p>")]
        public void TestEscapingHtml(string expr, string expectedIR, string expectedResult)
        {
            var expression = expr.Replace("\'", "\"");
            var engine = new RecalcEngine();
            engine.UpdateVariable("StringValue", "<script>alert('here');</script>");
            var tableRecordType = new KnownRecordType(TestUtils.DT("![Name:s,Value:n]"));
            var tableVar = FormulaValue.NewTable(
                tableRecordType,
                new InMemoryRecordValue(
                    IRContext.NotInSource(tableRecordType),
                    new NamedValue("Name", FormulaValue.New("Bread")),
                    new NamedValue("Value", FormulaValue.New(3.50))),
                new InMemoryRecordValue(
                    IRContext.NotInSource(tableRecordType),
                    new NamedValue("Name", FormulaValue.New("Milk")),
                    new NamedValue("Value", FormulaValue.New(4.99))),
                new InMemoryRecordValue(
                    IRContext.NotInSource(tableRecordType),
                    new NamedValue("Name", FormulaValue.New("Cheese")),
                    new NamedValue("Value", FormulaValue.New(5.75))),
                new InMemoryRecordValue(
                    IRContext.NotInSource(tableRecordType),
                    new NamedValue("Name", FormulaValue.New("Mac & cheese")),
                    new NamedValue("Value", FormulaValue.New(2.25))));
            engine.UpdateVariable("MyTable", tableVar);

            engine.IRTransformList.Add(new EscapeHtmlTransform());

            var opts = new ParserOptions { AllowsSideEffects = true };
            var check = new CheckResult(engine)
                .SetText(expression, opts)
                .SetBindingInfo();

            var ir = check.ApplyIR(); // calls rewriter

            var actualIR = check.GetCompactIRString();

            Assert.Equal(expectedIR.Replace("\'", "\""), actualIR);

            var run = check.GetEvaluator();
            var result = run.Eval();

            var objActual = result.ToObject();
            if (objActual is not string)
            {
                objActual = objActual.ToString();
            }

            Assert.Equal(objActual, expectedResult);
        }

        private class EscapeHtmlTransform : IRTransform
        {
            public EscapeHtmlTransform()
                : base("EscapeHtml")
            {
            }

            public override IntermediateNode Transform(IntermediateNode node, ICollection<ExpressionError> errors)
            {
                var v = new TestEscapeHtmlVisitor();
                var ctx = new object();
                var ret = node.Accept(v, ctx);
                if (ret.AlreadyEscaped || ret.Node.IRContext.ResultType._type.Kind != Core.Types.DKind.String)
                {
                    return ret.Node;
                }

                return TestEscapeHtmlVisitor.WrapWithEscaping(ret.Node);
            }
        }

        private class EscapedIntermediateNode
        {
            public IntermediateNode Node { get; set; }

            public bool AlreadyEscaped { get; set; }
        }

        private class TestEscapeHtmlVisitor : RewritingIRVisitor<EscapedIntermediateNode, object>
        {
            private readonly HashSet<IntermediateNode> _alreadyEscaped = new ();

            public TestEscapeHtmlVisitor()
            {
            }

            public override IntermediateNode Materialize(EscapedIntermediateNode ret)
            {
                return ret.Node;
            }

            public override EscapedIntermediateNode Visit(TextLiteralNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // String literals do not need escaping
                return temp;
            }

            public override EscapedIntermediateNode Visit(BooleanLiteralNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Boolean literals do not need escaping
                return temp;
            }

            public override EscapedIntermediateNode Visit(ColorLiteralNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Color literals do not need escaping
                return temp;
            }

            public override EscapedIntermediateNode Visit(DecimalLiteralNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Numeric literals do not need escaping
                return temp;
            }

            public override EscapedIntermediateNode Visit(NumberLiteralNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Numeric literals do not need escaping
                return temp;
            }

            public override EscapedIntermediateNode Visit(AggregateCoercionNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Coercions do not need escaping
                return temp;
            }

            public override EscapedIntermediateNode Visit(ChainingNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Any escaping would have been done in the last node
                return temp;
            }

            public override EscapedIntermediateNode Visit(Core.IR.Nodes.ErrorNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Errors do not need escaping
                return temp;
            }

            public override EscapedIntermediateNode Visit(Core.IR.Nodes.RecordNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Any escaping would have been done inside the record
                return temp;
            }

            public override EscapedIntermediateNode Visit(Core.IR.Nodes.UnaryOpNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Any escaping would have been done inside the child node
                return temp;
            }

            public override EscapedIntermediateNode Visit(LazyEvalNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Any escaping would have been done inside the child node
                return temp;
            }

            public override EscapedIntermediateNode Visit(RecordFieldAccessNode node, object context)
            {
                var newFrom = node.From.Accept(this, context);
                
                if (ReferenceEquals(newFrom.Node, node))
                {
                    // If record was already escaped, this is also considered escaped
                    return new EscapedIntermediateNode { Node = node, AlreadyEscaped = newFrom.AlreadyEscaped };
                }

                var newNode = new RecordFieldAccessNode(node.IRContext, newFrom.Node, node.Field);

                // If record was already escaped, this is also considered escaped
                return new EscapedIntermediateNode { Node = newNode, AlreadyEscaped = newFrom.AlreadyEscaped };
            }

            public override EscapedIntermediateNode Visit(ResolvedObjectNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = false; // Consider variables NOT to be escaped
                return temp;
            }

            public override EscapedIntermediateNode Visit(ScopeAccessNode node, object context)
            {
                var temp = base.Visit(node, context);
                temp.AlreadyEscaped = true; // Any necessary escaping would have been done inside the scope definition
                return temp;
            }

            public override EscapedIntermediateNode Visit(SingleColumnTableAccessNode node, object context)
            {
                var newFrom = node.From.Accept(this, context);

                if (ReferenceEquals(newFrom.Node, node))
                {
                    // If table was already escaped, this is also considered escaped
                    return new EscapedIntermediateNode { Node = node, AlreadyEscaped = newFrom.AlreadyEscaped };
                }

                var newNode = new SingleColumnTableAccessNode(node.IRContext, newFrom.Node, node.Field);

                // If table was already escaped, this is also considered escaped
                return new EscapedIntermediateNode { Node = newNode, AlreadyEscaped = newFrom.AlreadyEscaped };
            }

            internal static Core.IR.Nodes.CallNode WrapWithEscaping(IntermediateNode node)
            {
                var result = new Core.IR.Nodes.CallNode(
                    IRContext.NotInSource(FormulaType.String),
                    BuiltinFunctionsCore.Substitute,
                    node,
                    new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "&"),
                    new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "&amp;"));

                result = new Core.IR.Nodes.CallNode(
                    IRContext.NotInSource(FormulaType.String),
                    BuiltinFunctionsCore.Substitute,
                    result,
                    new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "<"),
                    new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "&lt;"));

                result = new Core.IR.Nodes.CallNode(
                    IRContext.NotInSource(FormulaType.String),
                    BuiltinFunctionsCore.Substitute,
                    result,
                    new TextLiteralNode(IRContext.NotInSource(FormulaType.String), ">"),
                    new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "&gt;"));

                return result;
            }

            public override EscapedIntermediateNode Visit(Core.IR.Nodes.BinaryOpNode node, object context)
            {
                switch (node.Op)
                {
                    case BinaryOpKind.Concatenate:
                        var newLeft = node.Left.Accept(this, context);
                        var newRight = node.Right.Accept(this, context);

                        var escapeAdded = false;
                        if (!newLeft.AlreadyEscaped)
                        {
                            escapeAdded = true;
                            newLeft = new EscapedIntermediateNode
                            {
                                Node = WrapWithEscaping(newLeft.Node),
                                AlreadyEscaped = true
                            };
                        }

                        if (!newRight.AlreadyEscaped)
                        {
                            escapeAdded = true;
                            newRight = new EscapedIntermediateNode
                            {
                                Node = WrapWithEscaping(newRight.Node),
                                AlreadyEscaped = true
                            };
                        }

                        if (!escapeAdded)
                        {
                            // Both were already escaped
                            return new EscapedIntermediateNode { Node = node, AlreadyEscaped = true };
                        }

                        // A branch was rewritten, create new node. 
                        var newNode = new Core.IR.Nodes.BinaryOpNode(node.IRContext, node.Op, newLeft.Node, newRight.Node);
                        return new EscapedIntermediateNode { Node = newNode, AlreadyEscaped = true };
                    default:
                        var result = base.Visit(node, context);
                        result.AlreadyEscaped = true; // Consider other binary operators as already escaped
                        return result;
                }
            }

            protected override EscapedIntermediateNode Ret(IntermediateNode node)
            {
                return new EscapedIntermediateNode { Node = node, AlreadyEscaped = false };
            }

            public override EscapedIntermediateNode Visit(Core.IR.Nodes.CallNode node, object context)
            {
                if (node.Args.Count == 0)
                {
                    // Consider already escaped
                    var noArgsResult = base.Visit(node, context);
                    noArgsResult.AlreadyEscaped = true;
                    return noArgsResult;
                }

                if (node.Function == BuiltinFunctionsCore.Concatenate)
                {
                    return VisitConcatenate(node, context);
                }

                EscapedIntermediateNode arg0 = node.Args[0].Accept(this, context);
                return VisitAndEncode(node, context, arg0);
            }

            private EscapedIntermediateNode VisitConcatenate(Core.IR.Nodes.CallNode node, object context)
            {
                List<IntermediateNode> newArgs = null;
                for (int i = 0; i < node.Args.Count; i++)
                {
                    var arg = node.Args[i];
                    var ret = arg.Accept(this, context);

                    if (newArgs != null || !ret.AlreadyEscaped || !ReferenceEquals(arg, ret.Node))
                    {
                        if (newArgs == null)
                        {
                            newArgs = new List<IntermediateNode>(node.Args.Count);

                            // Copy previous
                            for (int j = 0; j < i; j++)
                            {
                                newArgs.Add(node.Args[j]);
                            }
                        }

                        var newNode = ret.AlreadyEscaped ? ret.Node : WrapWithEscaping(ret.Node);
                        newArgs.Add(newNode);
                    }
                }

                if (newArgs == null)
                {
                    return new EscapedIntermediateNode
                    {
                        Node = node,
                        AlreadyEscaped = true
                    };
                }

                return new EscapedIntermediateNode
                {
                    Node = new Core.IR.Nodes.CallNode(node.IRContext, node.Function, newArgs),
                    AlreadyEscaped = true
                };
            }

            public EscapedIntermediateNode VisitAndEncode(Core.IR.Nodes.CallNode node, object context, EscapedIntermediateNode arg0)
            {
                var (alreadyEscaped, nodes) = VisitListEncoding(node.Args, context, arg0);
                var isEscaped = alreadyEscaped;

                if (nodes == null)
                {
                    // No change
                    return new EscapedIntermediateNode { Node = node, AlreadyEscaped = isEscaped };
                }

                // Copy over to new node 
                var newNode =
                    node.Scope == null ?
                    new Core.IR.Nodes.CallNode(node.IRContext, node.Function, nodes) :
                    new Core.IR.Nodes.CallNode(node.IRContext, node.Function, node.Scope, nodes);
                return new EscapedIntermediateNode { Node = newNode, AlreadyEscaped = isEscaped };
            }

            // Return null if no change. 
            // Else returns a new copy ofthe list with changes. 
            private (bool alreadyEscaped, IList<IntermediateNode> nodes) VisitListEncoding(IList<IntermediateNode> list, object context, EscapedIntermediateNode arg0 = default)
            {
                List<IntermediateNode> newArgs = null;
                var alreadyEscaped = arg0.AlreadyEscaped;

                for (int i = 0; i < list.Count; i++)
                {
                    var arg = list[i];
                    var ret = (i == 0 && arg0 != null) ? arg0 : arg.Accept(this, context);

                    var result = ret;
                    alreadyEscaped &= ret.AlreadyEscaped;

                    if (newArgs == null && !ReferenceEquals(arg, result.Node))
                    {
                        newArgs = new List<IntermediateNode>(list.Count);

                        // Copy previous
                        for (int j = 0; j < i; j++)
                        {
                            newArgs.Add(list[j]);
                        }
                    }

                    newArgs?.Add(result.Node);
                }

                return (alreadyEscaped: alreadyEscaped, nodes: newArgs);
            }
        }
    }
}
