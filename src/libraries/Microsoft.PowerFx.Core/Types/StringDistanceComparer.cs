// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Types
{
    /// <summary>
    /// A comparer that orders based on the Damerau-Levenshtein distance to a given zero-point string.
    /// The distances are modified to be case aware: a case mismatch is 0.1 units of distance, while
    /// a full mismatch and an insertion or deletion are worth the normal 1 unit of distance.
    /// </summary>
    internal class StringDistanceComparer : IComparer<string>
    {
        private readonly string _original;
        private readonly int _maxLength;

        private readonly Dictionary<string, float> _cache = new Dictionary<string, float>();

        public StringDistanceComparer(string original, int maxLength = int.MaxValue)
        {
            _original = original;
            _maxLength = maxLength;
        }

        public float Distance(string other)
        {
            if (_original.Length > _maxLength || other.Length > _maxLength)
            {
                return float.MaxValue;
            }

            if (_cache.TryGetValue(other, out var cached))
            {
                return cached;
            }

            // Common prefixes will be frequent, skip them.
            int start;
            for (start = 0; start < other.Length && start < _original.Length; ++start)
            {
                if (other[start] != _original[start])
                {
                    break;
                }
            }

            // One string is a prefix of the other, so we just have to give the length of the trailing portion
            if (start == other.Length - 1 || start == _original.Length - 1)
            {
                _cache[other] = Math.Abs(other.Length - _original.Length);
                return _cache[other];
            }

            // Need to leave one extra character for transpositions.
            start = Math.Max(start - 1, 0);

            _cache[other] = CoreDistance(
                _original.Substring(start),
                other.Substring(start));
            return _cache[other];
        }

        private static float CoreDistance(string left, string right)
        {
            var distances = new float[left.Length + 1, right.Length + 1];

            for (var i = 1; i < left.Length + 1; ++i)
            {
                distances[i, 0] = i;
            }

            for (var j = 1; j < right.Length + 1; ++j)
            {
                distances[0, j] = j;
            }

            for (var j = 1; j < right.Length + 1; ++j)
            {
                for (var i = 1; i < left.Length + 1; ++i)
                {
                    var substitute = distances[i - 1, j - 1];
                    if (left[i - 1] != right[j - 1])
                    {
                        substitute +=
                            char.ToLowerInvariant(left[i - 1]) != char.ToLowerInvariant(right[j - 1])
                                ? 1
                                : 0.1f;
                    }

                    var delete = distances[i - 1, j] + 1;
                    var insert = distances[i, j - 1] + 1;

                    distances[i, j] = Math.Min(delete, Math.Min(insert, substitute));
                    if (i > 1 && j > 1 && left[i - 1] == right[j - 2] && left[i - 2] == right[j - 1])
                    {
                        distances[i, j] = Math.Min(
                            distances[i, j],
                            distances[i - 2, j - 2] + 1);
                    }
                }
            }

            return distances[left.Length, right.Length];
        }

        public int Compare(string x, string y)
        {
            return Distance(x).CompareTo(Distance(y));
        }
    }
}