// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Stores the regular expression cache.
    /// This is for compile time only, not runtime.
    /// </summary>
    internal class RegexTypeCache
    {
        // Key is ("tbl_" or "rec_" + schema altering options + regex expression)
        // DType can be null if we have validated the regular expression, but didn't need the type for IsMatch
        // See Match.cs code for details
        internal ConcurrentDictionary<string, RegexTypeCacheEntry> Cache { get; }

        internal int CacheSize { get; }

        public RegexTypeCache(int regexCacheSize)
        {
            if (regexCacheSize < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(regexCacheSize), "Regular expression cache size must be -1 (disabled) or positive.");
            }

            CacheSize = regexCacheSize;
            Cache = regexCacheSize == -1 ? null : new ConcurrentDictionary<string, RegexTypeCacheEntry>();
        }

        public void Add(string key, RegexTypeCacheEntry entry)
        {
            if (Cache == null)
            {
                // nothing to do, we drop the entry
                return;
            }

            if (Cache.Count >= CacheSize)
            {
                // To preserve memory during authoring, we clear the cache if it gets
                // too large. This should only happen in a minority of cases and
                // should have no impact on deployed apps.
                Cache.Clear();
            }

            Cache[key] = entry;
        }

        public bool TryLookup(string key, out RegexTypeCacheEntry entry)
        {
            if (Cache != null && Cache.ContainsKey(key))
            {
                entry = Cache[key];
                return true;
            }

            entry = null;
            return false;
        }
    }

    internal class RegexTypeCacheEntry
    {
        public DType ReturnType;
        public ErrorResourceKey? Error;
        public DocumentErrorSeverity ErrorSeverity;
        public string ErrorParam;
    }
}
