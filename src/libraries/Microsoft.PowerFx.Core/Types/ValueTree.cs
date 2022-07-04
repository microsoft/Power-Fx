// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    // Implements a red-black tree mapping from string-valued keys to equatable objects.
    // It is an assertable offense for an aggregate to have invalid children.
    internal struct ValueTree : IEquatable<ValueTree>
    {
        private readonly RedBlackNode<EquatableObject> _root;
        private readonly Dictionary<RedBlackNode<EquatableObject>, int> _hashCodeCache;

        private ValueTree(RedBlackNode<EquatableObject> root)
        {
            _root = root;
            _hashCodeCache = new Dictionary<RedBlackNode<EquatableObject>, int>();
        }

        [Conditional("PARANOID_VALIDATION")]
        [SuppressMessage("Performance", "CA1822: Mark members as static", Justification = "n/a")]
        internal void AssertValid()
        {
#if PARANOID_VALIDATION
                if (_root != null)
                    _root.AssertValid();
#endif
        }

        public static ValueTree Create(IEnumerable<KeyValuePair<string, EquatableObject>> items)
        {
            Contracts.AssertValue(items);

#if DEBUG
            foreach (var item in items)
            {
                Contracts.AssertNonEmpty(item.Key);
                Contracts.Assert(item.Value.IsValid);
            }
#endif
            return new ValueTree(RedBlackNode<EquatableObject>.Create(items));
        }

        public bool IsEmpty => _root == null;

        public int Count => _root == null ? 0 : _root.Count;

        public bool Contains(string key)
        {
            Contracts.AssertValue(key);
            return TryGetValue(key, out _);
        }

        public bool TryGetValue(string key, out EquatableObject value)
        {
            Contracts.AssertValue(key);
            var fRet = RedBlackNode<EquatableObject>.TryGetValue(_root, key, out value);
            Contracts.Assert(fRet == (value.Object != null));
            return fRet;
        }

        public IEnumerable<KeyValuePair<string, EquatableObject>> GetPairs()
        {
            return RedBlackNode<EquatableObject>.GetPairs(_root);
        }

        public ValueTree SetItem(string name, EquatableObject value)
        {
            Contracts.AssertValue(name);
            Contracts.AssertValue(value.Object);
            return new ValueTree(RedBlackNode<EquatableObject>.SetItem(_root, name, value));
        }

        // Removes the specified name+value from the tree, and returns a new tree.
        // Sets fMissing if name cannot be found.
        public ValueTree RemoveItem(ref bool fMissing, string name)
        {
            Contracts.AssertValue(name);
            return new ValueTree(RedBlackNode<EquatableObject>.RemoveItem(ref fMissing, _root, name, _hashCodeCache));
        }

        // Removes the specified item from the tree, and returns a new tree.
        // If any item in 'names' cannot be found, sets fAnyMissing to true.
        public ValueTree RemoveItems(ref bool fAnyMissing, params DName[] names)
        {
            Contracts.AssertNonEmpty(names);
            Contracts.AssertAllValid(names);

            var root = _root;
            foreach (string name in names)
            {
                Contracts.AssertNonEmpty(name);
                root = RedBlackNode<EquatableObject>.RemoveItem(ref fAnyMissing, root, name, _hashCodeCache);
            }

            return new ValueTree(root);
        }

        public static bool operator ==(ValueTree tree1, ValueTree tree2) => RedBlackNode<EquatableObject>.Equals(tree1._root, tree2._root);

        public static bool operator !=(ValueTree tree1, ValueTree tree2) => !(tree1 == tree2);

        public bool Equals(ValueTree other)
        {
            return this == other;
        }

        public override bool Equals(object other)
        {
            if (other is not ValueTree)
            {
                return false;
            }

            return this == (ValueTree)other;
        }

        public override int GetHashCode()
        {
            var hash = 0x79B70F13;
            if (_root != null)
            {
                if (_hashCodeCache.ContainsKey(_root))
                {
                    hash = _hashCodeCache[_root];
                }
                else
                {
                    hash = Hashing.CombineHash(hash, _root.GetHashCode());
                }
            }

            return hash;
        }
    }
}
