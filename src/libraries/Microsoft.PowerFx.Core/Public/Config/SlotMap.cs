// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Storage for slots. Dense assignment. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SlotMap<T>
    {
        private int _lowest;

        private readonly List<T> _items = new List<T>();

        public int Add(T item)
        {
            for (var i = _lowest; i < _items.Count; i++)
            {
                if (_items[i] == null)
                {
                    _lowest = i + 1;
                    _items[i] = item;
                    return i;
                }
            }

            _items.Add(item);
            return _items.Count - 1;
        }

        public bool TryGet(int index, out T value)
        {
            if (index < _items.Count)
            {
                value = _items[index];
                return value != null;
            }

            var t = default(T);
            value = t;
            return false;
        }

        // Update a previously added slot 
        public void Set(int index, T item)
        {
            _items[index] = item;
        }

        // Frees up a slot for reassignment. 
        // Gettings this slot should now be free. 
        public void Remove(int index)
        {
            if (index < _lowest)
            {
                _lowest = index;
            }

            var t = default(T);
            _items[index] = t;
        }
    }
}
