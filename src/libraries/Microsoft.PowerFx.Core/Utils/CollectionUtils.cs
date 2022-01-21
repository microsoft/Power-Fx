// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static partial class CollectionUtils
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            var c = a;
            a = b;
            b = c;
        }

        public static T EnsureInstanceCreated<T>(ref T memberVariable, Func<T> creationMethod)
            where T : class
        {
            if (memberVariable == null)
            {
                Interlocked.CompareExchange(ref memberVariable, creationMethod(), null);
            }

            return memberVariable;
        }

        // Getting the size of a collection, when the collection may be null.

        public static int Size<T>(T[] rgv)
        {
            Contracts.AssertValueOrNull(rgv);
            return rgv == null ? 0 : rgv.Length;
        }

        public static int Size<T>(List<T> list)
        {
            Contracts.AssertValueOrNull(list);
            return list == null ? 0 : list.Count;
        }

        public static int Size<T>(IList<T> list)
        {
            Contracts.AssertValueOrNull(list);
            return list == null ? 0 : list.Count;
        }

        public static int Size<T>(Stack<T> stack)
        {
            Contracts.AssertValueOrNull(stack);
            return stack == null ? 0 : stack.Count;
        }

        public static int Size<TKey, TValue>(Dictionary<TKey, TValue> map)
        {
            Contracts.AssertValueOrNull(map);
            return map == null ? 0 : map.Count;
        }

        public static bool Contains<T>(ICollection<T> collection, T item)
        {
            if (collection == null)
            {
                return false;
            }

            return collection.Contains(item);
        }

        // Getting items from a collection when the collection may be null.

        public static bool TryGetValue<TK, TV>(Dictionary<TK, TV> map, TK key, out TV value)
        {
            Contracts.AssertValueOrNull(map);
            if (map == null)
            {
                value = default;
                return false;
            }

            return map.TryGetValue(key, out value);
        }

        // Adding items to a collection when the collection may be null.

        public static void Add<T>(ref List<T> list, T item)
        {
            Contracts.AssertValueOrNull(list);
            if (list == null)
            {
                list = new List<T>();
            }

            list.Add(item);
        }

        public static void Add<T>(ref List<T> listDst, List<T> listSrc)
        {
            Contracts.AssertValueOrNull(listSrc);
            Contracts.AssertValueOrNull(listDst);

            if (listSrc == null || listSrc.Count == 0)
            {
                return;
            }

            if (listDst == null)
            {
                listDst = new List<T>(listSrc.Count);
            }

            listDst.AddRange(listSrc);
        }

        public static void Add<T>(ref List<T> listDst, IEnumerable<T> src)
        {
            Contracts.AssertValueOrNull(listDst);
            Contracts.AssertValueOrNull(src);

            if (src == null)
            {
                return;
            }

            if (listDst == null)
            {
                listDst = new List<T>(src);
            }
            else
            {
                listDst.AddRange(src);
            }
        }

        public static void Add<TKey, TValue>(ref Dictionary<TKey, TValue> map, TKey key, TValue value, bool allowDupes = false)
        {
            Contracts.AssertValueOrNull(map);
            if (map == null)
            {
                map = new Dictionary<TKey, TValue>();
            }

            if (allowDupes)
            {
                map[key] = value;
            }
            else
            {
                map.Add(key, value);
            }
        }

        public static void Add<T>(ref HashSet<T> list, T item)
        {
            Contracts.AssertValueOrNull(list);
            if (list == null)
            {
                list = new HashSet<T>();
            }

            list.Add(item);
        }

        public static void Push<T>(ref Stack<T> stack, T item)
        {
            Contracts.AssertValueOrNull(stack);
            if (stack == null)
            {
                stack = new Stack<T>();
            }

            stack.Push(item);
        }

        public static T[] ToArray<T>(List<T> list)
        {
            Contracts.AssertValueOrNull(list);
            return list?.ToArray();
        }

        public static void Sort<T>(List<T> list)
        {
            Contracts.AssertValueOrNull(list);
            if (list != null)
            {
                list.Sort();
            }
        }

        public static TItem Append<TItem>(this List<TItem> list, TItem item)
        {
            Contracts.AssertValue(list);

            list.Add(item);
            return item;
        }
    }
}
