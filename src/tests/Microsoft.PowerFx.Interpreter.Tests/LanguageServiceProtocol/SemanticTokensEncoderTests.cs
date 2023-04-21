// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.LanguageServiceProtocol
{
    public class SemanticTokensEncoderTests : PowerFxTest
    {
        private static readonly IDictionary<TokenType, uint> EncodedTokens = new Dictionary<TokenType, uint>
        {
            { TokenType.Unknown, 0u },
            { TokenType.Control, 1u },
            { TokenType.Data, 2u },
            { TokenType.Function, 3u },
            { TokenType.Alias, 4u },
            { TokenType.Enum, 5u },
            { TokenType.Service, 6u },
            { TokenType.ThisItem, 7u },
            { TokenType.Punctuator, 8u },
            { TokenType.DottedNamePart, 9u },
            { TokenType.BinaryOp, 10u },
            { TokenType.UnaryOp, 11u },
            { TokenType.VariadicOp, 12u },
            { TokenType.BoolLit, 13u },
            { TokenType.NumLit, 14u },
            { TokenType.DecLit, 15u },
            { TokenType.StrLit, 16u },
            { TokenType.Delimiter, 17u },
            { TokenType.ScopeVariable, 18u },
            { TokenType.Comment, 19u },
            { TokenType.Self, 20u },
            { TokenType.Parent, 21u },
            { TokenType.StrInterpStart, 22u },
            { TokenType.StrInterpEnd, 23u },
            { TokenType.IslandStart, 24u },
            { TokenType.IslandEnd, 25u }
        };

        // This test is to ensure that any changes in TokenType enum don't break the encoding of token types using their positions in TokenType for semantic tokenization
        // Any changes that are not towards the end of TokenType would fail this test and person making changes would need to update the encoding
        [Fact]
        public void TestTokenTypesAreEncodedCorrectly()
        {
            // Arrange
            const string potentialChangesInTokenTypeEnumMsg = "Looks like changes in TokenType broke the encoding for semantic tokenization. Update the encoding in this test  and all the clients that uses semantic tokenization such as @microsoft/power-fx-formulabar npm package";

            // Act & Assert
            Assert.All(EncodedTokens.Values, (encodedVal) =>
            {
                var tokenType = SemanticTokensEncoder.TestOnly_GetTokenTypeFromEncodedValue(encodedVal);
                Assert.True(EncodedTokens.TryGetValue(tokenType, out var actualEncodedVal), $"{potentialChangesInTokenTypeEnumMsg}.\nCould not find encoded value for {tokenType}");
                Assert.True(encodedVal == actualEncodedVal, $"{potentialChangesInTokenTypeEnumMsg}.\nExpected encoded value {encodedVal} and actual one {actualEncodedVal} don't match for token type {tokenType}");
            });
        }

        [Theory]
        [InlineData("Max(1, 2, 3)")]
        [InlineData("If(Len(Phone_Number) < 10,\nNotify(\"Invalid Phone Number\"),Notify(\"Valid Phone No\"))")]
        [InlineData("1 + 1")]
        [InlineData("1")]
        [InlineData("true")]
        [InlineData("If(Len(Phone_Number) < 10,\r\nNotify(InvalidPhoneWarningText)\r\n,Notify({\r\nPhoneNumber: Phone_Number\r\n}.PhoneNumber))")]
        [InlineData("1+-2;true;\"String Literal\";Color.Blue;Max(1,2,3);DataVar;$\"1 + 2 = {3}\";// This is Comment")]
        public void TestNoOverlappingAndAllSingleTokensAreEncodedCorrectly(string expression)
        {
            // Arrange
            var checkResult = GetDefaultCheckResult(expression);
            var tokens = checkResult.GetTokens(new TokensComparer());
            var eol = ChooseEol(expression);

            // Act
            var encodedTokens = SemanticTokensEncoder.EncodeTokens(tokens, expression, eol).ToArray();

            // Assert
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(encodedTokens, expression, eol);
            Assert.Equal(tokens.Count, encodedTokens.Length / SemanticTokensEncoder.SlotsPerToken);
            for (var i = SemanticTokensEncoder.SlotsPerToken - 1; i >= 0 && i < encodedTokens.Length; i += SemanticTokensEncoder.SlotsPerToken)
            {
                Assert.Equal(0u, encodedTokens[i]);
            }

            Assert.Equal(tokens.Count, decodedTokens.Count);
            Assert.All(tokens.Zip(decodedTokens), (tokens) =>
            {
                var expectedToken = tokens.First;
                var actualToken = tokens.Second;
                Assert.Equal(expectedToken.StartIndex, actualToken.StartIndex);
                Assert.Equal(expectedToken.EndIndex, actualToken.EndIndex);
                Assert.Equal(expectedToken.TokenType, actualToken.TokenType);
            });
        }

        [Theory]
        [InlineData("Max(10, 20, 30)")]
        [InlineData("If(Len(Phone_Number) < 10,\nNotify(\"Invalid Phone Number\"),Notify(\"Valid Phone No\"))")]
        [InlineData("1 + 1")]
        [InlineData("1")]
        [InlineData("true")]
        [InlineData("If(Len(Phone_Number) < 10,\r\nNotify(InvalidPhoneWarningText)\r\n,Notify({\r\nPhoneNumber: Phone_Number\r\n}.PhoneNumber))")]
        [InlineData("1+-2;true;\"String Literal\";Color.Blue;Max(1,2,3);DataVar;$\"1 + 2 = {3}\";// This is Comment")]
        public void TestOverlappingAndAllSingleTokensAreEncodedCorrectly(string expression)
        {
            // Arrange
            const int MaxOverlappingToks = 5;
            var checkResult = GetDefaultCheckResult(expression);
            var tokens = checkResult.GetTokens().ToList();
            var overlappingToks = tokens.ToList();
            var eol = ChooseEol(expression);   
            
            for (var i = 0; i < MaxOverlappingToks; i++)
            {
                var rand = new Random();
                var idx = rand.Next(tokens.Count);
                var chosenToken = tokens[idx];
                for (var j = chosenToken.StartIndex; j < chosenToken.EndIndex; j++)
                {
                    var endIndex = -1;
                    if (j == chosenToken.StartIndex)
                    {
                        endIndex = chosenToken.EndIndex + 1;
                    }
                    else
                    {
                        endIndex = rand.Next(j + 1, expression.Length + 1);
                    }

                    overlappingToks.Add(new TokenTextSpan(chosenToken.TokenName, j, endIndex, chosenToken.TokenType, chosenToken.CanBeHidden));
                }
            }

            tokens.Sort(new TokensComparer());

            // Act
            var encodedTokens = SemanticTokensEncoder.EncodeTokens(overlappingToks, expression, eol).ToArray();

            // Assert
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(encodedTokens, expression, eol);
            Assert.Equal(tokens.Count, encodedTokens.Length / SemanticTokensEncoder.SlotsPerToken);
            for (var i = SemanticTokensEncoder.SlotsPerToken - 1; i >= 0 && i < encodedTokens.Length; i += SemanticTokensEncoder.SlotsPerToken)
            {
                Assert.Equal(0u, encodedTokens[i]);
            }

            Assert.Equal(tokens.Count, decodedTokens.Count);
            Assert.All(tokens.Zip(decodedTokens), (tokens) =>
            {
                var expectedToken = tokens.First;
                var actualToken = tokens.Second;
                Assert.Equal(expectedToken.StartIndex, actualToken.StartIndex);
                Assert.Equal(expectedToken.EndIndex, actualToken.EndIndex);
                Assert.Equal(expectedToken.TokenType, actualToken.TokenType);
            });
        }

        private static string ChooseEol(string expression)
        {
            if (expression.Contains("\r\n"))
            {
                return "\r\n";
            }

            return "\n";
        }

        private static CheckResult GetDefaultCheckResult(string expression)
        {
            var powerFxConfig = PowerFxConfig.BuildWithEnumStore(new EnumStoreBuilder().WithDefaultEnums(), new TexlFunctionSet());
            powerFxConfig.SymbolTable.AddVariable("Phone_Number", FormulaType.String);
            powerFxConfig.SymbolTable.AddVariable("InvalidPhoneWarningText", FormulaType.String);
            powerFxConfig.SymbolTable.AddEntity(new DataSource(), DName.MakeValid("DataVar", out var _));
            powerFxConfig.SymbolTable.AddFunction(new NotifyFunc());
            var engine = new RecalcEngine(powerFxConfig);
            var checkResult = engine.Check(expression, new ParserOptions { AllowsSideEffects = true, NumberIsFloat = true });
            Assert.False(checkResult.Errors.Any());
            return checkResult;
        }

        private class TokensComparer : IComparer<ITokenTextSpan>
        {
            public int Compare(ITokenTextSpan x, ITokenTextSpan y)
            {
                var score = x.StartIndex - y.StartIndex;
                return score;
            }
        }

        private class NotifyFunc : TexlFunction
        {
            public NotifyFunc()
                : base(DPath.Root, "Notify", "Notify", null, FunctionCategories.Behavior, DType.Void, BigInteger.Zero, 1, 1, DType.String)
            {
            }

            public override bool IsSelfContained => true;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                return Enumerable.Empty<TexlStrings.StringGetter[]>();  
            }
        }

        private class DataSource : IExternalDataSource
        {
            public string Name => "DataSource";

            public bool IsSelectable => true;

            public bool IsDelegatable => true;

            public bool RequiresAsync => true;

            public IExternalDataEntityMetadataProvider DataEntityMetadataProvider => null;

            public DataSourceKind Kind => DataSourceKind.Collection;

            public IExternalTableMetadata TableMetadata => null;

            public IDelegationMetadata DelegationMetadata => null;

            public DName EntityName => DName.MakeValid(Name, out var _);

            public DType Type => DType.EmptyTable;

            public bool IsPageable => true;
        }
    }
}
