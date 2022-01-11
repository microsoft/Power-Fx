// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// It provides a cached reusable <see cref="StringBuilder"/> instance per thread.
    /// This optimization reduces the number of instances constructed and collected.
    /// <br/>
    /// Class specialization prevents other components from sharing the same <see cref="StringBuilder"/>
    /// instance in the current thread.
    /// </summary>
    /// <remarks>
    /// This implementation is copied from the CLR, except this class is generic.
    /// It's done to allow multiple components to use their own <see cref="StringBuilderCache{T}"/> objects.
    /// </remarks>
    /// <see cref="https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/Text/StringBuilderCache.cs"/>
    /// <example>
    /// using SBCache = StringBuilderCache&lt;SampleClass&gt;;
    /// // ...
    /// StringBuilder sb = SBCache.Acquire(capacity: 32);
    /// sb.Append("sample text");
    /// string result = SBCache.GetStringAndRelease(sb);.
    /// </example>
    internal static class StringBuilderCache<T>
    {
        // The value 360 was chosen in discussion with performance experts as a compromise between using
        // as litle memory (per thread) as possible and still covering a large part of short-lived
        // StringBuilder creations on the startup path of VS designers.
        private static volatile int MaxBuilderSize = 360;

        [ThreadStatic]
        private static StringBuilder CachedInstance;

        /// <summary>
        /// Updates the default value for the maximum cached <see cref="StringBuilder"/> size.
        /// If the requested size is greater than this value, a new (non-cached) <see cref="StringBuilder"/> instance
        /// is created.
        /// </summary>
        /// <see cref="Acquire(int)"/>
        /// <remarks>
        /// Prefer calling this method before any other methods from this class.
        /// </remarks>
        public static void SetMaxBuilderSize(int maxSize)
        {
            Contracts.CheckParam(maxSize > 0, nameof(maxSize));
            MaxBuilderSize = maxSize;
        }

        public static StringBuilder Acquire(int capacity)
        {
            if (capacity <= MaxBuilderSize)
            {
                var sb = CachedInstance;

                // Avoid stringbuilder block fragmentation by getting a new StringBuilder
                // when the requested size is larger than the current capacity
                if (capacity <= sb?.Capacity)
                {
                    CachedInstance = null;
                    return sb;
                }
            }

            return new StringBuilder(capacity);
        }

        public static void Release(StringBuilder sb)
        {
            if (sb.Capacity <= MaxBuilderSize)
            {
                CachedInstance = sb;
                sb.Clear();
            }
        }

        public static string GetStringAndRelease(StringBuilder sb)
        {
            var result = sb.ToString();
            Release(sb);
            return result;
        }
    }
}
