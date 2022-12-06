// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    // Extensions, extracted from Utils 
    internal static partial class Utils2
    {
        public static bool TestBit(this BigInteger value, int bitIndex)
        {
            Contracts.Assert(bitIndex >= 0);

            return !(value & (BigInteger.One << bitIndex)).IsZero;
        }
        public static void AddRange<T>(this HashSet<T> hashset, HashSet<T> hashset2)
        {
            foreach (var t in hashset2)
            {
                hashset.Add(t);
            }
        }
    }
}
