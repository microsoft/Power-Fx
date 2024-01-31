// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AppMagic.Transport;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    [TransportType(TransportKind.ByValue)]
    internal interface ITokenTextSpan
    {
        string TokenName { get; }

        int StartIndex { get; }

        int EndIndex { get; }

        TokenType TokenType { get; }

        bool CanBeHidden { get; }
    }

    internal sealed class TokenTextSpan : ITokenTextSpan, ITextFirstFlag
    {
        public string TokenName { get; private set; }

        public int StartIndex { get; private set; }

        public int EndIndex { get; private set; }

        public TokenType TokenType { get; private set; }

        public bool CanBeHidden { get; private set; }

        public bool IsTextFirst { get; private set; }

        public TokenTextSpan(string name, int startIndex, int endIndex, TokenType type, bool canHide)
        {
            TokenName = name;
            StartIndex = startIndex;
            EndIndex = endIndex;
            TokenType = type;
            CanBeHidden = canHide;
        }

        public TokenTextSpan(string name, Token token, TokenType type, bool canHide = false)
            : this(name, token.VerifyValue().Span.Min, token.VerifyValue().Span.Lim, type, canHide)
        {
            IsTextFirst = token is ITextFirstFlag flag ? flag.IsTextFirst : false;
        }
    }

    internal sealed class TokenTextSpanComparer : IComparer<ITokenTextSpan>
    {
        public int Compare(ITokenTextSpan self, ITokenTextSpan other)
        {
            if (self == null)
            {
                if (other == null)
                {
                    return 0;
                }

                return -1;
            }

            if (other == null)
            {
                return 1;
            }

            if (self.TokenType != other.TokenType)
            {
                return self.TokenType.CompareTo(other.TokenType);
            }

            if (self.TokenName != other.TokenName)
            {
                return string.Compare(self.TokenName, other.TokenName, StringComparison.Ordinal);
            }

            return self.StartIndex.CompareTo(other.StartIndex);
        }
    }

    /// <summary>
    /// Wraps a TokenTextSpan to be accessible from the engine.
    /// </summary>
    public class TokenTextSpanWrap : ITokenTextSpan, ITextFirstFlag
    {
        private readonly TokenTextSpan _tokenTextSpan;

        public string TokenName => _tokenTextSpan.TokenName;

        public int StartIndex => _tokenTextSpan.StartIndex;

        public int EndIndex => _tokenTextSpan.EndIndex;

        public TokenType TokenType => _tokenTextSpan.TokenType;

        public bool CanBeHidden => _tokenTextSpan.CanBeHidden;

        public bool IsTextFirst => _tokenTextSpan.IsTextFirst;

        internal TokenTextSpanWrap(TokenTextSpan tokenTextSpan)
        {
            _tokenTextSpan = tokenTextSpan;
        }
    }
}
