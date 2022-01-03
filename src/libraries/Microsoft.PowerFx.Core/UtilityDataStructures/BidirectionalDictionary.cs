// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerFx.Core.UtilityDataStructures
{
    internal class BidirectionalDictionary<TFirst, TSecond>: IEnumerable<KeyValuePair<TFirst,TSecond>>
    {
        private IDictionary<TFirst, TSecond> _firstToSecond;
        private IDictionary<TSecond, TFirst> _secondToFirst;

        public BidirectionalDictionary()
        {
            _firstToSecond = new Dictionary<TFirst, TSecond>();
            _secondToFirst = new Dictionary<TSecond, TFirst>();
        }

        public BidirectionalDictionary(IDictionary<TFirst, TSecond> input) : this()
        {
            foreach (var kvp in input)
                Add(kvp.Key, kvp.Value);
        }

        public bool Add(TFirst first, TSecond second)
        {
            if (_firstToSecond.ContainsKey(first) || _secondToFirst.ContainsKey(second))
                return false;
            _firstToSecond.Add(first, second);
            _secondToFirst.Add(second, first);

            return true;
        }

        public bool ContainsFirstKey(TFirst first)
        {
            return _firstToSecond.ContainsKey(first);
        }

        public bool ContainsSecondKey(TSecond second)
        {
            return _secondToFirst.ContainsKey(second);
        }

        public bool TryGetFromFirst(TFirst first, out TSecond second)
        {
            return _firstToSecond.TryGetValue(first, out second);
        }

        public bool TryGetFromSecond(TSecond second, out TFirst first)
        {
            return _secondToFirst.TryGetValue(second, out first);
        }

        public bool TryRemoveFromFirst(TFirst first)
        {
            TSecond second;
            if (_firstToSecond.TryGetValue(first, out second))
            {
                return _firstToSecond.Remove(first) && _secondToFirst.Remove(second);
            }
            return false;
        }

        public bool TryRemoveFromSecond(TSecond second)
        {
            TFirst first;
            if (_secondToFirst.TryGetValue(second, out first))
            {
                return _firstToSecond.Remove(first) && _secondToFirst.Remove(second);
            }
            return false;
        }


        public IEnumerator<KeyValuePair<TFirst, TSecond>> GetEnumerator()
        {
            return _firstToSecond.GetEnumerator();
        }

        public IDictionary<TFirst, TSecond> ToDictionary()
        {
            return _firstToSecond;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _firstToSecond.GetEnumerator();
        }

        public IEnumerable<TFirst> Keys { get { return _firstToSecond.Keys; } }

        public IEnumerable<TSecond> Values { get { return _firstToSecond.Values; } }

        public override bool Equals(object obj)
        {
            var other = obj as BidirectionalDictionary<TFirst, TSecond>;

            return other != null && other._firstToSecond.Count == _firstToSecond.Count && !other._firstToSecond.Except(_firstToSecond).Any();
        }

        public override int GetHashCode()
        {
            return _firstToSecond.GetHashCode();
        }

        public BidirectionalDictionary<TFirst, TSecond> Clone()
        {
            var result = new BidirectionalDictionary<TFirst, TSecond>();
            result._firstToSecond = new Dictionary<TFirst, TSecond>(_firstToSecond);
            result._secondToFirst = new Dictionary<TSecond, TFirst>(_secondToFirst);
            return result;
        }
    }
}
