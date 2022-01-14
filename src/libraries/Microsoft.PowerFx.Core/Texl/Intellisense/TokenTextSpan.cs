// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.AppMagic.Transport;

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

    internal sealed class TokenTextSpan : ITokenTextSpan
    {
        public string TokenName { get; private set; }

        public int StartIndex { get; private set; }

        public int EndIndex { get; private set; }

        public TokenType TokenType { get; private set; }

        public bool CanBeHidden { get; private set; }

        public TokenTextSpan(string name, int startIndex, int endIndex, TokenType type, bool canHide)
        {
            TokenName = name;
            StartIndex = startIndex;
            EndIndex = endIndex;
            TokenType = type;
            CanBeHidden = canHide;
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
                return self.TokenName.CompareTo(other.TokenName);
            }

            return self.StartIndex.CompareTo(other.StartIndex);
        }
    }
}