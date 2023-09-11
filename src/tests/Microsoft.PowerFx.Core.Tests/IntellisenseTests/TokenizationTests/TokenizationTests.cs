// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    public class TokenizationTests : PowerFxTest
    {
        [Theory]
        [InlineData("gallery1", 1, 0, "gallery1", 0, 8, TokenType.Control, false)]
        [InlineData("gallery1.Selected.nestedLabel1", 3, 0, "gallery1", 0, 8, TokenType.Control, false)]
        [InlineData("gallery1.Selected.nestedLabel1", 3, 2, "Selected", 9, 17, TokenType.DottedNamePart, false)]
        [InlineData("gallery1.Selected.label2", 3, 0, "gallery1", 0, 8, TokenType.Control, false)]
        [InlineData("label2.Text", 2, 0, "label2", 0, 6, TokenType.Control, false)]
        [InlineData("label2.Text & \" Hello\"", 4, 2, TokenizerConstants.BinaryOp, 12, 13, TokenType.BinaryOp, false)]
        [InlineData("Text(label2.Text)", 3, 0, "label2", 5, 11, TokenType.Control, false)]
        [InlineData("Text(true)", 2, 0, "Text", 0, 4, TokenType.Function, false)]
        [InlineData("Text(true)", 2, 1, TokenizerConstants.BooleanLiteral, 5, 9, TokenType.BoolLit, false)]
        [InlineData("Text(123)", 2, 1, TokenizerConstants.NumericLiteral, 5, 8, TokenType.NumLit, false)]
        [InlineData("Color.Green", 2, 1, "Green", 6, 11, TokenType.DottedNamePart, false)]
        [InlineData("Color.Blue", 2, 0, "Color", 0, 5, TokenType.Enum, false)]
        [InlineData("First(foo).test", 3, 0, "foo", 6, 9, TokenType.Data, false)]
        [InlineData("name", 1, 0, "name", 0, 4, TokenType.Alias, false)]
        [InlineData("ThisItem.Income", 2, 0, "ThisItem", 0, 8, TokenType.ThisItem, false)]
        [InlineData("ThisItem.Name", 2, 0, "ThisItem", 0, 8, TokenType.ThisItem, false)]
        [InlineData("!true", 1, 0, TokenizerConstants.BooleanLiteral, 1, 5, TokenType.BoolLit, false)]
        internal void TestBasicTokenizationWithDefaultComparer(string inputText, int expectedTokenCount, int targetTokenIndex, string targetTokenName, int targetTokenStart, int targetTokenEnd, TokenType targetTokenType, bool targetTokenHidden)
        {
            // Arrange
            var checkResult = GetDefaultCheckResult(inputText);

            // Act
            var tokens = Tokenization.Tokenize(inputText, checkResult.Binding, checkResult.Parse.Comments);

            // Assert
            Assert.NotNull(tokens);
            var targetToken = tokens.ElementAt(targetTokenIndex);
            Assert.Equal(expectedTokenCount, tokens.Count());
            Assert.Equal(targetTokenName, targetToken.TokenName);
            Assert.Equal(targetTokenStart, targetToken.StartIndex);
            Assert.Equal(targetTokenEnd, targetToken.EndIndex);
            Assert.Equal(targetTokenType, targetToken.TokenType);
            Assert.Equal(targetTokenHidden, targetToken.CanBeHidden);
            AssertOrderOfTokens(tokens, new TokenTextSpanComparer());
        }

        [Theory]
        [InlineData("1+-2;true;\"String Literal\";Color.Blue;Max(1,2,3);gallery1.Selected.nestedLabel1;label2.Text;$\"1 + 2 = {3}\";// This is Comment")]
        public void TestTokensAreOrderedCorrectly(string expression)
        {
            // Arrange
            var checkResult = GetDefaultCheckResult(expression);
            var comparer = new TokenTextSpanComparer();

            // Act
            var tokens = Tokenization.Tokenize(expression, checkResult.Binding, checkResult.Parse.Comments, comparer);

            // Assert
            AssertOrderOfTokens(tokens, comparer);
        }

        [Theory]
        [InlineData("$\"1 + 2 = {3}\"")]
        [InlineData("$\"This is interpolated string {2\r\n\r\n /* Comment */}\"")]
        internal void TestCompilerGeneratedNodesDoNotShowUpAsTokens(string expression)
        {
            // Arrange
            var checkResult = GetDefaultCheckResult(expression);
            var stringInterpNodes = checkResult.Binding.GetStringInterpolations();
            var compilerGenCallNodes = stringInterpNodes.Where(node => checkResult.Binding.GetCompilerGeneratedCallNode(node) != null).Select(node => checkResult.Binding.GetCompilerGeneratedCallNode(node)).ToList();
            var compilerGenNodesNames = compilerGenCallNodes.Select(node => node.Head.Name.Value);

            // Act
            var tokens = Tokenization.Tokenize(expression, checkResult.Binding, checkResult.Parse.Comments);

            // Assert
            Assert.NotNull(tokens);
            Assert.All(compilerGenCallNodes, (node) =>
            {
                var spans = new List<Span> { node.GetTextSpan(), node.GetCompleteSpan(), node.GetSourceBasedSpan() };
                foreach (var token in tokens)
                {
                    if (token.TokenType == TokenType.Function)
                    {
                        foreach (var span in spans)
                        {
                            Assert.NotEqual(span.Min, token.StartIndex);
                            Assert.NotEqual(span.Lim, token.EndIndex);
                        }
                    }
                }
            });
            Assert.All(tokens, (tok) => Assert.DoesNotContain(compilerGenNodesNames, (name) => name == tok.TokenName));
            tokens = tokens.OrderBy(tok => tok.StartIndex).ToList();
            Assert.NotNull(tokens.FirstOrDefault());
            Assert.Equal(TokenType.StrInterpStart, tokens.FirstOrDefault().TokenType);
        }

        [Theory]
        [MemberData(nameof(StringInterpolationTestCases.StringInterpTestCasesAsObjects), MemberType = typeof(StringInterpolationTestCases))]
        internal void TestStringInterpolationTokens(TokenizationTestCase testCase)
        {
            // Arrange
            var expression = testCase.Expression;
            var checkResult = GetDefaultCheckResult(expression, assertNoErrors: false);

            // Act
            var tokens = Tokenization.Tokenize(expression, checkResult.Binding, checkResult.Parse.Comments, null);

            // Assert
            AssertTokens(testCase.ExpectedTokens, tokens);
        }

        [Theory]
        [MemberData(nameof(NumDecLitTokenTestCases.NumDecLiteralTestCasesAsObjects), MemberType = typeof(NumDecLitTokenTestCases))]
        internal void TestNumDecLitTokens(TokenizationTestCase testCase)
        {
            // Arrange
            var expression = testCase.Expression;
            var checkResult = GetDefaultCheckResult(expression, testCase.Options);

            // Act
            var tokens = Tokenization.Tokenize(expression, checkResult.Binding, checkResult.Parse.Comments, null);

            // Assert
            AssertTokens(testCase.ExpectedTokens, tokens);
        }

        private static void AssertTokens(IEnumerable<ITokenTextSpan> expectedTokens, IEnumerable<ITokenTextSpan> actualTokens)
        {
            expectedTokens = expectedTokens.OrderBy(tok => tok.StartIndex);
            actualTokens = actualTokens.OrderBy(tok => tok.StartIndex);
            Assert.Equal(expectedTokens.Count(), actualTokens.Count());
            foreach (var (expectedToken, actualToken) in expectedTokens.Zip(actualTokens))
            {
                Assert.Equal(expectedToken.StartIndex, actualToken.StartIndex);
                Assert.Equal(expectedToken.EndIndex, actualToken.EndIndex);
                Assert.Equal(expectedToken.TokenType, actualToken.TokenType);
            }
        }

        private static void AssertOrderOfTokens(IEnumerable<ITokenTextSpan> tokens, IComparer<ITokenTextSpan> comparer)
        {
            Assert.NotNull(tokens);
            ITokenTextSpan lastToken = null;
            foreach (var token in tokens)
            {
                if (lastToken != null)
                {
                    Assert.True(comparer.Compare(lastToken, token) <= 0);
                }

                lastToken = token;
            }
        }

        private static CheckResult GetDefaultCheckResult(string expression, ParserOptions options = null, bool assertNoErrors = true)
        {
            var powerFxConfig = PowerFxConfig.BuildWithEnumStore(new EnumStoreBuilder().WithDefaultEnums(), new TexlFunctionSet());
            var engine = new Engine(powerFxConfig);
            var mockSymbolTable = new MockSymbolTableForTokenization();
            mockSymbolTable.AddControlAsAggregateType("label2", new TypedName(DType.String, DName.MakeValid("Text", out _)));
            mockSymbolTable.AddControlAsControlType("nestedLabel1");
            mockSymbolTable.AddControlAsAggregateType("gallery1", new TypedName(DType.CreateRecord(mockSymbolTable.GetLookupInfoAsTypedName("label2"), mockSymbolTable.GetLookupInfoAsTypedName("nestedLabel1")), DName.MakeValid("Selected", out _)));
            mockSymbolTable.AddTable("foo", null, new TypedName(DType.String, DName.MakeValid("test", out _)));
            mockSymbolTable.AddRecord("ThisItem", BindKind.ThisItem, new TypedName(DType.Number, DName.MakeValid("Income", out _)), new TypedName(DType.String, DName.MakeValid("Name", out _)));
            mockSymbolTable.Add("name", new NameLookupInfo(BindKind.Alias, DType.String, DPath.Root, 0));
            var checkResult = engine.Check(expression, options ?? new ParserOptions { AllowsSideEffects = true, NumberIsFloat = true }, mockSymbolTable);
            if (assertNoErrors)
            {
                Assert.False(checkResult.Errors.Any(), string.Join(Environment.NewLine, checkResult.Errors));
            }

            return checkResult;
        }

        /// <summary>
        /// Just a small handy mock of symbol table to be able to customize binder to compute different token types.
        /// Might not be 100% correct but it works and allows testing against different token types.
        /// </summary>
        private class MockSymbolTableForTokenization : ReadOnlySymbolTable
        {
            public void Add(string name, NameLookupInfo info)
            {
                _variables.Add(name, info);
            }

            public void AddControlAsAggregateType(string name, params TypedName[] props)
            {
                AddRecord(name, BindKind.Control, props);
            }

            public void AddControlAsControlType(string name)
            {
                var controlType = new DType(DKind.Control);
                var controlInfo = new NameLookupInfo(BindKind.Control, controlType, DPath.Root, 0);
                Add(name, controlInfo);
            }

            public void AddTable(string name, BindKind? type = null, params TypedName[] keyValues)
            {
                type ??= BindKind.Data;
                var tableType = DType.CreateTable(keyValues);
                Add(name, new NameLookupInfo(type.Value, tableType, DPath.Root, 0, displayName: DName.MakeValid(name, out _)));
            }

            public TypedName GetLookupInfoAsTypedName(string name)
            {
                var validName = DName.MakeValid(name, out _);
                if (TryLookup(validName, out var lookupInfo))
                {
                    return new TypedName(lookupInfo.Type, validName);
                }

                return new TypedName(DType.Unknown, validName);
            }

            public void AddRecord(string name, BindKind? type = null, params TypedName[] keyValues)
            {
                type ??= BindKind.Data;
                var recordType = DType.CreateRecord(keyValues);
                Add(name, new NameLookupInfo(type.Value, recordType, DPath.Root, 0, displayName: DName.MakeValid(name, out _)));
            }

            internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
            {
                return _variables.TryGetValue(name.Value, out nameInfo);
            }

            internal override void EnumerateNames(List<SymbolEntry> names, EnumerateNamesOptions opts)
            {
                throw new NotImplementedException();
            }
        }
    }
}
