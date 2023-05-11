// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Stores the regular expression cache.
    /// This is for compile time only, not runtime.
    /// </summary>
    internal class RegexTypeCache
    {
        // Key is ("tbl_" or "rec_" + regex expression)
        // See Match.cs code for details
        internal ConcurrentDictionary<string, Tuple<DType, bool, bool, bool>> Cache { get; }

        internal int CacheSize { get; }

        public RegexTypeCache(int regexCacheSize)
        {
            if (regexCacheSize < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(regexCacheSize), "Regular expression cache size must be -1 (disabled) or positive.");
            }

            CacheSize = regexCacheSize;
            Cache = regexCacheSize == -1 ? null : new ConcurrentDictionary<string, Tuple<DType, bool, bool, bool>>();
        }
    }
}
