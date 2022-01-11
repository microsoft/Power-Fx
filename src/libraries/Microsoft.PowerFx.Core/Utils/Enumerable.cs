// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#pragma warning disable 420

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.PowerFx.Core.Utils
{
    // System.Linq.Enumerator<T>.Empty() will allocate a new array iterator
    // to iterate a size 0 array each time. This is stateless and so does one
    // allocation per process, per type (on first use).
    internal sealed class EmptyEnumerator<T> : IEnumerable<T>, IEnumerator<T>
    {
        private static volatile EmptyEnumerator<T> _instance;

        private EmptyEnumerator()
        {
        }

        public static EmptyEnumerator<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    Interlocked.CompareExchange(ref _instance, new EmptyEnumerator<T>(), null);
                }

                return _instance;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        T IEnumerator<T>.Current => throw Contracts.Except();

        void IDisposable.Dispose()
        {
        }

        object System.Collections.IEnumerator.Current => throw Contracts.Except();

        bool System.Collections.IEnumerator.MoveNext()
        {
            return false;
        }

        void System.Collections.IEnumerator.Reset()
        {
        }
    }

    internal static class EnumerableUtils
    {
        // Helper functions to yield each provided item once.
        public static IEnumerable<T> Yield<T>()
        {
            return EmptyEnumerator<T>.Instance;
        }

        public static IEnumerable<T> Yield<T>(T t)
        {
            yield return t;
        }

        public static IEnumerable<T> Yield<T>(T t0, T t1)
        {
            yield return t0;
            yield return t1;
        }

        public static IEnumerable<T> Yield<T>(T t0, T t1, T t2)
        {
            yield return t0;
            yield return t1;
            yield return t2;
        }

        public static IEnumerable<T> Yield<T>(T t0, T t1, T t2, T t3)
        {
            yield return t0;
            yield return t1;
            yield return t2;
            yield return t3;
        }

        public static int FindIndex<T>(this IEnumerable<T> list, Predicate<T> predicate) => list.Select((item, index) => predicate(item) ? index : -1).Where(i => i != -1).DefaultIfEmpty(-1).First();

        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> list) => list ?? Yield<T>();
    }
}
