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
    internal class StrictDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _backingDictionary = new Dictionary<TKey, TValue>();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _backingDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_backingDictionary).GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Contracts.Check(item.Value != null, "A strict dictionary can't accept a null value.");
            _backingDictionary.Add(item);
        }

        public void Clear()
        {
            _backingDictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _backingDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _backingDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _backingDictionary.Remove(item);
        }

        public int Count => _backingDictionary.Count;

        public bool IsReadOnly => _backingDictionary.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            Contracts.Check(value != null, "A strict dictionary can't accept a null value.");
            _backingDictionary.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _backingDictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return _backingDictionary.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _backingDictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => _backingDictionary[key];
            set
            {
                Contracts.Check(value != null, "A strict dictionary can't accept a null value.");
                _backingDictionary[key] = value;
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public ICollection<TKey> Keys => _backingDictionary.Keys;

        public ICollection<TValue> Values => _backingDictionary.Values;
    }
}
