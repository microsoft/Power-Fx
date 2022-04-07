// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Utils
{
    internal static class Hashing
    {
        public static uint CombineHash(uint u1, uint u2)
        {
            return ((u1 << 7) | (u1 >> 25)) ^ u2;
        }

        public static int CombineHash(int n1, int n2)
        {
            return (int)CombineHash((uint)n1, (uint)n2);
        }

        public static uint CombineHash(uint u1, uint u2, uint u3, uint u4, uint u5)
        {
            return (((u1 << 7) | (u1 >> 25)) ^ u2) ^ (((u3 << 15) | (u3 >> 17)) ^ u4) ^ (u5 << 5);
        }

        public static int CombineHash(int n1, int n2, int n3, int n4)
        {
            return (int)CombineHash(CombineHash((uint)n1, (uint)n2), CombineHash((uint)n3, (uint)n4));
        }

        public static int CombineHash(int n1, int n2, int n3, int n4, int n5)
        {
            return (int)CombineHash((uint)n1, (uint)n2, (uint)n3, (uint)n4, (uint)n5);
        }

        public static uint CombineHash(uint u1, uint u2, uint u3, uint u4, uint u5, uint u6, uint u7)
        {
            return CombineHash(
                CombineHash(u1, u2, u3, u4, u5),
                CombineHash(u6, u7));
        }

        public static int CombineHash(int n1, int n2, int n3, int n4, int n5, int n6, int n7)
        {
            return (int)CombineHash((uint)n1, (uint)n2, (uint)n3, (uint)n4, (uint)n5, (uint)n6, (uint)n7);
        }

        /// <summary>
        /// Hash the characters in a string.
        /// </summary>
        /// <param name="str">The string instance to hash.</param>
        public static uint HashString(string str)
        {
            Contracts.AssertValue(str);

            uint hash1 = 5381;
            var hash2 = hash1;

            for (var ich = str.Length; ich > 0;)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[--ich];
                if (ich <= 0)
                {
                    break;
                }

                hash2 = ((hash2 << 5) + hash2) ^ str[--ich];
            }

            return HashUint(hash1 + (hash2 * 1566083941));
        }

        public static uint HashUint(uint u)
        {
            var uu = u * 0x7ff19519UL; // this number is prime.
            return GetLo(uu) + GetHi(uu);
        }

        public static int HashInt(int n)
        {
            return (int)HashUint((uint)n);
        }

        private static uint GetLo(ulong uu)
        {
            return (uint)uu;
        }

        private static uint GetHi(ulong uu)
        {
            return (uint)(uu >> 32);
        }
    }
}
