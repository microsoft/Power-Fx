// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public sealed class LexerTest
    {
        [Fact]
        public void TestTryNameOrIdentifierToName()
        {
            DName name;

            Assert.True(TexlLexer.TryNameOrIdentifierToName(" Name   ", out name));
            Assert.Equal("Name", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName(" Name   Abcd ", out name));
            Assert.Equal("Name   Abcd", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName(" Name   Abcd", out name));
            Assert.Equal("Name   Abcd", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("Name   Abcd", out name));
            Assert.Equal("Name   Abcd", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("Name\bAbcd", out name));
            Assert.Equal("Name\bAbcd", name);

            Assert.False(TexlLexer.TryNameOrIdentifierToName(string.Empty, out name));
            Assert.False(TexlLexer.TryNameOrIdentifierToName("   ", out name));

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   'Escaped Name'", out name));
            Assert.Equal("Escaped Name", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   'Escaped '' Name'", out name));
            Assert.Equal("Escaped ' Name", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   'Escaped '' Name'''", out name));
            Assert.Equal("Escaped ' Name'", name);

            Assert.False(TexlLexer.TryNameOrIdentifierToName("   '   ' ", out name));

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   ''''", out name));
            Assert.Equal("'", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   'Escaped Name'   ", out name));
            Assert.Equal("Escaped Name", name);

            Assert.False(TexlLexer.TryNameOrIdentifierToName("   'Escaped Name   ", out name));
            Assert.False(TexlLexer.TryNameOrIdentifierToName("   'Escaped Name  ' abc ", out name));

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   '#a!>>?'", out name));
            Assert.Equal("#a!>>?", name);

            Assert.True(TexlLexer.TryNameOrIdentifierToName("   '123\u00ae'", out name));
            Assert.Equal("123\u00ae", name);
        }

        internal static bool IsInRangeInclusive(char ch, Tuple<char, char>[] range)
        {
            for (int i = 0; i < range.Length; i++)
            {
                if (range[i].Item1 <= ch && ch <= range[i].Item2)
                    return true;
            }
            return false;
        }

        [Fact]
        public void TestIsIdentStart()
        {
            Tuple<char, char>[] identStartRanges = new Tuple<char, char>[] {
                Tuple.Create('A', 'Z'),
                Tuple.Create('a', 'z'),
                Tuple.Create('_', '_'),
                Tuple.Create('\'', '\''),
            };

            for (int i = 0; i < 128; i++)
            {
                char ch = (char)i;
                Assert.Equal(LexerTest.IsInRangeInclusive(ch, identStartRanges), TexlLexer.IsIdentStart(ch));
            }
        }

        [Fact]
        public void TestContextDependableTokens()
        {
            Token[] tokens;
            string value;

            // Disable support.
            tokens = TexlLexer.LocalizedInstance.LexSource("%ID%", TexlLexer.Flags.None);
            Assert.NotNull(tokens);
            Assert.Equal(4, tokens.Length);
            Assert.Equal(TokKind.PercentSign, tokens[0].Kind);

            // Enable support. Also checks cache miss after the previous call.
            value = "%A%";
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.ReplaceableLit, tokens[0].Kind);
            Assert.Equal(TokKind.Eof, tokens[1].Kind);
            Assert.Equal(value, tokens[0].As<ReplaceableToken>().Value);

            // Single token.
            tokens = TexlLexer.LocalizedInstance.LexSource("%", TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.PercentSign, tokens[0].Kind);
            Assert.Equal(TokKind.Eof, tokens[1].Kind);

            // No actual data.
            tokens = TexlLexer.LocalizedInstance.LexSource("%%", TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(3, tokens.Length);
            Assert.Equal(TokKind.PercentSign, tokens[0].Kind);
            Assert.Equal(TokKind.PercentSign, tokens[1].Kind);
            Assert.Equal(TokKind.Eof, tokens[2].Kind);

            // Mixed content.
            value = "%data_##data##_data%";
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.ReplaceableLit, tokens[0].Kind);
            Assert.Equal(TokKind.Eof, tokens[1].Kind);
            Assert.Equal(value, tokens[0].As<ReplaceableToken>().Value);

            // No end terminator.
            value = "data%data";
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(4, tokens.Length);
            Assert.Equal(TokKind.PercentSign, tokens[1].Kind);

            // Chained expressions.
            value = "%data%";
            tokens = TexlLexer.LocalizedInstance.LexSource(value + value, TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(3, tokens.Length);
            Assert.Equal(TokKind.ReplaceableLit, tokens[0].Kind);
            Assert.Equal(TokKind.ReplaceableLit, tokens[1].Kind);
            Assert.Equal(TokKind.Eof, tokens[2].Kind);
            Assert.Equal(value, tokens[0].As<ReplaceableToken>().Value);
            Assert.Equal(value, tokens[1].As<ReplaceableToken>().Value);

            // In the middle of the text.
            value = "%data%";
            tokens = TexlLexer.LocalizedInstance.LexSource("text " + value + " text", TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(6, tokens.Length);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.Whitespace, tokens[1].Kind);
            Assert.Equal(TokKind.ReplaceableLit, tokens[2].Kind);
            Assert.Equal(TokKind.Whitespace, tokens[3].Kind);
            Assert.Equal(TokKind.Ident, tokens[4].Kind);
            Assert.Equal(TokKind.Eof, tokens[5].Kind);
            Assert.Equal(value, tokens[2].As<ReplaceableToken>().Value);
        }

        [Fact]
        public void TestZeroWidthSpaceCharacters()
        {
            Token[] tokens;
            string value;

            // No Zero Width Space after comma
            value = "ClearCollect(Temp1,[])";
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.None);
            Assert.NotNull(tokens);
            Assert.Equal(8, tokens.Length);
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
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.None);
            Assert.NotNull(tokens);
            Assert.Equal(9, tokens.Length);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.ParenOpen, tokens[1].Kind);
            Assert.Equal(TokKind.Ident, tokens[2].Kind);
            Assert.Equal(TokKind.Comma, tokens[3].Kind);
            Assert.Equal(TokKind.Error, tokens[4].Kind);
            Assert.Equal((tokens[4] as ErrorToken).ResourceKeyFormatStringArgs.Length, 2);
            Assert.Equal((tokens[4] as ErrorToken).DetailErrorKey.Value, TexlStrings.UnexpectedCharacterToken);
            Assert.Equal(TokKind.BracketOpen, tokens[5].Kind);
            Assert.Equal(TokKind.BracketClose, tokens[6].Kind);
            Assert.Equal(TokKind.ParenClose, tokens[7].Kind);
            Assert.Equal(TokKind.Eof, tokens[8].Kind);

            // Zero Width Space at the beginning of the formula
            value = string.Format("{0}", '\u200B') + "ClearCollect(Temp1,[])";
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.None);
            Assert.NotNull(tokens);
            Assert.Equal(9, tokens.Length);
            Assert.Equal(TokKind.Error, tokens[0].Kind);
            Assert.Equal((tokens[0] as ErrorToken).ResourceKeyFormatStringArgs.Length, 2);
            Assert.Equal((tokens[0] as ErrorToken).DetailErrorKey.Value, TexlStrings.UnexpectedCharacterToken);
        }

        [Fact]
        public void TestLocalizableTokens()
        {
            Token[] tokens;
            string value;

            // Disable support.
            tokens = TexlLexer.LocalizedInstance.LexSource("##ID##", TexlLexer.Flags.None);
            Assert.NotNull(tokens);
            Assert.Equal(6, tokens.Length);
            Assert.Equal(TokKind.Error, tokens[0].Kind);

            // Enable support.
            value = "##A##";
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.ReplaceableLit, tokens[0].Kind);
            Assert.Equal(TokKind.Eof, tokens[1].Kind);
            Assert.Equal(value, tokens[0].As<ReplaceableToken>().Value);

            // Invalid sequence.
            tokens = TexlLexer.LocalizedInstance.LexSource("#ID#", TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(4, tokens.Length);
            Assert.Equal(TokKind.Error, tokens[0].Kind);

            // Single token.
            tokens = TexlLexer.LocalizedInstance.LexSource("#", TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.Error, tokens[0].Kind);
            Assert.Equal(TokKind.Eof, tokens[1].Kind);

            // Single open token.
            tokens = TexlLexer.LocalizedInstance.LexSource("##", TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(3, tokens.Length);
            Assert.Equal(TokKind.Error, tokens[0].Kind);
            Assert.Equal(TokKind.Eof, tokens[2].Kind);

            // No actual data.
            tokens = TexlLexer.LocalizedInstance.LexSource("####", TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(5, tokens.Length);
            Assert.Equal(TokKind.Error, tokens[0].Kind);
            Assert.Equal(TokKind.Eof, tokens[4].Kind);

            // Mixed content.
            value = "##data_%data%_data##";
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.ReplaceableLit, tokens[0].Kind);
            Assert.Equal(TokKind.Eof, tokens[1].Kind);
            Assert.Equal(value, tokens[0].As<ReplaceableToken>().Value);

            // No end terminator.
            value = "data##data";
            tokens = TexlLexer.LocalizedInstance.LexSource(value, TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(5, tokens.Length);
            Assert.Equal(TokKind.Error, tokens[2].Kind);

            // Chained expressions.
            value = "##data##";
            tokens = TexlLexer.LocalizedInstance.LexSource(value + value, TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(3, tokens.Length);
            Assert.Equal(TokKind.ReplaceableLit, tokens[0].Kind);
            Assert.Equal(TokKind.ReplaceableLit, tokens[1].Kind);
            Assert.Equal(TokKind.Eof, tokens[2].Kind);
            Assert.Equal(value, tokens[0].As<ReplaceableToken>().Value);
            Assert.Equal(value, tokens[1].As<ReplaceableToken>().Value);

            // In the middle of the text.
            value = "##data##";
            tokens = TexlLexer.LocalizedInstance.LexSource("text " + value + " text", TexlLexer.Flags.AllowReplaceableTokens);
            Assert.NotNull(tokens);
            Assert.Equal(6, tokens.Length);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.Whitespace, tokens[1].Kind);
            Assert.Equal(TokKind.ReplaceableLit, tokens[2].Kind);
            Assert.Equal(TokKind.Whitespace, tokens[3].Kind);
            Assert.Equal(TokKind.Ident, tokens[4].Kind);
            Assert.Equal(TokKind.Eof, tokens[5].Kind);
            Assert.Equal(value, tokens[2].As<ReplaceableToken>().Value);
        }

        [Fact]
        public void TestLexDottedNames()
        {
            Token[] tokens;

            tokens = TexlLexer.LocalizedInstance.LexSource("A!B");
            Assert.NotNull(tokens);
            Assert.Equal(4, tokens.Length);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.Bang, tokens[1].Kind);
            Assert.Equal(TokKind.Ident, tokens[2].Kind);
            Assert.Equal(TokKind.Eof, tokens[3].Kind);
            Assert.True(tokens[1].IsDottedNamePunctuator);

            tokens = TexlLexer.LocalizedInstance.LexSource("A.B.C");
            Assert.NotNull(tokens);
            Assert.Equal(6, tokens.Length);
            Assert.Equal(TokKind.Ident, tokens[0].Kind);
            Assert.Equal(TokKind.Dot, tokens[1].Kind);
            Assert.Equal(TokKind.Ident, tokens[2].Kind);
            Assert.Equal(TokKind.Dot, tokens[3].Kind);
            Assert.Equal(TokKind.Ident, tokens[4].Kind);
            Assert.Equal(TokKind.Eof, tokens[5].Kind);
            Assert.True(tokens[1].IsDottedNamePunctuator);
            Assert.True(tokens[3].IsDottedNamePunctuator);

            tokens = TexlLexer.LocalizedInstance.LexSource("A[B]");
            Assert.NotNull(tokens);
            Assert.Equal(5, tokens.Length);
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
            Token[] tokens;

            tokens = TexlLexer.NewInstance(SomeFrenchLikeSettings()).LexSource("123456,78");
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.NumLit, tokens[0].Kind);
            Assert.Equal(123456.78, tokens[0].As<NumLitToken>().Value);

            tokens = TexlLexer.NewInstance(SomeRomanianLikeSettings()).LexSource("10`12345");
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.NumLit, tokens[0].Kind);
            Assert.Equal(10.12345, tokens[0].As<NumLitToken>().Value);
        }

        [Fact]
        public void TestLexListsWithLanguageSettings()
        {
            Token[] tokens;

            tokens = TexlLexer.NewInstance(SomeFrenchLikeSettings()).LexSource("[1,2;2,3;4]");
            Assert.NotNull(tokens);
            Assert.Equal(8, tokens.Length);
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
            Token[] tokens;

            tokens = TexlLexer.NewInstance(SomeFrenchLikeSettings()).LexSource("A ;; B ;; C");
            Assert.NotNull(tokens);
            Assert.Equal(10, tokens.Length);
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
            Span span = new Span(min, lim);
            Assert.Equal(fragment, span.GetFragment(script));
        }

        private ILanguageSettings SomeFrenchLikeSettings()
        {
            var loc = new LanguageSettings("fr-FR", "fr-FR");
            loc.AddPunctuator(",", ".");
            loc.AddPunctuator(";", ",");
            loc.AddPunctuator(";;", ";");
            return loc;
        }

        private ILanguageSettings SomeRomanianLikeSettings()
        {
            var loc = new LanguageSettings("ro-RO", "ro-RO");
            loc.AddPunctuator("`", ".");
            loc.AddPunctuator(",", ",");
            loc.AddPunctuator(";", ";");
            return loc;
        }

        /// <summary>
        /// Checks that the keyword arrays are properly constructed during the <see cref="TexlLexer"/> initialization.
        /// </summary>
        [Fact]
        public void Test_LexerTest_KeywordArrays()
        {
            // GetUnaryOperatorKeywords

            string[] unaryOperatorKeywords = TexlLexer.LocalizedInstance.GetUnaryOperatorKeywords();

            int expectedUOKeywordLength = 1;
            Assert.True(unaryOperatorKeywords.Contains(TexlLexer.KeywordNot));
            expectedUOKeywordLength += 1;

            Assert.True(unaryOperatorKeywords?.Length == expectedUOKeywordLength);

            // GetBinaryOperatorKeywords

            string[] binaryOperatorKeywords = TexlLexer.LocalizedInstance.GetBinaryOperatorKeywords();
            int expectedBOKeywordLength = 17;

            expectedBOKeywordLength += 2;
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.KeywordAnd));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.KeywordOr));

            Assert.True(binaryOperatorKeywords?.Length == expectedBOKeywordLength);

            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorAmpersand));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorAnd));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorOr));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorAdd));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorSub));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorMul));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorDiv));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorEqual));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorLess));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorLessOrEqual));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorGreater));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorGreaterOrEqual));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorNotEqual));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.PunctuatorCaret));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.KeywordIn));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.KeywordExactin));
            Assert.True(binaryOperatorKeywords.Contains(TexlLexer.KeywordAs));
            // GetOperatorKeywords

            // Primitive type.
            string[] primitiveOperatorKeywords = TexlLexer.LocalizedInstance.GetOperatorKeywords(new DType(DKind.Boolean));

            int expectedPOKeywordLength = 17;

            expectedPOKeywordLength += 2;
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.KeywordAnd));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.KeywordOr));

            Assert.True(primitiveOperatorKeywords?.Length == expectedPOKeywordLength);

            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorAmpersand));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorEqual));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorNotEqual));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorAdd));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorSub));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorMul));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorDiv));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorCaret));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorAnd));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorOr));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorLess));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorLessOrEqual));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorGreater));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.PunctuatorGreaterOrEqual));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.KeywordIn));
            Assert.True(primitiveOperatorKeywords.Contains(TexlLexer.KeywordExactin));

            // Aggregate/Control type.
            string[] aggregateOperatorKeywords = TexlLexer.LocalizedInstance.GetOperatorKeywords(new DType(DKind.Table));
            Assert.True(aggregateOperatorKeywords?.Length == 3);
            Assert.True(aggregateOperatorKeywords.Contains(TexlLexer.KeywordIn));
            Assert.True(aggregateOperatorKeywords.Contains(TexlLexer.KeywordExactin));
            Assert.True(aggregateOperatorKeywords.Contains(TexlLexer.KeywordAs));

            // Not a primitive nor an aggregate type.
            string[] errorOperatorKeywords = TexlLexer.LocalizedInstance.GetOperatorKeywords(new DType(DKind.Error));
            Assert.True(errorOperatorKeywords?.Length == 0);

            // GetConstantKeywords

            string[] constantKeywords = TexlLexer.LocalizedInstance.GetConstantKeywords(getParent: false);
            Assert.True(constantKeywords?.Length == 3);
            Assert.True(constantKeywords.Contains(TexlLexer.KeywordFalse));
            Assert.True(constantKeywords.Contains(TexlLexer.KeywordTrue));
            Assert.True(constantKeywords.Contains(TexlLexer.KeywordSelf));

            constantKeywords = TexlLexer.LocalizedInstance.GetConstantKeywords(getParent: true);
            Assert.True(constantKeywords?.Length == 4);
            Assert.True(constantKeywords.Contains(TexlLexer.KeywordFalse));
            Assert.True(constantKeywords.Contains(TexlLexer.KeywordTrue));
            Assert.True(constantKeywords.Contains(TexlLexer.KeywordParent));
            Assert.True(constantKeywords.Contains(TexlLexer.KeywordSelf));

            // GetPunctuatorsAndInvariants

            IDictionary<string, string> punctuatorsAndInvariants = TexlLexer.LocalizedInstance.GetPunctuatorsAndInvariants();
            Assert.True(punctuatorsAndInvariants?.Count == 3);
            Assert.True(punctuatorsAndInvariants.ContainsKey(TexlLexer.LocalizedInstance.LocalizedPunctuatorDecimalSeparator));
            Assert.True(punctuatorsAndInvariants.ContainsKey(TexlLexer.LocalizedInstance.LocalizedPunctuatorListSeparator));
            Assert.True(punctuatorsAndInvariants.ContainsKey(TexlLexer.LocalizedInstance.LocalizedPunctuatorChainingSeparator));
        }

        [Fact]
        public void TestUnsupportedDecimalSeparatorCausesFallback()
        {
            Token[] tokens;

            // Simulate an override of the decimal separator to something that AXL does not support.
            var oldCulture = CultureInfo.CurrentCulture;
            var newCulture = new CultureInfo(CultureInfo.CurrentCulture.Name);
            newCulture.NumberFormat.NumberDecimalSeparator = "+";
            CultureInfo.CurrentCulture = newCulture;

            // The lexer should fall back to the invariant separator.
            TexlLexer lexer = TexlLexer.NewInstance(null);
            Assert.Equal(lexer.LocalizedPunctuatorDecimalSeparator, TexlLexer.PunctuatorDecimalSeparatorInvariant);

            tokens = lexer.LexSource("123456.78");
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokKind.NumLit, tokens[0].Kind);
            Assert.Equal(123456.78, tokens[0].As<NumLitToken>().Value);

            CultureInfo.CurrentCulture = oldCulture;
        }
    }
}
