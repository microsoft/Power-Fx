// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.UtilityDataStructures
{
    /// <inheritdoc />
    /// <summary>
    /// Should be used for large collections, where reallocation is too expensive at the cost
    /// of a slower element access.
    /// Contains, Insert, Remove, RemoveAt and IndexOf are not support on this collection.
    /// </summary>
    internal sealed class ChunkedList<T> : IList<T>
    {
        private readonly List<List<T>> _chunks = new List<List<T>>();
        internal const int ChunkSize = 8192;
        private const int FirstChunkInitialSize = 4;
        private int _counter = 0;
        private int _version;
        private Stack<Action> onClear = new Stack<Action>();

        public T this[int index]
        {
            get
            {
                Contracts.Assert(index >= 0 && index < _counter);

                int chunk = index / ChunkSize;
                int idx = index % ChunkSize;
                return _chunks[chunk][idx];
            }
            set
            {
                Contracts.Assert(index >= 0 && index < _counter);

                int chunk = index / ChunkSize;
                int idx = index % ChunkSize;
                _chunks[chunk][idx] = value;
                _version++;
            }
        }

        public int Count => _counter;

        public bool IsReadOnly { get; } = false;

        public void Add(T item)
        {
            int chunk = _counter / ChunkSize;
            int idx = _counter % ChunkSize;

            // Allocate default capacity for first list to have dynamic allocation for small list.
            if (idx == 0)
                _chunks.Add(new List<T>((chunk == 0) ? FirstChunkInitialSize : ChunkSize));

            _chunks[chunk].Add(item);
            _counter++;
            _version++;
        }

        public void Clear()
        {
            foreach (var action in onClear)
            {
                action();
            }
            _chunks.Clear();
            _counter = 0;
            _version++;
        }

        public void TrackClear(Action action)
        {
            onClear.Push(action);
        }

        public void StopTrackingClear()
        {
            onClear.Pop();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        private ChunkedListEnumerator GetEnumeratorInternal()
        {
            return new ChunkedListEnumerator(this);
        }

        public int IndexOf(T item)
        {
            throw new NotSupportedException("IndexOf is not supported for ChunkedList");
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException("Insert is not supported for ChunkedList");
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException("CopyTo is not supported for ChunkedList");
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException("Removing elements from collection is not supported, as it skews element indices");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Removing elements from collection is not supported, as it skews element indices");
        }

        public bool Contains(T item)
        {
            throw new NotSupportedException("Contains in chunkedlist is not supported");
        }

        internal class ChunkedListEnumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private ChunkedList<T> list;
            private int index;
            private int version;
            private T current;

            internal ChunkedListEnumerator(ChunkedList<T> list)
            {
                this.list = list;
                index = 0;
                version = list._version;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {

                ChunkedList<T> localList = list;

                if (version == localList._version && ((uint)index < (uint)localList.Count))
                {
                    current = localList[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException("Chunked List is modified during enumeration");
                }

                index = list.Count + 1;
                current = default(T);
                return false;
            }

            public T Current
            {
                get
                {
                    return current;
                }
            }

            Object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list.Count + 1)
                    {
                        throw new IndexOutOfRangeException("ChunkedList out of range index accessed");
                    }
                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException("Chunked List is modified during enumeration");
                }

                index = 0;
                current = default(T);
            }
        }
    }
}
