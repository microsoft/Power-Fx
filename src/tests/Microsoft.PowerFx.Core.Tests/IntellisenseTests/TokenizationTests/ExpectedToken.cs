// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Texl.Intellisense;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    internal class ExpectedToken : ITokenTextSpan
    {
        /// <summary>
        /// Special start index to indicate that start index shoud be computed based on previous token end index.
        /// </summary>
        public const int SpecialStartIdx = -1;

        public int StartIndex { get; set; } = SpecialStartIdx;

        public int Length { get; set; }

        public TokenType TokenType { get; set; }

        public int EndIndex => Length + StartIndex;

        public bool CanBeHidden { get; set; } = false;

        public string TokenName { get; set; } = string.Empty;

        /// <summary>
        /// This kind of token is inserted in test case to compute the relative start indexes correctly.
        /// </summary>
        public bool IsIgnoredPlaceholderToken => TokenType == TokenType.Lim;

        public ExpectedToken(int startIdx, int length, TokenType type)
        {
            StartIndex = startIdx;
            Length = length;
            TokenType = type;
        }

        public override string ToString()
        {
            return $"Start: {StartIndex}, EndIndex: {EndIndex}, Type: {TokenType}";
        }

        public static ExpectedToken CreateStringInterpStartToken(int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, 2, TokenType.StrInterpStart);
        }

        public static ExpectedToken CreateStringInterpEndToken(int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, 1, TokenType.StrInterpEnd);
        }

        public static ExpectedToken CreateIslandStartToken(int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, 1, TokenType.IslandStart);
        }

        public static ExpectedToken CreateIslandEndToken(int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, 1, TokenType.IslandEnd);
        }

        public static ExpectedToken CreateNumLitToken(int length, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.NumLit);
        }

        public static ExpectedToken CreateDecLitToken(int length, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.DecLit);
        }

        public static ExpectedToken CreateStrLitToken(int length, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.StrLit);
        }

        public static ExpectedToken CreateVaridicOpToken(int startIdx = SpecialStartIdx, int length = 1)
        {
            return new ExpectedToken(startIdx, length, TokenType.VariadicOp);
        }

        public static ExpectedToken CreateStrLitToken(string str, int startIdx = SpecialStartIdx)
        {
            return CreateStrLitToken(str.Length, startIdx);
        }

        public static ExpectedToken CreateBinaryOpToken(int length = 1, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.BinaryOp);
        }

        public static ExpectedToken CreateIgnoredPlaceholderToken(int length)
        {
            return new ExpectedToken(SpecialStartIdx, length, TokenType.Lim);
        }

        public static ExpectedToken CreateUnaryOpToken(int length = 1, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.UnaryOp);
        }

        public static ExpectedToken CreateDelimeterToken(int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, 1, TokenType.Delimiter);
        }

        public static ExpectedToken CreateFunctionToken(int length = 1, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.Function);
        }

        public static ExpectedToken CreateDottedNamePartToken(int length, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.DottedNamePart);
        }

        public static ExpectedToken CreateControlToken(int length, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.Control);
        }

        public static ExpectedToken CreateEnumToken(int length, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.Enum);
        }

        public static ExpectedToken CreateColorEnumToken(int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, 5, TokenType.Enum);
        }

        public static ExpectedToken CreateDataToken(int length, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.Data);
        }

        public static ExpectedToken CreateThisItemToken(int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, "ThisItem".Length, TokenType.ThisItem);
        }

        public static ExpectedToken CreateAliasToken(int length, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(startIdx, length, TokenType.Alias);
        }
    }
}
