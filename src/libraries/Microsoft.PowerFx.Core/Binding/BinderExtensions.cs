// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    internal static class BinderExtensions
    {
        public static bool AddIfNotExisting<TKey, TValue>(this Dictionary<TKey, TValue> nodes, TKey key, TValue value)
        {
            if (!nodes.ContainsKey(key))
            {                
                nodes.Add(key, value);
                return true;
            }

            return false;
        }
    }
}
