// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.UtilityDataStructures
{
    /// <summary>
    ///  A dictionary which disallows null values. Throws a contract error if one
    ///  attempts to insert a null value.
    ///
    ///  Generally better to use this than a plain dictionary, because persistent nulls are extremely
    ///  difficult to debug. However, be careful about converting an existing dictionary to a strict
    ///  dictionary, since that could change behavior. This is safe in cases where a null in the
    ///  original dictionary would result in an almost certain crash anyway.
    /// </summary>
    internal class StrictDictionary<K, V> : IDictionary<K, V>, IReadOnlyDictionary<K, V>
    {
        private readonly IDictionary<K, V> _backingDictionary = new Dictionary<K, V>();

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return _backingDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_backingDictionary).GetEnumerator();
        }

        public void Add(KeyValuePair<K, V> item)
        {
            Contracts.Check(item.Value != null, "A strict dictionary can't accept a null value.");
            _backingDictionary.Add(item);
        }

        public void Clear()
        {
            _backingDictionary.Clear();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return _backingDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            _backingDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            return _backingDictionary.Remove(item);
        }

        public int Count => _backingDictionary.Count;

        public bool IsReadOnly => _backingDictionary.IsReadOnly;

        public void Add(K key, V value)
        {
            Contracts.Check(value != null, "A strict dictionary can't accept a null value.");
            _backingDictionary.Add(key, value);
        }

        public bool ContainsKey(K key)
        {
            return _backingDictionary.ContainsKey(key);
        }

        public bool Remove(K key)
        {
            return _backingDictionary.Remove(key);
        }

        public bool TryGetValue(K key, out V value)
        {
            return _backingDictionary.TryGetValue(key, out value);
        }

        public V this[K key]
        {
            get => _backingDictionary[key];
            set
            {
                Contracts.Check(value != null, "A strict dictionary can't accept a null value.");
                _backingDictionary[key] = value;
            }
        }

        IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;

        IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

        public ICollection<K> Keys => _backingDictionary.Keys;

        public ICollection<V> Values => _backingDictionary.Values;
    }
}