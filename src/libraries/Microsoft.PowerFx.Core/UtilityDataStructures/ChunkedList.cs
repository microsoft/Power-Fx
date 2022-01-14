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
        private int _version;
        private readonly Stack<Action> _onClear = new Stack<Action>();

        public T this[int index]
        {
            get
            {
                Contracts.Assert(index >= 0 && index < Count);

                var chunk = index / ChunkSize;
                var idx = index % ChunkSize;
                return _chunks[chunk][idx];
            }

            set
            {
                Contracts.Assert(index >= 0 && index < Count);

                var chunk = index / ChunkSize;
                var idx = index % ChunkSize;
                _chunks[chunk][idx] = value;
                _version++;
            }
        }

        public int Count { get; private set; } = 0;

        public bool IsReadOnly { get; } = false;

        public void Add(T item)
        {
            var chunk = Count / ChunkSize;
            var idx = Count % ChunkSize;

            // Allocate default capacity for first list to have dynamic allocation for small list.
            if (idx == 0)
            {
                _chunks.Add(new List<T>((chunk == 0) ? FirstChunkInitialSize : ChunkSize));
            }

            _chunks[chunk].Add(item);
            Count++;
            _version++;
        }

        public void Clear()
        {
            foreach (var action in _onClear)
            {
                action();
            }

            _chunks.Clear();
            Count = 0;
            _version++;
        }

        public void TrackClear(Action action)
        {
            _onClear.Push(action);
        }

        public void StopTrackingClear()
        {
            _onClear.Pop();
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
            private readonly ChunkedList<T> _list;
            private int _index;
            private readonly int _version;

            internal ChunkedListEnumerator(ChunkedList<T> list)
            {
                _list = list;
                _index = 0;
                _version = list._version;
                Current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var localList = _list;

                if (_version == localList._version && ((uint)_index < (uint)localList.Count))
                {
                    Current = localList[_index];
                    _index++;
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException("Chunked List is modified during enumeration");
                }

                _index = _list.Count + 1;
                Current = default;
                return false;
            }

            public T Current { get; private set; }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list.Count + 1)
                    {
                        throw new IndexOutOfRangeException("ChunkedList out of range index accessed");
                    }

                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException("Chunked List is modified during enumeration");
                }

                _index = 0;
                Current = default;
            }
        }
    }
}
