// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public sealed class LexerTests : PowerFxTest
    {
        private static void AssertTokens(string value, params TokKind[] tokKinds)
        {
            var tokens = TexlLexer.InvariantLexer.LexSource(value);
            Assert.NotNull(tokens);
            Assert.Equal(tokKinds.Length, tokens.Count);
            Assert.True(tokens.Zip(tokKinds, (t, k) => t.Kind == k).All(b => b));
        }

        [Fact]
        public void TestTryNameOrIdentifierToName()
        {
            Assert.True(TexlLexer.TryNameOrIdentifierToName(" Name   ", out var name));
            Assert.Equal("Name", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName(" Name   Abcd ", out name));
            Assert.Equal("Name   Abcd", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName(" Name   Abcd", out name));
            Assert.Equal("Name   Abcd", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("Name   Abcd", out name));
            Assert.Equal("Name   Abcd", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("Name\bAbcd", out name));
            Assert.Equal("Name\bAbcd", name);

            Assert.False(TexlLexer.TryNameOrIdentifierToName(string.Empty, out _));
            Assert.False(TexlLexer.TryNameOrIdentifierToName("   ", out _));

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   'Escaped Name'", out name));
            Assert.Equal("Escaped Name", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   'Escaped '' Name'", out name));
            Assert.Equal("Escaped ' Name", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   'Escaped '' Name'''", out name));
            Assert.Equal("Escaped ' Name'", name);

            Assert.False(TexlLexer.TryNameOrIdentifierToName("   '   ' ", out _));

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   ''''", out name));
            Assert.Equal("'", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   'Escaped Name'   ", out name));
            Assert.Equal("Escaped Name", name);

            Assert.False(TexlLexer.TryNameOrIdentifierToName("   'Escaped Name   ", out _));
            Assert.False(TexlLexer.TryNameOrIdentifierToName("   'Escaped Name  ' abc ", out _));

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   '#a!>>?'", out name));
            Assert.Equal("#a!>>?", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   '123\u00ae'", out name));
            Assert.Equal("123\u00ae", name);
        }

        internal static bool IsInRangeInclusive(char ch, Tuple<char, char>[] range)
        {
            for (var i = 0; i < range.Length; i++)
            {
                if (range[i].Item1 <= ch && ch <= range[i].Item2)
                {
                    return true;
                }
            }

            return false;
        }

        [Fact]
        public void TestIsIdentStart()
        {
            var identStartRanges = new Tuple<char, char>[]
            {
                Tuple.Create('A', 'Z'),
                Tuple.Create('a', 'z'),
                Tuple.Create('_', '_'),
                Tuple.Create('\'', '\''),
            };

            for (var i = 0; i < 128; i++)
            {
                var ch = (char)i;
                Assert.Equal(IsInRangeInclusive(ch, identStartRanges), TexlLexer.IsIdentStart(ch));
            }
        }

        [Fact]
        public void TestZeroWidthSpaceCharacters()
        {
            string value;

            // No Zero Width Space after comma
            value = "ClearCollect(Temp1,[])";
            var tokens = TexlLexer.InvariantLexer.LexSource(value, TexlLexer.Flags.None);
            Assert.NotNull(tokens);
            Assert.Equal(8, tokens.Count);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.ParenOpen, tokens[1].Kind);
            Assert.Equal(TokKind.Ident, tokens[2].Kind);
            Assert.Equal(TokKind.Comma, tokens[3].Kind);
            Assert.Equal(TokKind.BracketOpen, tokens[4].Kind);
            Assert.Equal(TokKind.BracketClose, tokens[5].Kind);
            Assert.Equal(TokKind.ParenClose, tokens[6].Kind);
            Assert.Equal(TokKind.Eof, tokens[7].Kind);

            // Zero Width Space after comma
            value = "ClearCollect(Temp1," + char.ConvertFromUtf32(8203) + "[])";
            tokens = TexlLexer.InvariantLexer.LexSource(value, TexlLexer.Flags.None);
            Assert.NotNull(tokens);
            Assert.Equal(9, tokens.Count);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.ParenOpen, tokens[1].Kind);
            Assert.Equal(TokKind.Ident, tokens[2].Kind);
            Assert.Equal(TokKind.Comma, tokens[3].Kind);
            Assert.Equal(TokKind.Error, tokens[4].Kind);
            Assert.Equal(2, (tokens[4] as ErrorToken).ResourceKeyFormatStringArgs.Length);
            Assert.Equal((tokens[4] as ErrorToken).DetailErrorKey.Value, TexlStrings.UnexpectedCharacterToken);
            Assert.Equal(TokKind.BracketOpen, tokens[5].Kind);
            Assert.Equal(TokKind.BracketClose, tokens[6].Kind);
            Assert.Equal(TokKind.ParenClose, tokens[7].Kind);
            Assert.Equal(TokKind.Eof, tokens[8].Kind);

            // Zero Width Space at the beginning of the formula
            value = string.Format(CultureInfo.InvariantCulture, "{0}", '\u200B') + "ClearCollect(Temp1,[])";
            tokens = TexlLexer.InvariantLexer.LexSource(value, TexlLexer.Flags.None);
            Assert.NotNull(tokens);
            Assert.Equal(9, tokens.Count);
            Assert.Equal(TokKind.Error, tokens[0].Kind);
            Assert.Equal(2, (tokens[0] as ErrorToken).ResourceKeyFormatStringArgs.Length);
            Assert.Equal((tokens[0] as ErrorToken).DetailErrorKey.Value, TexlStrings.UnexpectedCharacterToken);
        }

        [Fact]
        public void TestLexDottedNames()
        {
            var tokens = TexlLexer.InvariantLexer.LexSource("A!B");
            Assert.NotNull(tokens);
            Assert.Equal(4, tokens.Count);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.Bang, tokens[1].Kind);
            Assert.Equal(TokKind.Ident, tokens[2].Kind);
            Assert.Equal(TokKind.Eof, tokens[3].Kind);
            Assert.True(tokens[1].IsDottedNamePunctuator);

            tokens = TexlLexer.InvariantLexer.LexSource("A.B.C");
            Assert.NotNull(tokens);
            Assert.Equal(6, tokens.Count);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.Dot, tokens[1].Kind);
            Assert.Equal(TokKind.Ident, tokens[2].Kind);
            Assert.Equal(TokKind.Dot, tokens[3].Kind);
            Assert.Equal(TokKind.Ident, tokens[4].Kind);
            Assert.Equal(TokKind.Eof, tokens[5].Kind);
            Assert.True(tokens[1].IsDottedNamePunctuator);
            Assert.True(tokens[3].IsDottedNamePunctuator);

            tokens = TexlLexer.InvariantLexer.LexSource("A[B]");
            Assert.NotNull(tokens);
            Assert.Equal(5, tokens.Count);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.BracketOpen, tokens[1].Kind);
            Assert.Equal(TokKind.Ident, tokens[2].Kind);
            Assert.Equal(TokKind.BracketClose, tokens[3].Kind);
            Assert.Equal(TokKind.Eof, tokens[4].Kind);
            Assert.True(tokens[1].IsDottedNamePunctuator);
        }

        [Fact]
        public void TestLexNumbersWithLanguageSettings()
        {
            var tokens = TexlLexer.GetLocalizedInstance(GetFrenchSettings()).LexSource("123456,78");
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Count);
            Assert.Equal(TokKind.NumLit, tokens[0].Kind);
            Assert.Equal(123456.78, tokens[0].As<NumLitToken>().Value);
        }

        [Fact]
        public void TestLexListsWithLanguageSettings()
        {
            var tokens = TexlLexer.GetLocalizedInstance(GetFrenchSettings()).LexSource("[1,2;2,3;4]");
            Assert.NotNull(tokens);
            Assert.Equal(8, tokens.Count);
            Assert.Equal(TokKind.BracketOpen, tokens[0].Kind);
            Assert.Equal(TokKind.NumLit, tokens[1].Kind);
            Assert.Equal(1.2, tokens[1].As<NumLitToken>().Value);
            Assert.Equal(TokKind.Comma, tokens[2].Kind);
            Assert.Equal(TokKind.NumLit, tokens[3].Kind);
            Assert.Equal(2.3, tokens[3].As<NumLitToken>().Value);
            Assert.Equal(TokKind.Comma, tokens[4].Kind);
            Assert.Equal(TokKind.NumLit, tokens[5].Kind);
            Assert.Equal(4, tokens[5].As<NumLitToken>().Value);
            Assert.Equal(TokKind.BracketClose, tokens[6].Kind);
            Assert.Equal(TokKind.Eof, tokens[7].Kind);
        }

        [Fact]
        public void TestLexSemicolonListsWithLanguageSettings()
        {
            var tokens = TexlLexer.GetLocalizedInstance(GetFrenchSettings()).LexSource("A ;; B ;; C");
            Assert.NotNull(tokens);
            Assert.Equal(10, tokens.Count);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal("A", tokens[0].As<IdentToken>().Name.Value);
            Assert.Equal(TokKind.Whitespace, tokens[1].Kind);
            Assert.Equal(TokKind.Semicolon, tokens[2].Kind);
            Assert.Equal(TokKind.Whitespace, tokens[3].Kind);
            Assert.Equal(TokKind.Ident, tokens[4].Kind);
            Assert.Equal("B", tokens[4].As<IdentToken>().Name.Value);
            Assert.Equal(TokKind.Whitespace, tokens[5].Kind);
            Assert.Equal(TokKind.Semicolon, tokens[6].Kind);
            Assert.Equal(TokKind.Whitespace, tokens[7].Kind);
            Assert.Equal(TokKind.Ident, tokens[8].Kind);
            Assert.Equal("C", tokens[8].As<IdentToken>().Name.Value);
            Assert.Equal(TokKind.Eof, tokens[9].Kind);
        }

        [Theory]
        [InlineData("", 0, 0, "")]
        [InlineData("abc", 0, 0, "")]
        [InlineData("abc", 0, 1, "a")]
        [InlineData("abc", 1, 2, "b")]
        [InlineData("abc", 2, 3, "c")]
        [InlineData("abc", 0, 3, "abc")]
        public void TestLexGetFragment(string script, int min, int lim, string fragment)
        {
            var span = new Span(min, lim);
            Assert.Equal(fragment, span.GetFragment(script));
        }

        private static CultureInfo GetFrenchSettings()
        {
            return new CultureInfo("fr-FR");
        }

        /// <summary>
        /// Checks that the keyword arrays are properly constructed during the <see cref="TexlLexer"/> initialization.
        /// </summary>
        [Fact]
        public void Test_LexerTest_KeywordArrays()
        {
            // GetUnaryOperatorKeywords

            var unaryOperatorKeywords = TexlLexer.GetUnaryOperatorKeywords();

            var expectedUOKeywordLength = 1;
            Assert.Contains(TexlLexer.KeywordNot, unaryOperatorKeywords);
            expectedUOKeywordLength += 1;

            Assert.True(unaryOperatorKeywords?.Count == expectedUOKeywordLength);

            // GetBinaryOperatorKeywords

            var binaryOperatorKeywords = TexlLexer.GetBinaryOperatorKeywords();
            var expectedBOKeywordLength = 17;

            expectedBOKeywordLength += 2;
            Assert.Contains(TexlLexer.KeywordAnd, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordOr, binaryOperatorKeywords);

            Assert.True(binaryOperatorKeywords?.Count == expectedBOKeywordLength);

            Assert.Contains(TexlLexer.PunctuatorAmpersand, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorAnd, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorOr, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorAdd, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorSub, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorMul, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorDiv, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorEqual, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorLess, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorLessOrEqual, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorGreater, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorGreaterOrEqual, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorNotEqual, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorCaret, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordIn, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordExactin, binaryOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordAs, binaryOperatorKeywords);

            // GetOperatorKeywords
            // Primitive type.
            var primitiveOperatorKeywords = TexlLexer.GetOperatorKeywords(new DType(DKind.Boolean));

            var expectedPOKeywordLength = 17;

            expectedPOKeywordLength += 2;
            Assert.Contains(TexlLexer.KeywordAnd, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordOr, primitiveOperatorKeywords);

            Assert.True(primitiveOperatorKeywords?.Count == expectedPOKeywordLength);

            Assert.Contains(TexlLexer.PunctuatorAmpersand, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorEqual, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorNotEqual, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorAdd, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorSub, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorMul, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorDiv, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorCaret, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorAnd, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorOr, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorLess, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorLessOrEqual, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorGreater, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.PunctuatorGreaterOrEqual, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordIn, primitiveOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordExactin, primitiveOperatorKeywords);

            // Aggregate/Control type.
            var aggregateOperatorKeywords = TexlLexer.GetOperatorKeywords(new DType(DKind.Table));
            Assert.True(aggregateOperatorKeywords?.Count == 3);
            Assert.Contains(TexlLexer.KeywordIn, aggregateOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordExactin, aggregateOperatorKeywords);
            Assert.Contains(TexlLexer.KeywordAs, aggregateOperatorKeywords);

            // Not a primitive nor an aggregate type.
            var errorOperatorKeywords = TexlLexer.GetOperatorKeywords(new DType(DKind.Error));
            Assert.True(errorOperatorKeywords?.Count == 0);

            // GetConstantKeywords

            var constantKeywords = TexlLexer.GetConstantKeywords(getParent: false);
            Assert.True(constantKeywords?.Count == 3);
            Assert.Contains(TexlLexer.KeywordFalse, constantKeywords);
            Assert.Contains(TexlLexer.KeywordTrue, constantKeywords);
            Assert.Contains(TexlLexer.KeywordSelf, constantKeywords);

            constantKeywords = TexlLexer.GetConstantKeywords(getParent: true);
            Assert.True(constantKeywords?.Count == 4);
            Assert.Contains(TexlLexer.KeywordFalse, constantKeywords);
            Assert.Contains(TexlLexer.KeywordTrue, constantKeywords);
            Assert.Contains(TexlLexer.KeywordParent, constantKeywords);
            Assert.Contains(TexlLexer.KeywordSelf, constantKeywords);

            // GetPunctuatorsAndInvariants

            var punctuatorsAndInvariants = TexlLexer.InvariantLexer.GetPunctuatorsAndInvariants();
            Assert.True(punctuatorsAndInvariants?.Count == 3);
            Assert.True(punctuatorsAndInvariants.ContainsKey(TexlLexer.InvariantLexer.LocalizedPunctuatorDecimalSeparator));
            Assert.True(punctuatorsAndInvariants.ContainsKey(TexlLexer.InvariantLexer.LocalizedPunctuatorListSeparator));
            Assert.True(punctuatorsAndInvariants.ContainsKey(TexlLexer.InvariantLexer.LocalizedPunctuatorChainingSeparator));
        }

        [Fact]
        public void TestUnsupportedDecimalSeparatorCausesFallback()
        {
            // Simulate an override of the decimal separator to something that AXL does not support.
            var oldCulture = CultureInfo.CurrentCulture;
            var newCulture = new CultureInfo(CultureInfo.CurrentCulture.Name);
            newCulture.NumberFormat.NumberDecimalSeparator = "+";
            CultureInfo.CurrentCulture = newCulture;

            // The lexer should fall back to the invariant separator.
            var lexer = TexlLexer.GetLocalizedInstance(null);
            Assert.Equal(lexer.LocalizedPunctuatorDecimalSeparator, TexlLexer.PunctuatorDecimalSeparatorInvariant);

            var tokens = lexer.LexSource("123456.78");
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Count);
            Assert.Equal(TokKind.NumLit, tokens[0].Kind);
            Assert.Equal(123456.78, tokens[0].As<NumLitToken>().Value);

            CultureInfo.CurrentCulture = oldCulture;
        }

        [Fact]
        public void TestBasicStringInterpolation()
        {
            AssertTokens(
                "$\"Hello {name}\"",
                TokKind.StrInterpStart,
                TokKind.StrLit,
                TokKind.IslandStart,
                TokKind.Ident,
                TokKind.IslandEnd,
                TokKind.StrInterpEnd,
                TokKind.Eof);
        }

        [Fact]
        public void TestImbalancedBrackets()
        {
            AssertTokens(
                "}}}",
                TokKind.CurlyClose,
                TokKind.CurlyClose,
                TokKind.CurlyClose,
                TokKind.Eof);
        }

        [Fact]
        public void TestEscapeStringInterpolation()
        {
            AssertTokens(
                "$\"Hello {{name}} {name}\"",
                TokKind.StrInterpStart,
                TokKind.StrLit,
                TokKind.IslandStart,
                TokKind.Ident,
                TokKind.IslandEnd,
                TokKind.StrInterpEnd,
                TokKind.Eof);
        }

        [Fact]
        public void TestStringInterpolationWithTable()
        {
            AssertTokens(
                "$\"Hello {Table({a: 5})} World!\"",
                TokKind.StrInterpStart,
                TokKind.StrLit,
                TokKind.IslandStart,
                TokKind.Ident,
                TokKind.ParenOpen,
                TokKind.CurlyOpen,
                TokKind.Ident,
                TokKind.Colon,
                TokKind.Whitespace,
                TokKind.NumLit,
                TokKind.CurlyClose,
                TokKind.ParenClose,
                TokKind.IslandEnd,
                TokKind.StrLit,
                TokKind.StrInterpEnd,
                TokKind.Eof);
        }

        [Fact]
        public void TestNestedStringInterpolation()
        {
            AssertTokens(
                "$\"One {$\"Two {\"Three\"}\"} Four\"",
                TokKind.StrInterpStart,
                TokKind.StrLit,
                TokKind.IslandStart,
                TokKind.StrInterpStart,
                TokKind.StrLit,
                TokKind.IslandStart,
                TokKind.StrLit,
                TokKind.IslandEnd,
                TokKind.StrInterpEnd,
                TokKind.IslandEnd,
                TokKind.StrLit,
                TokKind.StrInterpEnd,
                TokKind.Eof);
        }

        [Fact]
        public void LexerIdentityTest()
        {
            Assert.True(object.ReferenceEquals(TexlLexer.InvariantLexer, TexlLexer.InvariantLexer));
            Assert.True(object.ReferenceEquals(TexlLexer.CommaDecimalSeparatorLexer, TexlLexer.CommaDecimalSeparatorLexer));
            Assert.False(object.ReferenceEquals(TexlLexer.InvariantLexer, TexlLexer.CommaDecimalSeparatorLexer));
        }
    }
}
