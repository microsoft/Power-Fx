// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class ParseTests : PowerFxTest
    {      
        [Theory]
        [InlineData("0")]
        [InlineData("-0")]
        [InlineData("1")]
        [InlineData("-1")]
        [InlineData("1.0", "1")]
        [InlineData("-1.0", "-1")]
        [InlineData("1.123456789")]
        [InlineData("-1.123456789")]
        [InlineData("0.0", "0")]
        [InlineData("0.000000", "0")]
        [InlineData("0.000001", "1E-06")]
        [InlineData("0.123456789")]
        [InlineData("-0.0", "-0")]
        [InlineData("-0.000000", "-0")]
        [InlineData("-0.000001", "-1E-06")]
        [InlineData("-0.123456789")]
        [InlineData("0.99999999")]
        [InlineData("9.99999999")]
        [InlineData("-0.99999999")]
        [InlineData("-9.99999999")]
        [InlineData("-100")]
        [InlineData("10e4", "100000")]
        [InlineData("10e-4", "0.001")]
        [InlineData("10e+4", "100000")]
        [InlineData("-10e4", "-100000")]
        [InlineData("-10e-4", "-0.001")]
        [InlineData("-10e+4", "-100000")]
        [InlineData("123456789")]
        [InlineData("-123456789")]
        [InlineData("123456789.987654321", "123456789.98765433")]
        [InlineData("-123456789.987654321", "-123456789.98765433")]
        [InlineData("2.E5", "200000")]
        public void TexlParseNumericLiterals(string script, string expected = null)
        {
            TestRoundtrip(script, expected);
        }

        [Theory]
        [InlineData("1.2.3")]
        [InlineData(".2.3")]
        [InlineData("2eee5")]
        [InlineData("2EEE5")]
        [InlineData("2e.5")]
        public void TexlParseNumericLiterals_Negative(string script)
        {
            TestParseErrors(script, 1, StringResources.Get(TexlStrings.ErrOperatorExpected));
        }

        [Theory]
        [InlineData("2e999")]
        [InlineData("4E88888")]
        [InlineData("-123e4567")]
        [InlineData("7E1111111")]
        public void TexlParseLargeNumerics_Negative(string script)
        {
            TestParseErrors(script, 1, StringResources.Get(TexlStrings.ErrNumberTooLarge));
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        public void TexlParseBoolLiterals(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"\"\"\"")]
        [InlineData("\" \"")]
        [InlineData("\"                                             \"")]
        [InlineData("\"hello world from Texl\"")]
        [InlineData("\"12345\"")]
        [InlineData("\"12345.12345\"")]
        [InlineData("\"true\"")]
        [InlineData("\"false\"")]
        [InlineData("\"Not an 'identifier' but a string\"")]
        [InlineData("\"Expert's opinion\"")]
        [InlineData("\"String with \"\"escaped\"\" \\\\ chars...\"")]
        [InlineData("\"\\n\\f\\r\\t\\v\\b\"")]
        public void TexlParseStringLiterals(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("\"Newline  \n characters   \r   galore  \u00085\"")]
        [InlineData("\"And \u2028    some   \u2029   more!\"")]
        [InlineData("\"Other supported ones:  \t\b\v\f\0\'     \"")]
        public void TexlParseStringLiteralsWithEscapableCharacters(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("1 + 1")]
        [InlineData("1 + 2 + 3 + 4")]
        [InlineData("1 * 2 + 3 * 4")]
        [InlineData("1 * 2 * 3 + 4 * 5")]
        [InlineData("1 * 2 * 3 * 4 * 5")]
        [InlineData("2 - 1", "2 + -1")]
        [InlineData("2 - 1 - 2 - 3 - 4", "2 + -1 + -2 + -3 + -4")]
        [InlineData("2^3")]
        [InlineData("123.456^9")]
        [InlineData("123.456^-9")]
        [InlineData("2 / 3")]
        [InlineData("2 / 0")]
        [InlineData("1234e3 / 1234", "1234000 / 1234")]
        [InlineData("1234e-3 / 1234.5678", "1.234 / 1234.5678")]
        public void TexlParseArithmeticOperators(string script, string expected = null)
        {
            TestRoundtrip(script, expected);
        }

        [Theory]
        [InlineData("A || B")]
        [InlineData("A || B || C")]
        [InlineData("A && B")]
        [InlineData("A && B && C")]
        [InlineData("A && B || C && D")]
        [InlineData("A || B && C || D")]
        [InlineData("(A || B) && (C || D)")]
        [InlineData("!A")]
        [InlineData("! A", "!A")]
        [InlineData("!!!!!!!!!A")]
        [InlineData("! ! ! ! ! ! ! ! ! A", "!!!!!!!!!A")]
        [InlineData("!    !    !!!!    !!!     A", "!!!!!!!!!A")]
        [InlineData("!A || !B || D")]
        [InlineData("!(A || B) && !(C && D)")]
        [InlineData("!!!!!!!!!(A || B || C && D)")]

        [InlineData("!false")]
        [InlineData("!true")]
        [InlineData("true || true")]
        [InlineData("true || false")]
        [InlineData("false || false")]
        [InlineData("false || true")]
        [InlineData("true && true")]
        [InlineData("true && false")]
        [InlineData("false && true")]
        [InlineData("false && false")]
        [InlineData("true && true && true && true && true")]
        [InlineData("false && false && false && false && false")]

        [InlineData("Price = 1200")]
        [InlineData("Gender = \"Female\"")]

        [InlineData("A = B")]
        [InlineData("A < B")]
        [InlineData("A <= B")]
        [InlineData("A >= B")]
        [InlineData("A > B")]
        [InlineData("A <> B")]

        // Note that we are parsing these, but internally they will be binary trees: "((1 < 2) < 3) < 4", etc.
        [InlineData("1 < 2 < 3 < 4", null, NodeKind.BinaryOp)]
        [InlineData("1 < 2 >= 3 < 4", null, NodeKind.BinaryOp)]
        [InlineData("1 <= 2 < 3 <= 4", null, NodeKind.BinaryOp)]
        [InlineData("4 > 3 > 2 > 1", null, NodeKind.BinaryOp)]
        [InlineData("4 > 3 >= 2 > 1", null, NodeKind.BinaryOp)]
        [InlineData("1 < 2 = 3 <> 4", null, NodeKind.BinaryOp)]

        [InlineData("true = false")]
        [InlineData("true <> false")]
        [InlineData("Gender <> \"Male\"")]
        internal void TexlParseLogicalOperators(string script, string expected = null, NodeKind expectedNodeKind = NodeKind.Error)
        {
            TestRoundtrip(script, expected, expectedNodeKind);
        }

        [Theory]
        [InlineData("A Or B")]
        [InlineData("A Or B Or C")]
        [InlineData("A And B")]
        [InlineData("A And B And C")]
        [InlineData("A And B || C And D")]
        [InlineData("A Or B And C Or D")]
        [InlineData("(A Or B) And (C Or D)")]
        [InlineData("Not A")]
        [InlineData("Not Not Not Not A")]
        [InlineData("Not A Or Not B Or D")]
        [InlineData("Not (A Or B) And Not (C And D)")]
        [InlineData("Not Not Not Not Not (A Or B Or C And D)")]
        public void TexlParseKeywordLogicalOperators(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("Or(A, B)")]
        [InlineData("Or(A, B Or C)")]
        [InlineData("And(A, B)")]
        [InlineData("And(A && C, B || D)")]
        [InlineData("And(A And B, C)")]
        [InlineData("Not(A)")]
        [InlineData("And(Not(A Or B), Not(C And D))")]
        [InlineData("Not(Not !Not(Not (A Or B Or C And D)))")]
        public void TexlParseLogicalOperatorsAndFunctions(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("A As B")]
        [InlineData("A As B As C")]
        [InlineData("F(A, B, C) As D")]
        [InlineData("A && B As C")]
        [InlineData("A.B As C")]
        [InlineData("F(A As B, C)")]
        [InlineData("A * B As C")]
        [InlineData("A As B * C")]
        public void TexlParseAsOperator(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("A As (B As C)")]
        [InlineData("A As F(B)")]
        [InlineData("A As (((B)))")]
        public void TexlParseAsOperator_Negative(string script)
        {
            TestParseErrors(script);
        }

        [Fact]
        public void TexlParseDoubleAmpVsSingleAmp()
        {
            // Test the correct parsing of double- vs. single-ampersand.

            // Double-ampersand should resolve to the logical conjunction operator.
            TestRoundtrip(
                "A && B",
                expectedNodeKind: NodeKind.BinaryOp,
                customTest: node =>
                {
                    Assert.Equal(BinaryOp.And, node.AsBinaryOp().Op);
                });

            // Single-ampersand should resolve to the concatenation operator.
            TestRoundtrip(
                "A & B",
                expectedNodeKind: NodeKind.BinaryOp,
                customTest: node =>
                {
                    Assert.Equal(BinaryOp.Concat, node.AsBinaryOp().Op);
                });

            // A triple-amp on the other hand should trigger a parse error.
            TestParseErrors("A &&& B", count: 1);
        }

        [Theory]
        [InlineData("", "", NodeKind.Blank)]
        internal void TexlParseBlank(string script, string expected, NodeKind expectedNodeKind)
        {
            // Test the correct parsing of Blank node.
            TestRoundtrip(script, expected, expectedNodeKind);
        }

        [Theory]

        // Unqualified identifiers
        [InlineData("A")]
        [InlineData("A12345")]
        [InlineData("'The name of a table'")]
        [InlineData("'A000'")]
        [InlineData("'__ '")]
        [InlineData("'A                                           _'")]
        [InlineData("'A                                           A'")]
        [InlineData("'A                                           123'")]

        // Identifiers with bangs (e.g. qualified entity names)
        [InlineData("A!B")]
        [InlineData("A!B!C")]
        [InlineData("A!'Some Column'!C")]
        [InlineData("'Some Table'!'Some Column'")]
        [InlineData("GlobalTable!B!C!D")]
        [InlineData("'My Table'!ColA!ColB!'ColC'")]

        // Disambiguated global identifiers
        [InlineData("[@foo]")]
        [InlineData("[@'foo with blanks']")]
        [InlineData("[@foo123]")]
        [InlineData("[@'A!B!C']")]
        [InlineData("[@'A!B!C']!X")]
        [InlineData("[@'A!B!C']!X!Y!Z")]

        // Disambiguated scope fields
        [InlineData("foo[@bar]")]
        [InlineData("foo[@bar]!X")]
        [InlineData("foo[@bar]!X!Y!Z")]
        public void TexlParseIdentifiers(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("A.B.C")]
        [InlineData("A.'Some Column'.C")]
        [InlineData("'Some Table'.'Some Column'")]
        [InlineData("GlobalTable.B.C.D")]
        [InlineData("'My Table'.ColA.ColB.'ColC'")]
        public void TexlParseDottedIdentifiers(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]

        // Identifiers can't be all-blank.
        [InlineData("' '")]
        [InlineData("'     '")]
        [InlineData("'                                          '")]

        // Can't mix dot and bang within the same identifier.
        [InlineData("A!B.C")]
        [InlineData("A.B!C")]
        [InlineData("A.B.C.D.E.F.G!H")]
        [InlineData("A!B!C!D!E!F!G.H")]

        // Missing delimiters
        [InlineData("'foo")]
        [InlineData("foo'")]

        // Disambiguated identifiers and scope fields
        [InlineData("@")]
        [InlineData("@[]")]
        [InlineData("[@@@@@@@@@@]")]
        [InlineData("[@]")]
        [InlineData("[@    ]")]
        [InlineData("[@foo!bar]")]
        [InlineData("[@foo.bar]")]
        [InlineData("[@'']")]
        [InlineData("[@\"\"]")]
        [InlineData("[@1234]")]
        [InlineData("X![@foo]")]
        [InlineData("X.[@foo]")]
        [InlineData("X!Y!Z![@foo]")]
        [InlineData("X.Y.Z.[@foo]")]
        [InlineData("X!Y!Z[@foo]")]
        [InlineData("X.Y.Z[@foo]")]
        [InlineData("[@foo][@bar]")]
        public void TexlParseIdentifiersNegative(string script)
        {
            // Identifiers can't be all-blank.
            TestParseErrors(script);
        }

        [Theory]

        // The language does not / no longer supports a null constant.
        // Out-of-context nulls are parsed as unbound identifiers.
        [InlineData("null", NodeKind.FirstName)]
        [InlineData("null && null")]
        [InlineData("null || null")]
        [InlineData("!null")]
        [InlineData("A = null")]
        [InlineData("A < null")]
        [InlineData("B > null")]
        [InlineData("NULL", NodeKind.FirstName)]
        [InlineData("Null", NodeKind.FirstName)]
        [InlineData("nuLL", NodeKind.FirstName)]
        internal void TexlParseNull(string script, NodeKind expectedNodeKind = NodeKind.Error)
        {
            // The language does not / no longer supports a null constant.
            // Out-of-context nulls are parsed as unbound identifiers.
            TestRoundtrip(script, expectedNodeKind: expectedNodeKind);
        }

        [Theory]
        [InlineData("ThisItem")]
        [InlineData("ThisItem!Price")]
        [InlineData("ThisItem.Price")]
        public void TexlParseThisItem(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("Parent", NodeKind.Parent)]
        [InlineData("Parent!Width")]
        [InlineData("Parent.Width")]
        internal void TexlParseParent(string script, NodeKind expectedNodeKind = NodeKind.Error)
        {
            TestRoundtrip(script, expectedNodeKind: expectedNodeKind);
        }

        [Theory]
        [InlineData("Self", NodeKind.Self)]
        [InlineData("Self!Width")]
        [InlineData("Self.Width")]
        [InlineData("If(Self.Width < 2, Self.Height, Self.X)")]
        internal void TexlParseSelf(string script, NodeKind expectedNodeKind = NodeKind.Error)
        {
            TestRoundtrip(script, null, expectedNodeKind);
        }

        [Theory]
        [InlineData("Concatenate(A, B)")]
        [InlineData("Abs(-12)")]
        [InlineData("If(A < 2, A, 2)")]
        [InlineData("Count(A!B!C)")]
        [InlineData("Count(A.B.C)")]
        [InlineData("Abs(12) + Abs(-12) + Abs(45) + Abs(-45)")]
        public void TexlParseFunctionCalls(string script)
        {
            TestRoundtrip(script);
        }

        [Theory]
        [InlineData("DateValue(,", 2)]
        [InlineData("DateValue(,,", 3)]
        [InlineData("DateValue(,,,,,,,,,", 10)]
        [InlineData("DateValue(Now(),,", 2)]
        public void TexlParseFunctionCallsNegative(string script, int expected)
        {
            TestParseErrors(script, expected);
        }

        [Theory]
        [InlineData("Facebook!GetFriends()", "Facebook.GetFriends()")]
        [InlineData("Facebook.GetFriends()")]
        [InlineData("Netflix!CatalogServices!GetRecentlyAddedTitles()", "Netflix.CatalogServices.GetRecentlyAddedTitles()")]
        [InlineData("Netflix.CatalogServices.GetRecentlyAddedTitles()")]
        public void TexlParseNamespaceQualifiedFunctionCalls(string script, string expected = null)
        {
            TestRoundtrip(script, expected);
        }

        [Fact]
        public void TexlCallHeadNodes()
        {
            TestRoundtrip(
                "GetSomething()",
                customTest: node =>
                {
                    Assert.True(node is CallNode);
                    Assert.Null(node.AsCall().HeadNode);
                    Assert.NotNull(node.AsCall().Head);
                    Assert.True(node.AsCall().Head is Identifier);
                    Assert.True((node.AsCall().Head as Identifier).Namespace.IsRoot);
                });

            TestRoundtrip(
                "Netflix!Services!GetMovieCatalog()", 
                expected: "Netflix.Services.GetMovieCatalog()",
                customTest: node =>
                {
                    Assert.True(node is CallNode);

                    Assert.NotNull(node.AsCall().Head);
                    Assert.True(node.AsCall().Head is Identifier);
                    Assert.False((node.AsCall().Head as Identifier).Namespace.IsRoot);
                    Assert.Equal("Netflix.Services", (node.AsCall().Head as Identifier).Namespace.ToDottedSyntax());

                    Assert.NotNull(node.AsCall().HeadNode);
                    Assert.True(node.AsCall().HeadNode is DottedNameNode);
                    Assert.Equal("Netflix.Services.GetMovieCatalog", node.AsCall().HeadNode.AsDottedName().ToDPath().ToDottedSyntax());
                });
        }

        [Theory]

        // "As" Ident cannot be a reserved keyword
        [InlineData("Filter([1,2,3] As Self, 'Self'.Value > 2)", 3)]
        public void TestReservedAsIdentifier(string script, int expected)
        {
            TestParseErrors(script, expected);
        }

        //[Timeout(1000)]
        [Theory]
        [InlineData(
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
        [InlineData(
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(" +
            "0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(0+(1+(2+(3+(4+(5+(6+(7+(8+(9+(")]
        [InlineData("A!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!")]
        [InlineData("A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A!A")]
        [InlineData("A............................................................")]
        [InlineData("A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A.A")]
        public void TexlExcessivelyDeepRules(string script)
        {
            TestParseErrors(script, count: 1, errorMessage: StringResources.Get(TexlStrings.ErrRuleNestedTooDeeply));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("//LineComment")]
        [InlineData("/* Block Comment Closed */")]
        public void TestBlankNodesAndCommentNodeOnlys(string script)
        {
            var result = TexlParser.ParseScript(script);
            var node = result.Root;

            Assert.NotNull(node);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData("/* Block Comment no end")]
        public void TexlTestCommentingSemantics_Negative(string script)
        {
            TestParseErrors(script);
        }

        [Theory]
        [InlineData("true and false")]
        [InlineData("\"a\" In \"astring\"")]
        [InlineData("\"a\" ExaCtIn \"astring\"")]
        [InlineData("true ANd false")]
        public void TestBinaryOpCaseParseErrors(string script)
        {
            TestParseErrors(script);
        }

        [Theory]
        [InlineData("a!b", "a.b")]
        [InlineData("a.b", "a.b")]
        [InlineData("a[@b]", "a.b")]
        [InlineData("a!b!c", "a.b.c")]
        [InlineData("a.b.c", "a.b.c")]
        [InlineData("a!b!c!d!e!f!g!h!i!j!k!l!m!n!o!p!q!r!s!t!w!x!y!z", "a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.w.x.y.z")]
        [InlineData("a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.w.x.y.z", "a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.w.x.y.z")]
        public void TestNodeToDPath(string script, string dpath)
        {
            var result = TexlParser.ParseScript(script);
            var node = result.Root;

            Assert.NotNull(node);
            Assert.Empty(result.Errors);
            Assert.True(node is DottedNameNode);

            var dotted = node as DottedNameNode;
            Assert.Equal(dpath, dotted.ToDPath().ToDottedSyntax());
        }

        [Theory]
        [InlineData("{}", "{  }")]
        [InlineData("{  }")]
        [InlineData("{ A:10 }")]
        [InlineData("{ WhateverIdentifierHere:10 }")]
        [InlineData("{ 'someFieldName':10 }")]
        [InlineData("{ 'somefield   weird identifier with spaces and stuff...':10, 'some...and another one':true }")]
        [InlineData("{ A:10, B:\"hello\", C:true }")]
        [InlineData("{ A:Abs(12) + Abs(-12) + Abs(45) + Abs(-45), B:Nz(A), C:X!Y + Y!Z }")]
        [InlineData("{ A:Abs(12) + Abs(-12) + Abs(45) + Abs(-45), B:Nz(A), C:X.Y + Y.Z }")]

        // Nested
        [InlineData("{ A:{  }, B:{  }, C:{  } }")]
        [InlineData("{ A:{ X:10, Y:true }, B:{ Z:\"Hello\" }, C:{ W:{  } } }")]
        [InlineData("{ A:{ X:10, Y:true }, B:{ 'ZZZZZ':\"Hello\" }, C:{ 'WWW WWWW WWWW':{  } } }")]
        public void TestParseRecords(string script, string expected = null)
        {
            TestRoundtrip(script, expected);
        }

        [Theory]
        [InlineData("{{}}")]
        [InlineData("{ , }")]
        [InlineData("{A:1, }")]
        [InlineData("{A:1,,, }")]
        [InlineData("{A:1, B:2,,, }")]
        [InlineData("{ . . . }")]
        [InlineData("{ some identifiers }")]
        [InlineData("{ {some identifiers} }")]
        [InlineData("{{}, {}, {}}")]
        [InlineData("{ 10 20 30 }")]
        [InlineData("{10, 20, 30}")]
        [InlineData("{A; B; C}")]
        [InlineData("{A:1, B C}")]
        [InlineData("{A, B, C}")]
        [InlineData("{A B C}")]
        [InlineData("{true, false, true, true, false}")]
        [InlineData("{\"a\", \"b\", \"c\"}")]
        [InlineData("{:, :, :}")]
        [InlineData("{A:10; B:30; C:40}")]
        [InlineData("{A:10, , , , C:30}")]
        [InlineData("{{}:A}")]
        [InlineData("{10:B, 20:C}")]
        [InlineData("{A:10 B:20 C:30}")]
        [InlineData("{A:10 . B:20 . C:30}")]
        [InlineData("{A;10, B;20, C;30}")]
        [InlineData("{A=20, B=30, C=40}")]
        [InlineData("{A:=20, B:=30, C:=true}")]
        [InlineData("{A:20+}")]
        [InlineData("{A:20, B:30; }")]
        [InlineData("{A:20, B:30 ++ }")]
        public void TestParseRecordsNegative(string script)
        {
            TestParseErrors(script);
        }

        [Theory]
        [InlineData("[]", "[  ]")]
        [InlineData("[  ]")]
        [InlineData("[ 1, 2, 3, 4, 5 ]")]
        [InlineData("[ Abs(12), Abs(-12), Abs(45), Abs(-45), X!Y + Y!Z ]")]
        [InlineData("[ Abs(12), Abs(-12), Abs(45), Abs(-45), X.Y + Y.Z ]")]
        [InlineData("[ \"a\", \"b\", \"c\" ]")]

        // Nested
        [InlineData("[ [ 1, 2, 3 ], [ 4, 5, 6 ], [ \"a\", \"b\", \"c\" ] ]")]
        public void TestParseTables(string script, string expected = null)
        {
            TestRoundtrip(script, expected);

            // Nested
            TestRoundtrip("[ [ 1, 2, 3 ], [ 4, 5, 6 ], [ \"a\", \"b\", \"c\" ] ]");
        }

        [Theory]
        [InlineData("[a:10]")]
        [InlineData("[a:10, b:20]")]
        [InlineData("[10; 20; 30]")]
        [InlineData("[10 20 30]")]
        public void TestParseTables_Negative(string script)
        {
            TestParseErrors(script);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("AFormula = 10;")]
        [InlineData("A_Formula = 10;")]
        [InlineData("'A Formula  ' = 10;")]
        [InlineData("'Formul@  ^%' = 10;")]
        [InlineData("   a    =  10    ;  ")]
        [InlineData("a = b = 10;")]
        [InlineData("a = 10; c = 20;")]
        public void TestFormulasParse(string script)
        {
            TestFormulasParseRoundtrip(script);
        }

        [Theory]
        [InlineData("a = 10")]
        [InlineData("Formul@ = 10; b = 20;")]
        [InlineData("a;")]
        [InlineData(";")]
        [InlineData("a = 10;;")]
        [InlineData("a = 10; b")]
        [InlineData("A = If(true,1;);")]
        [InlineData("A = If(true,1;2);")]
        [InlineData("1 + 2);")]
        [InlineData("1 = 10")]
        [InlineData("a.b = c")]
        public void TestFormulasParse_Negative(string script)
        {
            TestFormulasParseError(script);
        }

        [Theory]
        [InlineData("A;B;C", "A ; B ; C")]
        [InlineData("Foo(1);Bar(2)", "Foo(1) ; Bar(2)")]
        public void TestChainParse(string script, string expected = null)
        {
            TestRoundtrip(script, expected, flags: TexlParser.Flags.EnableExpressionChaining);
        }

        internal void TestRoundtrip(string script, string expected = null, NodeKind expectedNodeKind = NodeKind.Error, Action<TexlNode> customTest = null, TexlParser.Flags flags = TexlParser.Flags.None)
        {
            var result = TexlParser.ParseScript(script, flags: flags);
            var node = result.Root;            
                        
            Assert.NotNull(node);
            Assert.False(result.HasError, result.ParseErrorText);

            var startid = node.Id;

            // Test cloning
            var clone = node.Clone(ref startid, default);
            Assert.Equal(TexlPretty.PrettyPrint(node), TexlPretty.PrettyPrint(clone), false);

            if (expected == null)
            {
                expected = script;
            }

            Assert.Equal(expected, TexlPretty.PrettyPrint(node), false);

            if (expectedNodeKind != NodeKind.Error)
            {
                Assert.Equal(expectedNodeKind, node.Kind);
            }

            customTest?.Invoke(node);
        }

        internal void TestParseErrors(string script, int count = 1, string errorMessage = null)
        {
            var result = TexlParser.ParseScript(script);
            Assert.NotNull(result.Root);
            Assert.True(result.HasError);
            Assert.True(result._errors.Count >= count);

            //Assert.IsTrue(result.Errors.All(err => err.ErrorKind == DocumentErrorKind.AXL && err.TextSpan != null));
            Assert.True(errorMessage == null || result._errors.Any(err => err.ShortMessage == errorMessage));
        }

        internal void TestFormulasParseRoundtrip(string script)
        {
            var result = TexlParser.ParseFormulasScript(script);

            Assert.False(result.HasError);
        }

        internal void TestFormulasParseError(string script)
        {
            var result = TexlParser.ParseFormulasScript(script);

            Assert.True(result.HasError);
        }
    }
}
