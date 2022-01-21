// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.UtilityDataStructures
{
    internal sealed class DedupedQueue<T>
    {
        private readonly Queue<T> _itemQueue;
        private readonly HashSet<T> _itemSet;

        public DedupedQueue()
        {
            _itemQueue = new Queue<T>();
            _itemSet = new HashSet<T>();
        }

        public DedupedQueue(IEnumerable<T> other)
        {
            _itemQueue = new Queue<T>();
            _itemSet = new HashSet<T>();

            foreach (var item in other)
            {
                Enqueue(item);
            }
        }

        public DedupedQueue(int size)
        {
            _itemQueue = new Queue<T>(size);
            _itemSet = new HashSet<T>();
        }

        public int Count => _itemQueue.Count;

        public void Enqueue(T item)
        {
            if (!_itemSet.Add(item))
            {
                return;
            }

            _itemQueue.Enqueue(item);
        }

        public T Dequeue()
        {
            // Will throw InvalidOperationException if empty
            var item = _itemQueue.Dequeue();
            _itemSet.Remove(item);

            return item;
        }

        public void Clear()
        {
            _itemSet.Clear();
            _itemQueue.Clear();
        }
    }
}
