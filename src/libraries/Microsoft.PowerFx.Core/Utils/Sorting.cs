// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static class Sorting
    {
        public static void RemoveDupsFromSorted<T>(T[] rgv, int ivMin, ref int ivLim, Func<T, T, bool> cmp)
        {
            Contracts.AssertValue(rgv);
            Contracts.Assert(ivMin >= 0 && ivMin <= ivLim && ivLim <= rgv.Length);
            Contracts.AssertValue(cmp);

            if (ivLim - ivMin <= 1)
            {
                return;
            }

            var ivDst = ivMin + 1;
            for (var ivSrc = ivMin + 1; ivSrc < ivLim; ivSrc++)
            {
                var itemCur = rgv[ivSrc];
                if (!cmp(rgv[ivDst - 1], itemCur))
                {
                    if (ivDst < ivSrc)
                    {
                        rgv[ivDst] = itemCur;
                    }

                    ivDst++;
                }
            }

            ivLim = ivDst;
        }

        public static void Sort<T>(T[] rgv, int ivFirst, int ivLim, Comparison<T> cmp)
        {
            Contracts.AssertValue(rgv);
            Contracts.Assert(ivFirst >= 0 && ivFirst < ivLim && ivLim <= rgv.Length);
            Contracts.AssertValue(cmp);

            Array.Sort<T>(rgv, ivFirst, ivLim - ivFirst, Comparer<T>.Create(cmp));
        }
    }
}
