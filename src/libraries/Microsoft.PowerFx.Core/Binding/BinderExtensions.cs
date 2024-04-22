// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    internal static class BinderExtensions
    {
        public static void AddIfNotSameSpan(this IDictionary<Token, string> nodes, Token key, string value)            
        {
            if (!nodes.ContainsKey(key))
            {
                nodes.Add(key, value);
            }
        }
    }

    internal class NodesToReplaceComparer : IEqualityComparer<Token>
    {
        public bool Equals(Token x, Token y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Span.Min == y.Span.Min && x.Span.Lim == y.Span.Lim;
        }

        public int GetHashCode(Token token)
        {
            return Hashing.CombineHash(token.Span.Min.GetHashCode(), token.Span.Lim.GetHashCode());
        }
    }
}
