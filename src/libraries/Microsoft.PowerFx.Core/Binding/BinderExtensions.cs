// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    internal static class BinderExtensions
    {
        public static void AddIfNotSameSpan(this IList<KeyValuePair<Token, string>> nodes, Token key, string value)            
        {
            foreach (KeyValuePair<Token, string> kvp in nodes)
            {
                if (kvp.Key.Span == key.Span)
                {
                    return;
                }
            }

            nodes.Add(new KeyValuePair<Token, string>(key, value));
        }
    }
}
