// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    internal class RegexCache
    {
        internal ConcurrentDictionary<string, Tuple<DType, bool, bool, bool>> RegexTypeCache { get; }

        internal int RegexCacheSize { get; }

        public RegexCache(int regexCacheSize)
        {
            if (regexCacheSize < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(regexCacheSize), "Regular expression cache size must be -1 (disabled) or positive.");
            }

            RegexCacheSize = regexCacheSize;
            RegexTypeCache = regexCacheSize == -1 ? null : new ConcurrentDictionary<string, Tuple<DType, bool, bool, bool>>();
        }
    }
}
