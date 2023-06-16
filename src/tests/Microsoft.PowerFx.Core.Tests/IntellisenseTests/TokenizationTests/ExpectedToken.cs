// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

extern alias PfxCore;

using PfxCore.Microsoft.PowerFx.Core.Texl.Intellisense;

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

        public static ExpectedToken CreateIgnoredPlaceholderToken(int length)
        {
            return new ExpectedToken(SpecialStartIdx, length, TokenType.Lim);
        }

        public static ExpectedToken CreateUnaryOpToken(int length = 1, int startIdx = SpecialStartIdx)
        {
            return new ExpectedToken(SpecialStartIdx, length, TokenType.UnaryOp);
        }
    }
}
