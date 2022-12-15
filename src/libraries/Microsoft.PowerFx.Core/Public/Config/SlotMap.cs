// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Storage for slots. Dense assignment. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SlotMap<T>
    {
        // Lowest value of a null slot in _items. 
        // Saves a linear scan from idnex 0. 
        private int _lowest;

        // null items mean vacant slot. 
        private readonly List<T> _items = new List<T>();

        // Total non-null items. 
        private int _count = 0;

        public bool IsEmpty => _count == 0;

        public SlotMap()
        {
            // Either a class or nullable struct.
            Contracts.Assert(default(T) == null);
        }

        // Must be followed by a call to SetInitial. 
        public int Alloc()
        {
            for (var i = _lowest; i < _items.Count; i++)
            {
                if (_items[i] == null)
                {
                    // Found gap.
                    _lowest = i + 1;
                    Contracts.Assert(_items[i] == null);
                    _count++;
                    return i;
                }
            }

            // Fully packed, add to end. 
            _lowest = _items.Count + 1;

            _count++;
            _items.Add(default(T));
            return _items.Count - 1;
        }

        // Update a previously added slot 
        // Caller ensures this is a valid slot. 
        public void SetInitial(int index, T item)
        {
            Contracts.Assert(item != null);
            Contracts.Assert(_items[index] == null);

            _items[index] = item;
        }

        // Caller ensures this is a valid slot. 
        public bool TryGet(int index, out T value)
        {
            if (index < _items.Count)
            {
                value = _items[index];
                return value != null;
            }

            value = default(T);
            return false;
        }

        // Frees up a slot for reassignment. 
        // Gettings this slot should now be free. 
        public void Remove(int index)
        {
            if (index < _lowest)
            {
                _lowest = index;
            }

            // There should be some non-null value from SetInitial. 
            Contracts.Assert(_items[index] != null);

            _count--;
            Contracts.Assert(_count >= 0);
            _items[index] = default(T);
        }
    }
}
