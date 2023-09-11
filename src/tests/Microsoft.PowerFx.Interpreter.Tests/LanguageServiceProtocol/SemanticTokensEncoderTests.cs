// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
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
        [InlineData("1+-2;true;\"String Literal\";Color.Blue;Max(1,2,3);DataVar;DataVar.Prop1;$\"1 + 2 = {3}\";// This is Comment")]
        public void TestNoOverlappingAndAllSingleLineTokensAreEncodedCorrectly(string expression)
        {
            // Arrange
            var checkResult = GetDefaultCheckResult(expression);
            var tokens = checkResult.GetTokens();
            tokens = tokens.OrderBy(token => token, new TokensComparer());
            var eol = ChooseEol(expression);

            // Act
            var encodedTokens = SemanticTokensEncoder.EncodeTokens(tokens, expression, eol);

            // Assert
            AssertEncodedTokens(encodedTokens, tokens, expression, eol);
        }

        [Theory]
        [InlineData("Max(10, 20, 30)")]
        [InlineData("If(Len(Phone_Number) < 10,\nNotify(\"Invalid Phone Number\"),Notify(\"Valid Phone No\"))")]
        [InlineData("1 + 1")]
        [InlineData("1")]
        [InlineData("true")]
        [InlineData("If(Len(Phone_Number) < 10,\r\nNotify(InvalidPhoneWarningText)\r\n,Notify({\r\nPhoneNumber: Phone_Number\r\n}.PhoneNumber))")]
        [InlineData("1+-2;true;\"String Literal\";Color.Blue;Max(1,2,3);DataVar;$\"1 + 2 = {3}\";// This is Comment")]
        public void TestOverlappingAndAllSingleLineTokensAreEncodedCorrectly(string expression)
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
            var encodedTokens = SemanticTokensEncoder.EncodeTokens(overlappingToks, expression, eol);

            // Assert
            AssertEncodedTokens(encodedTokens, tokens, expression, eol, false);
        }

        [Theory]
        [InlineData("1 +2;/*This is comment line one\r\nThis is comment line two\r\nLine 3 \r\n*/\r\n\r\ntrue;Max(1,2,3)")]
        [InlineData("1 +2;\"This is string line one\r\nThis is string line two\r\nLine 3 \r\n\"\r\n\r\n;true;Max(1,2,3)")]
        [InlineData("1 +2;\"This is string line one\r\nThis is string line two\r\nLine 3 \r\n\"\r\n\r\n;//This is single line comment \r\n\r\ntrue;Max(1,2,3)")]
        [InlineData("\"String One\r\nString Two\"\"String Three\r\nStringFour\r\n\"\r\n\r\n\r\n;/*This is comment \r\n Comment Two\r\n */\r\n\r\n\r\n\"Yet another string\r\n yet another line\r\n\"")]
        [InlineData("Max\r\n(1\r\n,1344,\r\n34\r\n);\"String One\r\nString Two\"\"String Three\r\nStringFour\r\n\"\r\n\r\n\r\n;/*This is comment \r\n Comment Two\r\n */\r\n\r\n\r\n\"Yet another string\r\n yet another line\r\n\"")]
        [InlineData("// This is single line comment \r\n")]
        [InlineData("/* One line \r\n Second Line */\r\n\r\n\r\n/*Third Comment First One \r\n Fourth Comment*/")]
        [InlineData("\"\r\n\r\n\r\n\r\nA\r\n       \r\nS\"\r\n\r\n\r\n\r\n       /*\r\n\r\nYet another comment \r\n Yet another line*/            \r\n    \r\n\r\n//Single Line Comment\r\n\r\n\r\n//Single Line Comment")]
        [InlineData("$\"This is interplolated \r\n string and 1 + 2 = \r\n{3}\r\n\r\n \r\n{\"End\r\n of a\r\n string\"\r\n\r\n\r\n\r\n       /*\r\n\r\nYet another comment \r\n Yet another line*/            \r\n    \r\n\r\n/* One line \r\n Second Line */\r\n\r\n\r\n/*Third Comment First One \r\n Fourth Comment*/}\"")]
        [InlineData("//Comment one \n//Comment Two \n \"String One\n\";\"String One \n String Two \n\";")]
        [InlineData("\n//Comment one \n//Comment Two \n \"String One\n\";\"String One \n String Two \n\";")]
        [InlineData("/* One line \r\n Second Line */\r\n\r\n\r\n/*Third Comment First One \r\n Fourth Comment*/ \r\n //Comment one")]
        [InlineData("//Comment one \n /* Comment Two */ \n//Comment Three \n \"String One\n\";\"String One \n String Two \n\";")]
        [InlineData("/* Comment One */ \n//Comment Two \n //Comment Three \n \"String One\n\";\"String One \n String Two \n\";")]
        [InlineData("// Comment one//Comment Two")]
        [InlineData("// Comment one\n//Comment Two")]
        [InlineData("// Comment one\n\n\n//Comment Two")]
        [InlineData("// Comment one\n\r\n\r\n//Comment Two")]
        [InlineData("// Comment one \\n\"String token\"\\n//Comment Two")]
        [InlineData("// Comment one \\n\\n\"String token\"\\n//Comment Two\"String token two\"\\n//Comment Three")]
        [InlineData("// Comment one \\n\\n\\r\\n\"String token\"\\n\\n\\n//Comment Two\"String token two\"\\n\\r\\n\\r\\n//Comment Three")]
        [InlineData("// Comment one \\n\\n\\r\\n\"String token\"\\n\\n\\n/* Comment Two */\"String token two\"\\n\\r\\n\\r\\n//Comment Three")]
        [InlineData("// Comment one \\n\\n\\r\\n\"String token\"\\n\\n\\n/* Comment Two */\\n\"String token two\"\\n\\r\\n\\r\\n//Comment Three")]
        [InlineData("// Comment one \\n\\n\\r\\n\"String token\"\\n\\n\\n/* Comment Two */\"String token two\"\\n\"String token three\"\\n\\r\\n\\r\\n//Comment Three")]
        [InlineData("/*jj*/RGBA(255, 255, 255, 1)\n//yes")]
        [InlineData("/*jj*/\r\nRGBA(255, 255, 255, 1)\n//yes")]
        [InlineData("/*jj*///yes")]
        [InlineData("/*jj*/\n//yes")]
        [InlineData("/*jj*/\n\n//yes")]
        [InlineData("/*jj*/\n\r\n\r\n//yes")]
        [InlineData("/*jj*/\n\r\n\r\n//yes/*kk*/")]
        [InlineData("/*jj*/\n\r\n\r\n//yes\n/*kk*/")]
        [InlineData("/*jj*/\n\r\n\r\n//yes\n\n\n/*kk*/")]
        [InlineData("/*jj*/\n\r\n\r\n//yes\n \"String token one\"/*kk*/")]
        [InlineData("/*jj*/\n\r\n\r\n//yes\n \"String token one\"\n/*kk*/")]
        [InlineData("/*jj*/\n\r\n\r\n//yes\n \"String token one\"\n\n/*kk*/")]
        [InlineData("/*jj*/\nRGBA(\n    /*j2*/\n    255,\n    255,\n    255,\n    1\n)//yes")]
        [InlineData("/*jj*/\nRGBA(\n    /*j2*/\n    255,\n    255,\n    255,\n    1\n)\n//yes")]
        [InlineData("/*jj*/\nRGBA(\n    /*j2*/    255,\n    255,\n    255,\n    1\n)\n//yes")]
        [InlineData("/*jj*/RGBA(\n    /*j2*/\n    255,\n    255,\n    255,\n    1\n)\n//yes")]
        [InlineData("/*jj*/RGBA(\n    /*j2*/\n    255,\n    255,\n    255,\n    1\n)\n\n\n\n//yes")]
        public void TestMultilineTokensAreEncodedCorrectly(string expression)
        {
            // Arrange
            var checkResult = GetDefaultCheckResult(expression);
            var tokens = checkResult.GetTokens();
            tokens = tokens.OrderBy(token => token, new TokensComparer());
            var eol = ChooseEol(expression);

            // Act
            var encodedTokens = SemanticTokensEncoder.EncodeTokens(tokens, expression, eol);

            // Assert
            AssertEncodedTokens(encodedTokens, tokens, expression, eol, true);
        }

        private static void AssertEncodedTokens(IEnumerable<uint> encodedTokensCollection, IEnumerable<ITokenTextSpan> tokens, string expression, string eol, bool hasMultilineTokens = false)
        {
            var encodedTokens = encodedTokensCollection.ToArray();
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(encodedTokens, expression, eol);

            if (hasMultilineTokens)
            {
                var nonBrokenTokens = decodedTokens.Where(decodedToken => tokens.SingleOrDefault(actualToken => actualToken.StartIndex == decodedToken.StartIndex && actualToken.EndIndex == decodedToken.EndIndex && actualToken.TokenType == decodedToken.TokenType) != default).ToList();
                var brokenTokens = decodedTokens.Where(decodedToken => !nonBrokenTokens.Contains(decodedToken));
                var tokensPutTogether = new List<ITokenTextSpan>();
                TokenWithAdjustment currentToken = null;
                foreach (var brokenToken in brokenTokens)
                {
                    var adjustedStartIdx = brokenToken.StartIndex;
                    while (adjustedStartIdx > 0 && eol.IndexOf(expression[adjustedStartIdx - 1]) != -1)
                    {
                        adjustedStartIdx--;
                    }

                    var adjustedEndIndex = brokenToken.EndIndex;
                    while (adjustedEndIndex < expression.Length && eol.IndexOf(expression[adjustedEndIndex]) != -1)
                    {
                        adjustedEndIndex++;
                    }

                    if (currentToken == null || (currentToken.EndIndex != brokenToken.StartIndex || currentToken.TokenType != brokenToken.TokenType))
                    {
                        currentToken = new TokenWithAdjustment(adjustedStartIdx, adjustedEndIndex, brokenToken.TokenType, brokenToken.StartIndex - adjustedStartIdx, adjustedEndIndex - brokenToken.EndIndex);
                    }
                    else
                    {
                        currentToken = new TokenWithAdjustment(currentToken.StartIndex, adjustedEndIndex, currentToken.TokenType, currentToken.LeftAdjustment, adjustedEndIndex - brokenToken.EndIndex);
                    }

                    if (currentToken != null)
                    {
                        for (int left = 0; left <= currentToken.LeftAdjustment; left++)
                        {
                            for (int right = 0; right <= currentToken.RightAdjustment; right++)
                            {
                                if (tokens.Any(tok => tok.StartIndex == currentToken.StartIndex + left && tok.EndIndex == currentToken.EndIndex - right && tok.TokenType == currentToken.TokenType))
                                {
                                    tokensPutTogether.Add(new TokenWithAdjustment(currentToken.StartIndex + left, currentToken.EndIndex - right, currentToken.TokenType));
                                    currentToken = null;
                                    break;
                                }
                            }

                            if (currentToken == null)
                            {
                                break;
                            }
                        }
                    }
                }

                Assert.Null(currentToken);
                decodedTokens = nonBrokenTokens.Concat(tokensPutTogether).OrderBy(tok => tok, new TokensComparer()).ToList();
            }

            Assert.True(encodedTokens.Length / SemanticTokensEncoder.SlotsPerToken >= tokens.Count());
            for (var i = SemanticTokensEncoder.SlotsPerToken - 1; i >= 0 && i < encodedTokens.Length; i += SemanticTokensEncoder.SlotsPerToken)
            {
                Assert.Equal(0u, encodedTokens[i]);
            }

            Assert.Equal(tokens.Count(), decodedTokens.Count());
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
            var mockSymbolTable = new MockSymbolTable();
            mockSymbolTable.Add("Phone_Number", new NameLookupInfo(BindKind.PowerFxResolvedObject, DType.String, DPath.Root, 0));
            mockSymbolTable.AddRecord("DataVar", null, new TypedName(DType.String, DName.MakeValid("Prop1", out _)));
            mockSymbolTable.Add("InvalidPhoneWarningText", new NameLookupInfo(BindKind.PowerFxResolvedObject, DType.String, DPath.Root, 0));
            powerFxConfig.SymbolTable.AddFunction(new NotifyFunc());
            var engine = new Engine(powerFxConfig);
            var checkResult = engine.Check(expression, new ParserOptions { AllowsSideEffects = true, NumberIsFloat = true }, mockSymbolTable);
            Assert.False(checkResult.Errors.Any(), string.Join(Environment.NewLine, checkResult.Errors));
            return checkResult;
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

        private class TokenWithAdjustment : ITokenTextSpan
        {
            public string TokenName { get; set; } = string.Empty;

            public int StartIndex { get; set; }

            public int EndIndex { get; set; }

            public TokenType TokenType { get; set; }

            public bool CanBeHidden => false;

            public int LeftAdjustment { get; set; } = 0;

            public int RightAdjustment { get; set; } = 0;

            public TokenWithAdjustment(int startIdx, int endIdx, TokenType type, int leftAdjustment = 0, int rightAdjustment = 0)
            {
                StartIndex = startIdx;
                EndIndex = endIdx;
                TokenType = type;
                LeftAdjustment = leftAdjustment;
                RightAdjustment = rightAdjustment;
            }
        }

        private class TokensComparer : IComparer<ITokenTextSpan>
        {
            public int Compare(ITokenTextSpan x, ITokenTextSpan y)
            {
                var score = x.StartIndex - y.StartIndex;
                return score;
            }
        }

        /// <summary>
        /// Just a small handy mock of symbol table to be able to customize binder to compute different token types.
        /// Might not be 100% correct but it works and allows testing against different token types.
        /// </summary>
        private class MockSymbolTable : ReadOnlySymbolTable
        {
            public void Add(string name, NameLookupInfo info)
            {
                _variables.Add(name, info);
            }

            public void AddRecord(string name, BindKind? type = null, params TypedName[] keyValues)
            {
                type ??= BindKind.Data;
                var recordType = DType.CreateRecord(keyValues);
                Add(name, new NameLookupInfo(type.Value, recordType, DPath.Root, 0, displayName: DName.MakeValid(name, out _)));
            }

            internal override void EnumerateNames(List<SymbolEntry> names, EnumerateNamesOptions opts)
            {
                throw new NotImplementedException();
            }

            internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
            {
                return _variables.TryGetValue(name.Value, out nameInfo);
            }
        }
    }
}
