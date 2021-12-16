// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    // Implements a red-black tree mapping from string-valued key to DType.
    internal struct TypeTree : IEquatable<TypeTree>, IEnumerable<KeyValuePair<string, DType>>
    {
        private readonly RedBlackNode<DType> _root;
        private readonly Dictionary<RedBlackNode<DType>, int> _hashCodeCache;

        private TypeTree(RedBlackNode<DType> root)
        {
            _root = root;
            _hashCodeCache = new Dictionary<RedBlackNode<DType>, int>();
        }

        [Conditional("PARANOID_VALIDATION")]
        internal void AssertValid()
        {
#if PARANOID_VALIDATION
                if (_root != null)
                    _root.AssertValid();
#endif
        }

        public static TypeTree Create(IEnumerable<KeyValuePair<string, DType>> items)
        {
            Contracts.AssertValue(items);

#if DEBUG
            foreach (var item in items)
            {
                Contracts.AssertNonEmpty(item.Key);
                Contracts.Assert(item.Value.IsValid);
            }
#endif
            return new TypeTree(RedBlackNode<DType>.Create(items));
        }

        public bool IsEmpty { get { return _root == null; } }

        public int Count { get { return _root == null ? 0 : _root.Count; } }

        public bool Contains(string key)
        {
            Contracts.AssertValue(key);
            DType value;
            return TryGetValue(key, out value);
        }

        public bool TryGetValue(string key, out DType value)
        {
            Contracts.AssertValue(key);
            bool fRet = RedBlackNode<DType>.TryGetValue(_root, key, out value);
            value = value ?? DType.Invalid;
            Contracts.Assert(fRet == value.IsValid);
            return fRet;
        }

        public IEnumerable<KeyValuePair<string, DType>> GetPairs()
        {
            return RedBlackNode<DType>.GetPairs(_root);
        }

        public TypeTree SetItem(string name, DType type, bool skipCompare = false)
        {
            Contracts.AssertValue(name);
            Contracts.Assert(type.IsValid);
            return new TypeTree(RedBlackNode<DType>.SetItem(_root, name, type, skipCompare));
        }

        // Removes the specified name/field from the tree, and returns a new tree.
        // Sets fMissing if name cannot be found.
        public TypeTree RemoveItem(ref bool fMissing, string name)
        {
            Contracts.AssertValue(name);
            return new TypeTree(RedBlackNode<DType>.RemoveItem(ref fMissing, _root, name, _hashCodeCache));
        }

        // Removes the specified item from the tree, and returns a new tree.
        // If any item in rgname cannot be found, sets fAnyMissing to true.
        public TypeTree RemoveItems(ref bool fAnyMissing, params DName[] rgname)
        {
            Contracts.AssertNonEmpty(rgname);
            Contracts.AssertAllValid(rgname);

            RedBlackNode<DType> root = _root;
            foreach (string name in rgname)
            {
                Contracts.AssertNonEmpty(name);
                root = RedBlackNode<DType>.RemoveItem(ref fAnyMissing, root, name, _hashCodeCache);
            }

            return new TypeTree(root);
        }

        public static bool operator ==(TypeTree tree1, TypeTree tree2)
        {
            return RedBlackNode<DType>.Equals(tree1._root, tree2._root);
        }

        public static bool operator !=(TypeTree tree1, TypeTree tree2)
        {
            return !(tree1 == tree2);
        }

        public bool Equals(TypeTree other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TypeTree))
                return false;
            return this == (TypeTree)obj;
        }

        public override int GetHashCode()
        {
            int hash = 0x450C1E25;
            if (_root != null)
            {
                if (_hashCodeCache.ContainsKey(_root))
                    hash = _hashCodeCache[_root];
                else
                    hash = Hashing.CombineHash(hash, _root.GetHashCode());
            }

            return hash;
        }

        IEnumerator<KeyValuePair<string, DType>> IEnumerable<KeyValuePair<string, DType>>.GetEnumerator()
        {
            // The IEnumerable functionality of DType should only be used for debug scenarios.
            return GetPairs().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // The IEnumerable functionality of DType should only be used for debug scenarios.
            return GetPairs().GetEnumerator();
        }
    }
}