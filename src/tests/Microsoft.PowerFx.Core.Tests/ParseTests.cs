// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DocumentServer.Core.Tests.Formulas
{
    [TestClass]
    public class ParseTests
    {
        [TestMethod, Owner("ragru")]
        public void TexlParseNumericLiterals()
        {
            TestRoundtrip("0");
            TestRoundtrip("-0");
            TestRoundtrip("1");
            TestRoundtrip("-1");
            TestRoundtrip("1.0", "1");
            TestRoundtrip("-1.0", "-1");
            TestRoundtrip("1.123456789");
            TestRoundtrip("-1.123456789");
            TestRoundtrip("0.0", "0");
            TestRoundtrip("0.000000", "0");
            TestRoundtrip("0.000001", "1E-06");
            TestRoundtrip("0.123456789");
            TestRoundtrip("-0.0", "-0");
            TestRoundtrip("-0.000000", "-0");
            TestRoundtrip("-0.000001", "-1E-06");
            TestRoundtrip("-0.123456789");
            TestRoundtrip("0.99999999");
            TestRoundtrip("9.99999999");
            TestRoundtrip("-0.99999999");
            TestRoundtrip("-9.99999999");
            TestRoundtrip("-100");
            TestRoundtrip("10e4", "100000");
            TestRoundtrip("10e-4", "0.001");
            TestRoundtrip("10e+4", "100000");
            TestRoundtrip("-10e4", "-100000");
            TestRoundtrip("-10e-4", "-0.001");
            TestRoundtrip("-10e+4", "-100000");
            TestRoundtrip("123456789");
            TestRoundtrip("-123456789");
            TestRoundtrip("123456789.987654321", "123456789.98765433");
            TestRoundtrip("-123456789.987654321", "-123456789.98765433");
            TestRoundtrip("2.E5", "200000");
        }

        [DataTestMethod, Owner("ragru")]
        [DataRow("1.2.3")]
        [DataRow(".2.3")]
        [DataRow("2eee5")]
        [DataRow("2EEE5")]
        [DataRow("2e.5")]
        public void TexlParseNumericLiterals_Negative(string script)
        {
            TestParseErrors(script, 1, StringResources.Get(TexlStrings.ErrOperatorExpected));
        }

        [DataTestMethod, Owner("ragru")]
        [DataRow("2e999")]
        [DataRow("4E88888")]
        [DataRow("-123e4567")]
        [DataRow("7E1111111")]
        public void TexlParseLargeNumerics_Negative(string script)
        {
            TestParseErrors(script, 1, StringResources.Get(TexlStrings.ErrNumberTooLarge));
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseBoolLiterals()
        {
            TestRoundtrip("true");
            TestRoundtrip("false");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseStringLiterals()
        {
            TestRoundtrip("\"\"");
            TestRoundtrip("\"\"\"\"");
            TestRoundtrip("\" \"");
            TestRoundtrip("\"                                             \"");
            TestRoundtrip("\"hello world from Texl\"");
            TestRoundtrip("\"12345\"");
            TestRoundtrip("\"12345.12345\"");
            TestRoundtrip("\"true\"");
            TestRoundtrip("\"false\"");
            TestRoundtrip("\"Not an 'identifier' but a string\"");
            TestRoundtrip("\"Expert's opinion\"");
            TestRoundtrip("\"String with \"\"escaped\"\" \\\\ chars...\"");
            TestRoundtrip("\"\\n\\f\\r\\t\\v\\b\"");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseStringLiteralsWithEscapableCharacters()
        {
            TestRoundtrip("\"Newline  \n characters   \r   galore  \u00085\"");
            TestRoundtrip("\"And \u2028    some   \u2029   more!\"");
            TestRoundtrip("\"Other supported ones:  \t\b\v\f\0\'     \"");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseArithmeticOperators()
        {
            TestRoundtrip("1 + 1");
            TestRoundtrip("1 + 2 + 3 + 4");
            TestRoundtrip("1 * 2 + 3 * 4");
            TestRoundtrip("1 * 2 * 3 + 4 * 5");
            TestRoundtrip("1 * 2 * 3 * 4 * 5");
            TestRoundtrip("2 - 1", "2 + -1");
            TestRoundtrip("2 - 1 - 2 - 3 - 4", "2 + -1 + -2 + -3 + -4");
            TestRoundtrip("2^3");
            TestRoundtrip("123.456^9");
            TestRoundtrip("123.456^-9");
            TestRoundtrip("2 / 3");
            TestRoundtrip("2 / 0");
            TestRoundtrip("1234e3 / 1234", "1234000 / 1234");
            TestRoundtrip("1234e-3 / 1234.5678", "1.234 / 1234.5678");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseLogicalOperators()
        {
            TestRoundtrip("A || B");
            TestRoundtrip("A || B || C");
            TestRoundtrip("A && B");
            TestRoundtrip("A && B && C");
            TestRoundtrip("A && B || C && D");
            TestRoundtrip("A || B && C || D");
            TestRoundtrip("(A || B) && (C || D)");
            TestRoundtrip("!A");
            TestRoundtrip("! A", expected: "!A");
            TestRoundtrip("!!!!!!!!!A");
            TestRoundtrip("! ! ! ! ! ! ! ! ! A", expected: "!!!!!!!!!A");
            TestRoundtrip("!    !    !!!!    !!!     A", expected: "!!!!!!!!!A");
            TestRoundtrip("!A || !B || D");
            TestRoundtrip("!(A || B) && !(C && D)");
            TestRoundtrip("!!!!!!!!!(A || B || C && D)");

            TestRoundtrip("!false");
            TestRoundtrip("!true");
            TestRoundtrip("true || true");
            TestRoundtrip("true || false");
            TestRoundtrip("false || false");
            TestRoundtrip("false || true");
            TestRoundtrip("true && true");
            TestRoundtrip("true && false");
            TestRoundtrip("false && true");
            TestRoundtrip("false && false");
            TestRoundtrip("true && true && true && true && true");
            TestRoundtrip("false && false && false && false && false");

            TestRoundtrip("Price = 1200");
            TestRoundtrip("Gender = \"Female\"");

            TestRoundtrip("A = B");
            TestRoundtrip("A < B");
            TestRoundtrip("A <= B");
            TestRoundtrip("A >= B");
            TestRoundtrip("A > B");
            TestRoundtrip("A <> B");

            // Note that we are parsing these, but internally they will be binary trees: "((1 < 2) < 3) < 4", etc.
            TestRoundtrip("1 < 2 < 3 < 4", expectedNodeKind: NodeKind.BinaryOp);
            TestRoundtrip("1 < 2 >= 3 < 4", expectedNodeKind: NodeKind.BinaryOp);
            TestRoundtrip("1 <= 2 < 3 <= 4", expectedNodeKind: NodeKind.BinaryOp);
            TestRoundtrip("4 > 3 > 2 > 1", expectedNodeKind: NodeKind.BinaryOp);
            TestRoundtrip("4 > 3 >= 2 > 1", expectedNodeKind: NodeKind.BinaryOp);
            TestRoundtrip("1 < 2 = 3 <> 4", expectedNodeKind: NodeKind.BinaryOp);

            TestRoundtrip("true = false");
            TestRoundtrip("true <> false");
            TestRoundtrip("Gender <> \"Male\"");
        }

        [TestMethod, Owner("lesaltzm")]
        [DataRow("A Or B")]
        [DataRow("A Or B Or C")]
        [DataRow("A And B")]
        [DataRow("A And B And C")]
        [DataRow("A And B || C And D")]
        [DataRow("A Or B And C Or D")]
        [DataRow("(A Or B) And (C Or D)")]
        [DataRow("Not A")]
        [DataRow("Not Not Not Not A")]
        [DataRow("Not A Or Not B Or D")]
        [DataRow("Not (A Or B) And Not (C And D)")]
        [DataRow("Not Not Not Not Not (A Or B Or C And D)")]
        public void TexlParseKeywordLogicalOperators(string script)
        {
            TestRoundtrip(script);
        }

        [TestMethod, Owner("lesaltzm")]
        [DataRow("Or(A, B)")]
        [DataRow("Or(A, B Or C)")]
        [DataRow("And(A, B)")]
        [DataRow("And(A && C, B || D)")]
        [DataRow("And(A And B, C)")]
        [DataRow("Not(A)")]
        [DataRow("And(Not(A Or B), Not(C And D))")]
        [DataRow("Not(Not !Not(Not (A Or B Or C And D)))")]
        public void TexlParseLogicalOperatorsAndFunctions(string script)
        {
            TestRoundtrip(script);
        }

        [TestMethod, Owner("lesaltzm")]
        [DataRow("A As B")]
        [DataRow("A As B As C")]
        [DataRow("F(A, B, C) As D")]
        [DataRow("A && B As C")]
        [DataRow("A.B As C")]
        [DataRow("F(A As B, C)")]
        [DataRow("A * B As C")]
        [DataRow("A As B * C")]
        public void TexlParseAsOperator(string script)
        {
            TestRoundtrip(script);
        }

        [TestMethod, Owner("lesaltzm")]
        [DataRow("A As (B As C)")]
        [DataRow("A As F(B)")]
        [DataRow("A As (((B)))")]
        public void TexlParseAsOperator_Negative(string script)
        {
            TestParseErrors(script);
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseDoubleAmpVsSingleAmp()
        {
            // Test the correct parsing of double- vs. single-ampersand.

            // Double-ampersand should resolve to the logical conjunction operator.
            TestRoundtrip("A && B",
                expectedNodeKind: NodeKind.BinaryOp,
                customTest: node =>
                {
                    Assert.AreEqual(BinaryOp.And, node.AsBinaryOp().Op);
                });

            // Single-ampersand should resolve to the concatenation operator.
            TestRoundtrip("A & B",
                expectedNodeKind: NodeKind.BinaryOp,
                customTest: node =>
                {
                    Assert.AreEqual(BinaryOp.Concat, node.AsBinaryOp().Op);
                });

            // A triple-amp on the other hand should trigger a parse error.
            TestParseErrors("A &&& B", count: 1);
        }

        [TestMethod, Owner("hekum")]
        public void TexlParseBlank()
        {
            // Test the correct parsing of Blank node.
            TestRoundtrip("", "", expectedNodeKind: NodeKind.Blank);
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseIdentifiers()
        {
            // Unqualified identifiers
            TestRoundtrip("A");
            TestRoundtrip("A12345");
            TestRoundtrip("'The name of a table'");
            TestRoundtrip("'A000'");
            TestRoundtrip("'__ '");
            TestRoundtrip("'A                                           _'");
            TestRoundtrip("'A                                           A'");
            TestRoundtrip("'A                                           123'");

            // Identifiers with bangs (e.g. qualified entity names)
            TestRoundtrip("A!B");
            TestRoundtrip("A!B!C");
            TestRoundtrip("A!'Some Column'!C");
            TestRoundtrip("'Some Table'!'Some Column'");
            TestRoundtrip("GlobalTable!B!C!D");
            TestRoundtrip("'My Table'!ColA!ColB!'ColC'");

            // Disambiguated global identifiers
            TestRoundtrip("[@foo]");
            TestRoundtrip("[@'foo with blanks']");
            TestRoundtrip("[@foo123]");
            TestRoundtrip("[@'A!B!C']");
            TestRoundtrip("[@'A!B!C']!X");
            TestRoundtrip("[@'A!B!C']!X!Y!Z");

            // Disambiguated scope fields
            TestRoundtrip("foo[@bar]");
            TestRoundtrip("foo[@bar]!X");
            TestRoundtrip("foo[@bar]!X!Y!Z");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseDottedIdentifiers()
        {
            TestRoundtrip("A.B.C");
            TestRoundtrip("A.'Some Column'.C");
            TestRoundtrip("'Some Table'.'Some Column'");
            TestRoundtrip("GlobalTable.B.C.D");
            TestRoundtrip("'My Table'.ColA.ColB.'ColC'");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseIdentifiersNegative()
        {
            // Identifiers can't be all-blank.
            TestParseErrors("' '");
            TestParseErrors("'     '");
            TestParseErrors("'                                          '");

            // Can't mix dot and bang within the same identifier.
            TestParseErrors("A!B.C");
            TestParseErrors("A.B!C");
            TestParseErrors("A.B.C.D.E.F.G!H");
            TestParseErrors("A!B!C!D!E!F!G.H");

            // Missing delimiters
            TestParseErrors("'foo");
            TestParseErrors("foo'");

            // Disambiguated identifiers and scope fields
            TestParseErrors("@");
            TestParseErrors("@[]");
            TestParseErrors("[@@@@@@@@@@]");
            TestParseErrors("[@]");
            TestParseErrors("[@    ]");
            TestParseErrors("[@foo!bar]");
            TestParseErrors("[@foo.bar]");
            TestParseErrors("[@'']");
            TestParseErrors("[@\"\"]");
            TestParseErrors("[@1234]");
            TestParseErrors("X![@foo]");
            TestParseErrors("X.[@foo]");
            TestParseErrors("X!Y!Z![@foo]");
            TestParseErrors("X.Y.Z.[@foo]");
            TestParseErrors("X!Y!Z[@foo]");
            TestParseErrors("X.Y.Z[@foo]");
            TestParseErrors("[@foo][@bar]");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseNull()
        {
            // The language does not / no longer supports a null constant.
            // Out-of-context nulls are parsed as unbound identifiers.
            TestRoundtrip("null", expectedNodeKind: NodeKind.FirstName);
            TestRoundtrip("null && null");
            TestRoundtrip("null || null");
            TestRoundtrip("!null");
            TestRoundtrip("A = null");
            TestRoundtrip("A < null");
            TestRoundtrip("B > null");
            TestRoundtrip("NULL", expectedNodeKind: NodeKind.FirstName);
            TestRoundtrip("Null", expectedNodeKind: NodeKind.FirstName);
            TestRoundtrip("nuLL", expectedNodeKind: NodeKind.FirstName);
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseThisItem()
        {
            TestRoundtrip("ThisItem");
            TestRoundtrip("ThisItem!Price");
            TestRoundtrip("ThisItem.Price");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseParent()
        {
            TestRoundtrip("Parent", expectedNodeKind: NodeKind.Parent);
            TestRoundtrip("Parent!Width");
            TestRoundtrip("Parent.Width");
        }

        [TestMethod, Owner("lesaltzm")]
        public void TexlParseSelf()
        {
            TestRoundtrip("Self", expectedNodeKind: NodeKind.Self);
            TestRoundtrip("Self!Width");
            TestRoundtrip("Self.Width");
            TestRoundtrip("If(Self.Width < 2, Self.Height, Self.X)");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseFunctionCalls()
        {
            TestRoundtrip("Concatenate(A, B)");
            TestRoundtrip("Abs(-12)");
            TestRoundtrip("If(A < 2, A, 2)");
            TestRoundtrip("Count(A!B!C)");
            TestRoundtrip("Count(A.B.C)");
            TestRoundtrip("Abs(12) + Abs(-12) + Abs(45) + Abs(-45)");
        }

        [TestMethod, Owner("ragru")]
        public void TexlParseFunctionCallsNegative()
        {
            TestParseErrors("DateValue(,", 2);
            TestParseErrors("DateValue(,,", 3);
            TestParseErrors("DateValue(,,,,,,,,,", 10);
            TestParseErrors("DateValue(Now(),,", 2);
        }


        [TestMethod, Owner("ragru")]
        public void TexlParseNamespaceQualifiedFunctionCalls()
        {
            TestRoundtrip("Facebook!GetFriends()", expected: "Facebook.GetFriends()");
            TestRoundtrip("Facebook.GetFriends()");
            TestRoundtrip("Netflix!CatalogServices!GetRecentlyAddedTitles()", expected: "Netflix.CatalogServices.GetRecentlyAddedTitles()");
            TestRoundtrip("Netflix.CatalogServices.GetRecentlyAddedTitles()");
        }

        [TestMethod, Owner("ragru")]
        public void TexlCallHeadNodes()
        {
            TestRoundtrip("GetSomething()",
                customTest: node =>
                {
                    Assert.IsTrue(node is CallNode);
                    Assert.IsNull(node.AsCall().HeadNode);
                    Assert.IsNotNull(node.AsCall().Head);
                    Assert.IsTrue(node.AsCall().Head is Identifier);
                    Assert.IsTrue((node.AsCall().Head as Identifier).Namespace.IsRoot);
                });

            TestRoundtrip("Netflix!Services!GetMovieCatalog()", expected: "Netflix.Services.GetMovieCatalog()",
                customTest: node =>
                {
                    Assert.IsTrue(node is CallNode);

                    Assert.IsNotNull(node.AsCall().Head);
                    Assert.IsTrue(node.AsCall().Head is Identifier);
                    Assert.IsFalse((node.AsCall().Head as Identifier).Namespace.IsRoot);
                    Assert.AreEqual("Netflix.Services", (node.AsCall().Head as Identifier).Namespace.ToDottedSyntax("."));

                    Assert.IsNotNull(node.AsCall().HeadNode);
                    Assert.IsTrue(node.AsCall().HeadNode is DottedNameNode);
                    Assert.AreEqual("Netflix.Services.GetMovieCatalog", node.AsCall().HeadNode.AsDottedName().ToDPath().ToDottedSyntax("."));
                });
        }

        [TestMethod, Owner("lesaltzm")]
        public void TestReservedAsIdentifier()
        {
            // "As" Ident cannot be a reserved keyword
            TestParseErrors("Filter([1,2,3] As Self, 'Self'.Value > 2)", 3);
        }

        [Timeout(1000)]
        [DataTestMethod, Owner("ragru")]
        [DataRow(
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(" +
            "Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(Text(")]
        [DataRow(
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(")]
        [DataRow("A!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!")]
        [DataRow("A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A")]
        [DataRow("A............................................................")]
        [DataRow("A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A")]
        public void TexlExcessivelyDeepRules(string script)
        {
            TestParseErrors(script, count: 1, errorMessage: StringResources.Get(TexlStrings.ErrRuleNestedTooDeeply));
        }

        [DataTestMethod, Owner("janewby")]
        [DataRow("")]
        [DataRow("  ")]
        [DataRow("//LineComment")]
        [DataRow("/* Block Comment Closed */")]
        public void TestBlankNodesAndCommentNodeOnlys(string script)
        {
            var result = TexlParser.ParseScript(script);
            var node = result.Root;

            Assert.IsNotNull(node);
            Assert.IsNull(result.Errors);
        }

        [DataTestMethod, Owner("janewby")]
        [DataRow("/* Block Comment no end")]
        public void TexlTestCommentingSemantics_Negative(string script)
        {
            TestParseErrors(script);
        }

        [TestMethod, Owner("lesaltzm")]
        [DataRow("true and false")]
        [DataRow("\"a\" In \"astring\"")]
        [DataRow("\"a\" ExaCtIn \"astring\"")]
        [DataRow("true ANd false")]
        public void TestBinaryOpCaseParseErrors(string script)
        {
            TestParseErrors(script);
        }

        [DataTestMethod, Owner("ragru")]
        [DataRow("a!b", "a.b")]
        [DataRow("a.b", "a.b")]
        [DataRow("a[@b]", "a.b")]
        [DataRow("a!b!c", "a.b.c")]
        [DataRow("a.b.c", "a.b.c")]
        [DataRow("a!b!c!d!e!f!g!h!i!j!k!l!m!n!o!p!q!r!s!t!w!x!y!z", "a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.w.x.y.z")]
        [DataRow("a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.w.x.y.z", "a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.w.x.y.z")]
        public void TestNodeToDPath(string script, string dpath)
        {
            var result = TexlParser.ParseScript(script);
            var node = result.Root;

            Assert.IsNotNull(node);
            Assert.IsNull(result.Errors);
            Assert.IsTrue(node is DottedNameNode);

            DottedNameNode dotted = node as DottedNameNode;
            Assert.AreEqual(dpath, dotted.ToDPath().ToDottedSyntax(punctuator: "."));
        }

        [TestMethod, Owner("ragru")]
        public void TestParseRecords()
        {
            TestRoundtrip("{}", "{  }");
            TestRoundtrip("{  }");
            TestRoundtrip("{ A:10 }");
            TestRoundtrip("{ WhateverIdentifierHere:10 }");
            TestRoundtrip("{ 'someFieldName':10 }");
            TestRoundtrip("{ 'somefield   weird identifier with spaces and stuff...':10, 'some...and another one':true }");
            TestRoundtrip("{ A:10, B:\"hello\", C:true }");
            TestRoundtrip("{ A:Abs(12) + Abs(-12) + Abs(45) + Abs(-45), B:Nz(A), C:X!Y + Y!Z }");
            TestRoundtrip("{ A:Abs(12) + Abs(-12) + Abs(45) + Abs(-45), B:Nz(A), C:X.Y + Y.Z }");

            // Nested
            TestRoundtrip("{ A:{  }, B:{  }, C:{  } }");
            TestRoundtrip("{ A:{ X:10, Y:true }, B:{ Z:\"Hello\" }, C:{ W:{  } } }");
            TestRoundtrip("{ A:{ X:10, Y:true }, B:{ 'ZZZZZ':\"Hello\" }, C:{ 'WWW WWWW WWWW':{  } } }");
        }

        [TestMethod, Owner("ragru")]
        public void TestParseRecordsNegative()
        {
            TestParseErrors("{{}}");
            TestParseErrors("{ , }");
            TestParseErrors("{A:1, }");
            TestParseErrors("{A:1,,, }");
            TestParseErrors("{A:1, B:2,,, }");
            TestParseErrors("{ . . . }");
            TestParseErrors("{ some identifiers }");
            TestParseErrors("{ {some identifiers} }");
            TestParseErrors("{{}, {}, {}}");
            TestParseErrors("{ 10 20 30 }");
            TestParseErrors("{10, 20, 30}");
            TestParseErrors("{A; B; C}");
            TestParseErrors("{A:1, B C}");
            TestParseErrors("{A, B, C}");
            TestParseErrors("{A B C}");
            TestParseErrors("{true, false, true, true, false}");
            TestParseErrors("{\"a\", \"b\", \"c\"}");
            TestParseErrors("{:, :, :}");
            TestParseErrors("{A:10; B:30; C:40}");
            TestParseErrors("{A:10, , , , C:30}");
            TestParseErrors("{{}:A}");
            TestParseErrors("{10:B, 20:C}");
            TestParseErrors("{A:10 B:20 C:30}");
            TestParseErrors("{A:10 . B:20 . C:30}");
            TestParseErrors("{A;10, B;20, C;30}");
            TestParseErrors("{A=20, B=30, C=40}");
            TestParseErrors("{A:=20, B:=30, C:=true}");
            TestParseErrors("{A:20+}");
            TestParseErrors("{A:20, B:30; }");
            TestParseErrors("{A:20, B:30 ++ }");
        }

        [TestMethod, Owner("ragru")]
        public void TestParseTables()
        {
            TestRoundtrip("[]", "[  ]");
            TestRoundtrip("[  ]");
            TestRoundtrip("[ 1, 2, 3, 4, 5 ]");
            TestRoundtrip("[ Abs(12), Abs(-12), Abs(45), Abs(-45), X!Y + Y!Z ]");
            TestRoundtrip("[ Abs(12), Abs(-12), Abs(45), Abs(-45), X.Y + Y.Z ]");
            TestRoundtrip("[ \"a\", \"b\", \"c\" ]");

            // Nested
            TestRoundtrip("[ [ 1, 2, 3 ], [ 4, 5, 6 ], [ \"a\", \"b\", \"c\" ] ]");
        }

        [TestMethod, Owner("ragru")]
        public void TestParseTables_Negative()
        {
            TestParseErrors("[a:10]");
            TestParseErrors("[a:10, b:20]");
            TestParseErrors("[10; 20; 30]");
            TestParseErrors("[10 20 30]");
        }

        [TestMethod, Owner("emhommer")]
        public void TestFormulasParse()
        {
            TestFormulasParseRoundtrip("a=10;");
            TestFormulasParseRoundtrip("a=b=10;");
            TestFormulasParseRoundtrip("a=10;c=20;");
        }

        [TestMethod, Owner("emhommer")]
        public void TestFormulasParse_Negative()
        {
            TestFormulasParseError("a=10"); // missing ;
            TestFormulasParseError("a;"); // missing = and expression
            TestFormulasParseError(";"); // missing identifier and expression
            TestFormulasParseError("a=a=10;"); // circular reference
        }

        internal void TestRoundtrip(string script, string expected = null, NodeKind expectedNodeKind = NodeKind.Error, Action<TexlNode> customTest = null)
        {
            var result = TexlParser.ParseScript(script);
            var node = result.Root;
            Assert.IsNotNull(node);
            Assert.IsFalse(result.HasError);

            int startid = node.Id;
            // Test cloning
            TexlNode clone = node.Clone(ref startid, default(Span));
            Assert.AreEqual(TexlPretty.PrettyPrint(node), TexlPretty.PrettyPrint(clone), false);

            if (expected == null)
                expected = script;

            Assert.AreEqual(expected, TexlPretty.PrettyPrint(node), false);

            if (expectedNodeKind != NodeKind.Error)
                Assert.AreEqual(expectedNodeKind, node.Kind);

            if (customTest != null)
                customTest(node);
        }

        internal void TestParseErrors(string script, int count = 1, string errorMessage = null)
        {
            var result = TexlParser.ParseScript(script);
            Assert.IsNotNull(result.Root);
            Assert.IsTrue(result.HasError);
            Assert.IsTrue(result.Errors.Count >= count);
            //Assert.IsTrue(result.Errors.All(err => err.ErrorKind == DocumentErrorKind.AXL && err.TextSpan != null));
            Assert.IsTrue(errorMessage == null || result.Errors.Any(err => err.ShortMessage == errorMessage));
        }

        internal void TestFormulasParseRoundtrip(string script, string expected = null, NodeKind expectedNodeKind = NodeKind.Error, Action<TexlNode> customTest = null)
        {
            var result = TexlParser.ParseFormulasScript(script);

            Assert.IsTrue(result.NamedFormulas.Count > 0);
            Assert.IsFalse(result.HasError);
        }

        internal void TestFormulasParseError(string script, string expected = null, NodeKind expectedNodeKind = NodeKind.Error, Action<TexlNode> customTest = null)
        {
            var result = TexlParser.ParseFormulasScript(script);

            Assert.IsTrue(result.NamedFormulas.Count == 0 || result.HasError);
        }
    }
}
