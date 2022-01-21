// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace Microsoft.PowerFx.Core.Utils
{
    // RedBlackNode<V> instances are immutable. An empty tree is represented by null.
    // Assumes a string "key", but general "value" of type "V".

    // NOTE: Ideally, RedBlackTree<V> would be a struct with a single field of type RedBlackTree<V>.Node.
    // However, then "struct DType" would need a field of type RedBlackTree<DType>, which causes
    // a TypeLoadException. This is a limitation of the CLR. The solution is that DType owns
    // a struct wrapper around RedBlackNode<DType>.

    // This partial contains the public API.
    internal abstract partial class RedBlackNode<T> : IEquatable<RedBlackNode<T>>
    {
        public static RedBlackNode<T> Create(IEnumerable<KeyValuePair<string, T>> items)
        {
            Contracts.AssertValue(items);

            var rgkvp = items.ToArray();
            var count = rgkvp.Length;
            switch (count)
            {
                case 0:
                    return null;
                case 1:
                    return new LeafNode(rgkvp[0]);
            }

            var rgikvp = new int[count];
            for (var iikvp = 0; iikvp < count; ++iikvp)
            {
                rgikvp[iikvp] = iikvp;
            }

            Sorting.Sort(
                rgikvp, 
                0, 
                count,
                (ikvp1, ikvp2) =>
                {
                    if (ikvp1 == ikvp2)
                    {
                        return 0;
                    }

                    var cmp = Compare(rgkvp[ikvp1].Key, rgkvp[ikvp2].Key);
                    if (cmp != 0)
                    {
                        return cmp;
                    }

                    // Secondary sort order is _descending_ by index, so when there are dups,
                    // we keep the _last_.
                    return ikvp2 - ikvp1;
                });

            Sorting.RemoveDupsFromSorted(
                rgikvp,
                0,
                ref count,
                (ikvp1, ikvp2) => rgkvp[ikvp1].Key == rgkvp[ikvp2].Key);

            return CreateFromArraySorted(rgkvp, rgikvp, 0, count);
        }

        public abstract int Count { get; }

        // Standard key/value lookup method.
        public static bool TryGetValue(RedBlackNode<T> root, string key, out T value)
        {
            Contracts.AssertValueOrNull(root);
            Contracts.AssertValue(key);

            if (root != null && TryFindNode(root, key, out root))
            {
                value = root._value;
                return true;
            }

            value = default;
            return false;
        }

        public static IEnumerable<KeyValuePair<string, T>> GetPairs(RedBlackNode<T> root)
        {
            Contracts.AssertValueOrNull(root);

            if (root == null)
            {
                yield break;
            }

            var stack = new Stack<RedBlackNode<T>>();
            var node = root;
LStart:
            if (node.Left != null)
            {
                stack.Push(node);
                node = node.Left;
                goto LStart;
            }

LYieldSelf:
            yield return new KeyValuePair<string, T>(node._key, node._value);

            if (node.Right != null)
            {
                node = node.Right;
                goto LStart;
            }

            if (stack.Count == 0)
            {
                yield break;
            }

            node = stack.Pop();
            goto LYieldSelf;
        }

        public static RedBlackNode<T> SetItem(RedBlackNode<T> root, string name, T value, bool skipCompare = false)
        {
            Contracts.AssertValueOrNull(root);
            Contracts.AssertValue(name);

            if (root == null)
            {
                return new LeafNode(name, value);
            }

            // Any red-red violations at the root don't matter because the root gets forced to black.
            AddItemCore(ref root, Color.Black, name, value, skipCompare);

            return root;
        }

        public static RedBlackNode<T> RemoveItem(ref bool fError, RedBlackNode<T> root, string name, Dictionary<RedBlackNode<T>, int> hashCodeCache)
        {
            Contracts.AssertValueOrNull(root);
            Contracts.AssertValue(name);

            if (RemoveItemCore(ref root, Color.Black, name, hashCodeCache) == RemoveCoreResult.ItemNotFound)
            {
                fError = true;
            }

            return root;
        }

        // Note that we DON'T implement == and !=, so those operators indicate
        // reference equality.
        public static bool Equals(RedBlackNode<T> root1, RedBlackNode<T> root2)
        {
            Contracts.AssertValueOrNull(root1);
            Contracts.AssertValueOrNull(root2);

            if (root1 == root2)
            {
                return true;
            }

            if (root1 == null || root2 == null)
            {
                return false;
            }

            if (root1.Count != root2.Count)
            {
                return false;
            }

            using (var ator1 = GetPairs(root1).GetEnumerator())
            using (var ator2 = GetPairs(root2).GetEnumerator())
            {
                while (ator1.MoveNext())
                {
                    var fTmp = ator2.MoveNext();
                    Contracts.Assert(fTmp);

                    if (ator1.Current.Key != ator2.Current.Key)
                    {
                        return false;
                    }

                    if (!ator1.Current.Value.Equals(ator2.Current.Value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool Equals(RedBlackNode<T> other)
        {
            Contracts.AssertValueOrNull(other);
            return Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            if (obj is not RedBlackNode<T> other)
            {
                return false;
            }

            return this == other;
        }

        public override int GetHashCode()
        {
            var hash = Hashing.CombineHash(0x34028ABC, Hashing.HashInt(Count));
            foreach (var kvp in GetPairs(this))
            {
                hash = Hashing.CombineHash(hash, kvp.Key.GetHashCode());
                hash = Hashing.CombineHash(hash, kvp.Value.GetHashCode());
            }

            return hash;
        }
    }

    // This partial contains the base implementation. In the ideal world, RedBlackTree<V> is a
    // struct and this partial is a nested abstract type named Node.
    internal abstract partial class RedBlackNode<T>
    {
        protected enum Color : byte
        {
            Red,
            Black
        }

        private readonly string _key;
        private readonly T _value;

        private RedBlackNode(string key, T value)
        {
            Contracts.AssertNonEmpty(key);
            _key = key;
            _value = value;
        }

        private RedBlackNode(KeyValuePair<string, T> kvp)
        {
            Contracts.AssertNonEmpty(kvp.Key);
            _key = kvp.Key;
            _value = kvp.Value;
        }

        protected abstract RedBlackNode<T> Left { get; }

        protected abstract RedBlackNode<T> Right { get; }

        protected abstract Color LeftColor { get; }

        protected abstract Color RightColor { get; }

        // Creates a new node with the same key and structure of the current node
        // but uses the new value.
        protected abstract RedBlackNode<T> CloneStructure(T value);

        [Conditional("PARANOID_VALIDATION")]
        internal void AssertValid()
        {
#if PARANOID_VALIDATION
            // Assume this is a root node and use black.
            // If we get a parent later, it will call AssertValidCore with our actual color.
            AssertValidCore(Color.Black);
#endif
        }

#if PARANOID_VALIDATION
        // Returns the black height of the node.
        private int AssertValidCore(Color colorSelf)
        {
            int leftBlackHeight = 0;
            int rightBlackHeight = 0;
            int count = 1;

            if (Left != null)
            {
                // Tree should be sorted
                Contracts.Assert(Compare(Left.Key, Key) < 0);
                leftBlackHeight = Left.AssertValidCore(LeftColor);
                count += Left.Count;
            }
            else
            {
                Contracts.Assert(LeftColor == Color.Black);
                leftBlackHeight = 1;
            }

            if (Right != null)
            {
                // Tree should be sorted
                Contracts.Assert(Compare(Right.Key, Key) > 0);
                rightBlackHeight = Right.AssertValidCore(RightColor);
                count += Right.Count;
            }
            else
            {
                Contracts.Assert(RightColor == Color.Black);
                rightBlackHeight = 1;
            }
            Contracts.Assert(Count == count);

            // Red nodes can't have red children.
            Contracts.Assert(colorSelf == Color.Black || (LeftColor == Color.Black && RightColor == Color.Black));
            // All paths to the root must have the same black height.
            Contracts.Assert(leftBlackHeight == rightBlackHeight);

            if (colorSelf == Color.Black)
                return leftBlackHeight + 1;

            return leftBlackHeight;
        }
#endif
    }

    // This partial contains the implementations. In the ideal world, RedBlackTree<V> is a
    // struct and these derive from an abstract nested type named Node.
    internal abstract partial class RedBlackNode<T>
    {
        private sealed class LeafNode : RedBlackNode<T>
        {
            public LeafNode(string key, T value)
                : base(key, value)
            {
            }

            public LeafNode(KeyValuePair<string, T> kvp)
                : base(kvp)
            {
            }

            public override int Count => 1;

            protected override RedBlackNode<T> Left => null;

            protected override RedBlackNode<T> Right => null;

            protected override Color LeftColor => Color.Black;

            protected override Color RightColor => Color.Black;

            protected override RedBlackNode<T> CloneStructure(T value)
            {
                return new LeafNode(_key, value);
            }
        }

        private sealed class InteriorNode : RedBlackNode<T>
        {
            private readonly int _count;
            private readonly RedBlackNode<T> _left;
            private readonly RedBlackNode<T> _right;
            private readonly Color _leftColor;
            private readonly Color _rightColor;

            public InteriorNode(string key, T value, RedBlackNode<T> left, RedBlackNode<T> right, Color leftColor, Color rightColor)
                : base(key, value)
            {
                Contracts.Assert(left != null || right != null);
                Contracts.Assert(left != null || leftColor == Color.Black);
                Contracts.Assert(right != null || rightColor == Color.Black);

                _count = 1;
                if (left != null)
                {
                    _left = left;
                    _count += _left.Count;
                }

                if (right != null)
                {
                    _right = right;
                    _count += _right.Count;
                }

                _leftColor = leftColor;
                _rightColor = rightColor;

#if PARANOID_VALIDATION
                AssertValid();
#endif
            }

            public InteriorNode(KeyValuePair<string, T> kvp, RedBlackNode<T> left, RedBlackNode<T> right, Color leftColor, Color rightColor)
                : this(kvp.Key, kvp.Value, left, right, leftColor, rightColor)
            {
            }

            public override int Count => _count;

            protected override RedBlackNode<T> Left => _left;

            protected override RedBlackNode<T> Right => _right;

            protected override Color LeftColor => _leftColor;

            protected override Color RightColor => _rightColor;

            protected override RedBlackNode<T> CloneStructure(T value)
            {
                return new InteriorNode(_key, value, _left, _right, _leftColor, _rightColor);
            }
        }

        // Creates a new node in the tree given that left and/or right may be null.
        private static RedBlackNode<T> Create(string key, T value, RedBlackNode<T> left, RedBlackNode<T> right, Color leftColor, Color rightColor)
        {
            Contracts.AssertNonEmpty(key);
            Contracts.AssertValueOrNull(left);
            Contracts.AssertValueOrNull(right);

            if (left != null || right != null)
            {
                return new InteriorNode(key, value, left, right, leftColor, rightColor);
            }

            Contracts.Assert(leftColor == Color.Black && rightColor == Color.Black);
            return new LeafNode(key, value);
        }
    }

    // This partial contains the core tree manipulation methods.
    internal abstract partial class RedBlackNode<T>
    {
        // Red Black tree rules:
        // 1) A node is either red or black. 
        // 2) The root is black.
        // 3) All null nodes are black.
        // 4) Both children of red nodes are black.
        // 5) Every path from a node to any of its leaves contains the same number of black nodes (its 'black height').
        // References:
        //   Introduction to Algorithms -  Cormen, Leiserson, Rivest 
        //   Purely Functional Data Structures - Okasaki

        // Enum for private Add API that needs to maintain more state about the tree
        private enum AddCoreResult
        {
            // The item was already in the tree, no change.
            ItemPresent,

            // The key was already in the tree, and was updated with a new value.
            // A new tree is being built, but no colors need to change.
            ItemUpdated,

            // The key was not in the tree.
            // A new tree is being built, but no colors need to change.
            ItemAdded,

            // The key was not in the tree.
            // A new tree is being built, the returned node should be red.
            NewNodeIsRed,

            // The key was not in the tree.
            // A new tree is being built, the returned node is red
            // and its left child is also red, the caller needs to fix this violation
            DoubleRedLeftChild,

            // The key was not in the tree.
            // A new tree is being built, the returned node is red
            // and its right child is also red, the caller needs to fix this violation
            DoubleRedRightChild
        }

        // Enum for private Remove API that needs to maintain more state about the tree
        private enum RemoveCoreResult
        {
            // The key was not in the tree, no change.
            ItemNotFound,

            // The key was in the tree.
            // A new tree is being built, but no colors need to change.
            ItemRemoved,

            // The key was in the tree.
            // A new tree is being built, the returned node should be black.
            NewNodeIsBlack,

            // The key was in the tree.
            // A new tree is being built, the returned node is 'double black'
            // which is a violation that needs to be fixed by the caller.
            NewNodeIsDoubleBlack,
        }

        public static int Compare(string str0, string str1)
        {
            Contracts.AssertValue(str0);
            Contracts.AssertValue(str1);
            return string.CompareOrdinal(str0, str1);
        }

        private static RedBlackNode<T> CreateFromArraySorted(KeyValuePair<string, T>[] rgkvp, int[] rgikvp, int iikvpMin, int iikvpLim)
        {
            Contracts.AssertValue(rgkvp);
            Contracts.AssertValue(rgikvp);
            Contracts.Assert(iikvpMin >= 0 && iikvpMin <= rgkvp.Length && iikvpLim <= rgkvp.Length && iikvpMin <= iikvpLim);
            Contracts.Assert(iikvpMin <= rgikvp.Length && iikvpLim <= rgikvp.Length);

            var nodeCount = iikvpLim - iikvpMin;
            var half = 0;
            RedBlackNode<T> left = null;
            RedBlackNode<T> right = null;
            var leftColor = Color.Black;

            // What is this 'magic' test "if (((nodeCount + 2) & (nodeCount + 1)) == 0)" below?
            // The max height of an RB tree is 2*Floor(Log_2(N + 1)) (every other node is red)
            // This means the min height of an RB tree is Floor(Log_2(N + 1)) (maximum number of black nodes)
            // If N is odd, no problem:
            // Left subtree gets (N-1)/2 nodes, root gets 1 node, right subtree gets (N-1)/2 nodes (all black)
            // If N is even:
            // Left subtree gets N/2 nodes, root gets 1 node, right subtree gets (N/2) - 1 nodes
            // (Since we are going for min height)
            // For the tree to be valid, black heights must be equal. Since we are going for all black nodes
            // min height == black height
            // LNC = Left Node Count (N/2); RNC = Right Node Count ((N/2) - 1);
            // Floor(Log_2(LNC + 1)) == Floor(Log_2(RNC + 1)) must be true for a valid tree
            // Floor(Log_2(LNC + 1)) == Floor(Log_2(LNC)) // LNC == RNC + 1
            // What values of LNC make this formula invalid? 
            // When LNC is 1 less than a power of 2 (1, 3, 7, 15, 31, 63 etc).
            // In this case, the left subtree has a black height that is 1 greater than the right.
            // For this to be a valid tree, that means the root of the left subtree must be red.
            // So, what does that mean for N when LNC is 1 less than a power of 2?
            // If 'magic' LNC values are 1, 3, 7, 15, 31, 63 etc and LNC is N/2,
            // that means 'magic' N values are 2, 6, 14, 30, 62, 126 etc
            // So....when building a tree of N nodes, if N is 2 less than a power of 2, 
            // the root of the left subtree must be red.
            // if N is 2 less than a power of 2, then (N+2)&(N+1) must be 0
            switch (nodeCount)
            {
                case 0:
                    return null;
                case 1:
                    return new LeafNode(rgkvp[rgikvp[iikvpMin]]);
                case 2:
                    left = new LeafNode(rgkvp[rgikvp[iikvpMin]]);
                    return new InteriorNode(rgkvp[rgikvp[iikvpMin + 1]], left, null, Color.Red, Color.Black);
                case 3:
                    left = new LeafNode(rgkvp[rgikvp[iikvpMin]]);
                    right = new LeafNode(rgkvp[rgikvp[iikvpMin + 2]]);
                    return new InteriorNode(rgkvp[rgikvp[iikvpMin + 1]], left, right, Color.Black, Color.Black);
                default:
                    if ((nodeCount & 1) == 1)
                    {
                        half = nodeCount / 2;
                    }
                    else
                    {
                        half = (nodeCount + 1) / 2;
                        if (((nodeCount + 2) & (nodeCount + 1)) == 0)
                        {
                            leftColor = Color.Red;
                        }
                    }

                    left = CreateFromArraySorted(rgkvp, rgikvp, iikvpMin, iikvpMin + half);
                    right = CreateFromArraySorted(rgkvp, rgikvp, iikvpMin + half + 1, iikvpLim);
                    return new InteriorNode(rgkvp[rgikvp[iikvpMin + half]], left, right, leftColor, Color.Black);
            }
        }

        private static bool TryFindNode(RedBlackNode<T> root, string key, out RedBlackNode<T> node)
        {
            Contracts.AssertValueOrNull(root);
            Contracts.AssertValue(key);

            node = root;
            while (node != null)
            {
                var cmp = Compare(key, node._key);
                if (cmp == 0)
                {
                    return true;
                }

                node = (cmp < 0) ? node.Left : node.Right;
            }

            return false;
        }

        private static AddCoreResult AddItemCore(ref RedBlackNode<T> root, Color rootColor, string key, T value, bool skipCompare = false)
        {
            Contracts.AssertValue(root);
            Contracts.AssertValue(key);

            var cmp = Compare(key, root._key);
            if (cmp == 0)
            {
                if (!skipCompare && value.Equals(root._value))
                {
                    return AddCoreResult.ItemPresent; // Don't build a new tree.
                }

                root = root.CloneStructure(value);
                return AddCoreResult.ItemUpdated; // Build a new tree, don't change the count.
            }

            RedBlackNode<T> left;
            RedBlackNode<T> right;
            AddCoreResult result;
            if (cmp < 0)
            {
                // Add to the left side of the tree.
                right = root.Right;
                if (root.Left == null)
                {
                    // Make a new left child, set its color to red.
                    root = new InteriorNode(root._key, root._value, new LeafNode(key, value), right, Color.Red, root.RightColor);

                    // Check for double red violation and warn the caller.
                    return (rootColor == Color.Red) ? AddCoreResult.DoubleRedLeftChild : AddCoreResult.ItemAdded;
                }

                // DIAGRAM LEGEND:
                // [] - black node
                // numbers - color doesn't matter, may be null in some cases

                RedBlackNode<T> newLeft;
                RedBlackNode<T> newRight;
                left = root.Left;
                result = AddItemCore(ref left, root.LeftColor, key, value, skipCompare: skipCompare);
                switch (result)
                {
                    case AddCoreResult.ItemUpdated:
                    case AddCoreResult.ItemAdded:
                        root = new InteriorNode(root._key, root._value, left, right, root.LeftColor, root.RightColor);
                        return result;
                    case AddCoreResult.NewNodeIsRed:
                        root = new InteriorNode(root._key, root._value, left, right, Color.Red, root.RightColor);

                        // Check for double red violation and warn the caller.
                        return (rootColor == Color.Red) ? AddCoreResult.DoubleRedLeftChild : AddCoreResult.ItemAdded;
                    case AddCoreResult.DoubleRedLeftChild:
                        //     [Z]     
                        //     / \
                        //    Y   4           Y
                        //   /\              /  \
                        //  X  3     =>   [X]   [Z]
                        //  /\            /  \  /  \
                        // 1 2           1   2 3   4
                        Contracts.Assert(rootColor == Color.Black);

                        newLeft = left.Left;
                        newRight = Create(root._key, root._value, left.Right, root.Right, left.RightColor, root.RightColor);
                        root = new InteriorNode(left._key, left._value, newLeft, newRight, Color.Black, Color.Black);
                        return AddCoreResult.NewNodeIsRed;
                    case AddCoreResult.DoubleRedRightChild:
                        //    [Z]     
                        //    / \
                        //   X   4           Y
                        //  /\              /  \
                        // 1  Y     =>   [X]   [Z]
                        //    /\         /  \  /  \
                        //   2 3        1   2 3   4
                        Contracts.Assert(rootColor == Color.Black);

                        var leftRight = left.Right;
                        Contracts.AssertValue(leftRight);
                        newLeft = Create(left._key, left._value, left.Left, leftRight.Left, left.LeftColor, leftRight.LeftColor);
                        newRight = Create(root._key, root._value, leftRight.Right, right, leftRight.RightColor, root.RightColor);
                        root = new InteriorNode(leftRight._key, leftRight._value, newLeft, newRight, Color.Black, Color.Black);
                        return AddCoreResult.NewNodeIsRed;
                    default:
                        Contracts.Assert(result == AddCoreResult.ItemPresent);
                        return AddCoreResult.ItemPresent;
                }
            }
            else
            {
                // Add to the right side of the tree.
                left = root.Left;
                if (root.Right == null)
                {
                    // Make a new right child, set its color to red.
                    root = new InteriorNode(root._key, root._value, left, new LeafNode(key, value), root.LeftColor, Color.Red);

                    // Check for double red violation and warn the caller.
                    return (rootColor == Color.Red) ? AddCoreResult.DoubleRedRightChild : AddCoreResult.ItemAdded;
                }

                RedBlackNode<T> newLeft;
                RedBlackNode<T> newRight;
                right = root.Right;
                result = AddItemCore(ref right, root.RightColor, key, value, skipCompare: skipCompare);
                switch (result)
                {
                    case AddCoreResult.ItemUpdated:
                    case AddCoreResult.ItemAdded:
                        root = new InteriorNode(root._key, root._value, left, right, root.LeftColor, root.RightColor);
                        return result;
                    case AddCoreResult.NewNodeIsRed:
                        root = new InteriorNode(root._key, root._value, left, right, root.LeftColor, Color.Red);

                        // Check for double red violation and warn the caller.
                        return (rootColor == Color.Red) ? AddCoreResult.DoubleRedRightChild : AddCoreResult.ItemAdded;
                    case AddCoreResult.DoubleRedRightChild:
                        //    [X]        
                        //    / \
                        //   1   Y              Y
                        //      / \           /   \
                        //     2  Z    =>   [X]   [Z]
                        //        /\        / \   / \
                        //       3 4       1   2 3   4
                        Contracts.Assert(rootColor == Color.Black);

                        newLeft = Create(root._key, root._value, root.Left, right.Left, root.LeftColor, right.LeftColor);
                        newRight = right.Right;
                        root = new InteriorNode(right._key, right._value, newLeft, newRight, Color.Black, Color.Black);
                        return AddCoreResult.NewNodeIsRed;
                    case AddCoreResult.DoubleRedLeftChild:
                        //    [X]        
                        //    / \
                        //   1   Z              Y
                        //      / \           /   \
                        //     Y  4    =>   [X]   [Z]
                        //     /\           / \   / \
                        //     2 3         1   2 3   4
                        Contracts.Assert(rootColor == Color.Black);

                        var rightLeft = right.Left;
                        Contracts.AssertValue(rightLeft);
                        newLeft = Create(root._key, root._value, root.Left, rightLeft.Left, root.LeftColor, rightLeft.LeftColor);
                        newRight = Create(right._key, right._value, rightLeft.Right, right.Right, rightLeft.RightColor, right.RightColor);
                        root = new InteriorNode(rightLeft._key, rightLeft._value, newLeft, newRight, Color.Black, Color.Black);
                        return AddCoreResult.NewNodeIsRed;
                    default:
                        Contracts.Assert(result == AddCoreResult.ItemPresent);
                        return AddCoreResult.ItemPresent;
                }
            }
        }

        private static RemoveCoreResult RemoveItemCore(ref RedBlackNode<T> root, Color rootColor, string key, Dictionary<RedBlackNode<T>, int> hashCodeCache)
        {
            Contracts.AssertValueOrNull(root);
            Contracts.AssertValue(key);

            if (root == null)
            {
                return RemoveCoreResult.ItemNotFound;
            }

            var cmp = Compare(key, root._key);
            if (cmp == 0)
            {
                // We found it. Update count.
                if (root.Left == null)
                {
                    var rightColor = root.RightColor;
                    root = root.Right;

                    // If the removed node or its only child is red, just color it black
                    // and we are done, no black height violations.
                    if (rootColor == Color.Red || rightColor == Color.Red)
                    {
                        return RemoveCoreResult.NewNodeIsBlack;
                    }

                    // Mark the new node as a fake 'double black' to maintain the black height,
                    // callers will do fixup.
                    return RemoveCoreResult.NewNodeIsDoubleBlack;
                }

                if (root.Right == null)
                {
                    var leftColor = root.LeftColor;
                    root = root.Left;

                    // If the removed node or its only child is red, just color it black
                    // and we are done, no black height violations.
                    if (rootColor == Color.Red || leftColor == Color.Red)
                    {
                        return RemoveCoreResult.NewNodeIsBlack;
                    }

                    // Mark the new node as a fake 'double black' to maintain the black height,
                    // callers will do fixup.
                    return RemoveCoreResult.NewNodeIsDoubleBlack;
                }

                var newRight = root.Right;

                // Find the left-most child of the right node and remove it, then use its key/value
                // as the new key/value for this node, thus 'removing' the target node.
                var result = RemoveLeftMost(ref newRight, root.RightColor, hashCodeCache, out var leftMost);
                Contracts.Assert(result != RemoveCoreResult.ItemNotFound);

                switch (result)
                {
                    case RemoveCoreResult.ItemRemoved:
                        root = new InteriorNode(leftMost._key, leftMost._value, root.Left, newRight, root.LeftColor, root.RightColor);
                        return RemoveCoreResult.ItemRemoved;
                    case RemoveCoreResult.NewNodeIsBlack:
                        root = new InteriorNode(leftMost._key, leftMost._value, root.Left, newRight, root.LeftColor, Color.Black);
                        return RemoveCoreResult.ItemRemoved;
                    default:
                        Contracts.Assert(result == RemoveCoreResult.NewNodeIsDoubleBlack);
                        return RemoveFixupRight(ref root, leftMost, rootColor, newRight);
                }
            }

            if (cmp < 0)
            {
                // Check the left side of the tree.
                var left = root.Left;
                var result = RemoveItemCore(ref left, root.LeftColor, key, hashCodeCache);
                switch (result)
                {
                    case RemoveCoreResult.ItemRemoved:
                        root = Create(root._key, root._value, left, root.Right, root.LeftColor, root.RightColor);
                        return RemoveCoreResult.ItemRemoved;
                    case RemoveCoreResult.NewNodeIsBlack:
                        root = Create(root._key, root._value, left, root.Right, Color.Black, root.RightColor);
                        return RemoveCoreResult.ItemRemoved;
                    case RemoveCoreResult.NewNodeIsDoubleBlack:
                        return RemoveFixupLeft(ref root, rootColor, left);
                    default:
                        Contracts.Assert(result == RemoveCoreResult.ItemNotFound);
                        return RemoveCoreResult.ItemNotFound;
                }
            }
            else
            {
                // Check the right side of the tree.
                var right = root.Right;
                var result = RemoveItemCore(ref right, root.RightColor, key, hashCodeCache);
                switch (result)
                {
                    case RemoveCoreResult.ItemRemoved:
                        root = Create(root._key, root._value, root.Left, right, root.LeftColor, root.RightColor);
                        return RemoveCoreResult.ItemRemoved;
                    case RemoveCoreResult.NewNodeIsBlack:
                        root = Create(root._key, root._value, root.Left, right, root.LeftColor, Color.Black);
                        return RemoveCoreResult.ItemRemoved;
                    case RemoveCoreResult.NewNodeIsDoubleBlack:
                        return RemoveFixupRight(ref root, root, rootColor, right);
                    default:
                        Contracts.Assert(result == RemoveCoreResult.ItemNotFound);
                        return RemoveCoreResult.ItemNotFound;
                }
            }
        }

        private static RemoveCoreResult RemoveLeftMost(ref RedBlackNode<T> root, Color rootColor, Dictionary<RedBlackNode<T>, int> hashCodeCache, out RedBlackNode<T> removedNode)
        {
            Contracts.AssertValue(root);

            if (root.Left == null)
            {
                // We've reached the left most node, remove it and clear its hash code from the cache
                var rightColor = root.RightColor;
                removedNode = root;
                root = root.Right;

                hashCodeCache.Remove(removedNode);

                // If the removed node or its only child is red, just color it black
                // and we are done, no black height violations.
                if (rootColor == Color.Red || rightColor == Color.Red)
                {
                    return RemoveCoreResult.NewNodeIsBlack;
                }

                // Mark the new node as a fake 'double black' to maintain the black height,
                // callers will do fixup.
                return RemoveCoreResult.NewNodeIsDoubleBlack;
            }

            var left = root.Left;
            var result = RemoveLeftMost(ref left, root.LeftColor, hashCodeCache, out removedNode);
            Contracts.Assert(result != RemoveCoreResult.ItemNotFound);

            switch (result)
            {
                case RemoveCoreResult.ItemRemoved:
                    root = Create(root._key, root._value, left, root.Right, root.LeftColor, root.RightColor);
                    return RemoveCoreResult.ItemRemoved;
                case RemoveCoreResult.NewNodeIsBlack:
                    root = Create(root._key, root._value, left, root.Right, Color.Black, root.RightColor);
                    return RemoveCoreResult.ItemRemoved;
                default:
                    Contracts.Assert(result == RemoveCoreResult.NewNodeIsDoubleBlack);
                    return RemoveFixupLeft(ref root, rootColor, left);
            }
        }

        // Performs red-black tree fixup for the root whose new left child (left)
        // is a 'double black' node.
        private static RemoveCoreResult RemoveFixupLeft(ref RedBlackNode<T> root, Color rootColor, RedBlackNode<T> left)
        {
            Contracts.AssertValue(root);
            Contracts.AssertValue(root.Right);
            Contracts.AssertValueOrNull(left);

            var rootRight = root.Right;

            // There are 4 main cases, order matters to avoid introducing red-red violations.
            // DIAGRAM LEGEND:
            // [] - black node
            // [[]] - double black node
            // {} - red or black node
            // numbers - color doesn't matter, may be null in some cases

            // CASE 1: The 'double black' node's furthest nephew is red.
            // Perform a left rotation and recolor. Tree is valid after this step.
            if (rootRight.RightColor == Color.Red)
            {
                //        {B}                              {D}           
                //     /       \                        /       \
                // [[A]]        [D]                 [B]         [E]
                //  / \         / \      =>        /   \        / \
                // 1   2     {C}   E            [A]     {C}    5   6      
                //           / \  / \           / \     / \
                //          3   4 5 6          1   2   3   4
                Contracts.Assert(root.RightColor == Color.Black);

                var newLeft = Create(root._key, root._value, left, rootRight.Left, Color.Black, rootRight.LeftColor);
                var newRight = rootRight.Right;
                root = new InteriorNode(rootRight._key, rootRight._value, newLeft, newRight, Color.Black, Color.Black);
                return RemoveCoreResult.ItemRemoved;
            }

            // CASE 2: The 'double black' node's nearest nephew is red.
            // Performing a right rotation and recolor on the root's right child transforms this to case 1.
            // Then performing a left rotation and recolor on the root makes a valid tree.
            // Both rotations and recolorings are performed in one step to save on node creation.
            if (rootRight.LeftColor == Color.Red)
            {
                //        {B}                    {B}                        {C}
                //     /       \               /     \                    /    \
                // [[A]]        [D]        [[A]]      [C]              [B]      [D]
                //  / \         / \    =>   /  \      /  \             / \      / \
                // 1   2      C   [E]      1   2    3     D     =>  [A]   3    4   [E]
                //           / \  / \                    / \        / \            / \
                //          3   4 5 6                   4  [E]     1   2          5   6
                //                                         / \
                //                                        5   6
                Contracts.Assert(root.RightColor == Color.Black);
                Contracts.Assert(rootRight.RightColor == Color.Black);

                var rootRightLeft = rootRight.Left;
                Contracts.AssertValue(rootRightLeft);
                var newLeft = Create(root._key, root._value, left, rootRightLeft.Left, Color.Black, rootRightLeft.LeftColor);
                var newRight = Create(rootRight._key, rootRight._value, rootRightLeft.Right, rootRight.Right, rootRightLeft.RightColor, Color.Black);
                root = new InteriorNode(rootRightLeft._key, rootRightLeft._value, newLeft, newRight, Color.Black, Color.Black);
                return RemoveCoreResult.ItemRemoved;
            }

            // CASE 3: The 'double black' node's sibling is red.
            // Performing a left rotation and recolor on the root transforms this into EITHER case 1, 2 OR 4, 
            // depending on the values of root.Right.Left.LeftColor and root.Right.Left.RightColor
            // All rotations and recolorings are performed in one step to save on node creation.
            // In all cases, the tree is valid after this step.
            if (root.RightColor == Color.Red)
            {
                Contracts.Assert(rootColor == Color.Black);
                Contracts.Assert(rootRight.LeftColor == Color.Black);
                Contracts.Assert(rootRight.RightColor == Color.Black);

                RedBlackNode<T> newLeft;
                RedBlackNode<T> newRight;
                var rootRightLeft = rootRight.Left;
                Contracts.AssertValue(rootRightLeft);

                // Case 3 -> Case 1
                if (rootRightLeft.RightColor == Color.Red)
                {
                    //        [B]                           [D]                            [D]
                    //     /       \                     /       \                       /     \
                    // [[A]]          D                B          [E]                  C        [E]
                    //  / \         /   \    =>     /     \       / \   =>          /     \     /  \
                    // 1   2     [C]    [E]      [[A]]   [C]     6   7           [B]     [CZ]  6   7
                    //           / \    / \       /\     /  \                   /  \     / \
                    //          3   CZ  6 7      1  2   3   CZ                [A]   3  4    5
                    //              / \                    /  \              /  \
                    //             4   5                  4    5            1    2
                    var newLeftLeft = Create(root._key, root._value, left, rootRightLeft.Left, Color.Black, rootRightLeft.LeftColor);
                    var newLeftRight = rootRightLeft.Right;
                    newLeft = new InteriorNode(rootRightLeft._key, rootRightLeft._value, newLeftLeft, newLeftRight, Color.Black, Color.Black);
                    newRight = rootRight.Right;
                    root = new InteriorNode(rootRight._key, rootRight._value, newLeft, newRight, Color.Red, Color.Black);
                    return RemoveCoreResult.ItemRemoved;
                }

                // Case 3 -> Case 2 -> Case 1
                if (rootRightLeft.LeftColor == Color.Red)
                {
                    //        [B]                           [D]        <See Case 2                    [D]
                    //     /       \                     /       \      for 2 stage rotation       /        \
                    // [[A]]          D                B          [E]   using B as root>         CA          [E]
                    //  / \         /   \    =>     /     \       / \        =>                /    \       /  \
                    // 1   2     [C]    [E]      [[A]]   [C]     7   8                      [B]       [C]  7    8
                    //           / \    / \       /\     /  \                              /  \     /    \
                    //          CA [CZ] 7  8     1  2   CA [CZ]                          [A]   3    4   [CZ]
                    //         / \  / \                 /\  /\                          /  \            /  \
                    //        3  4 5   6               3  4 5 6                        1    2          5    6
                    var rootRightLeftLeft = rootRightLeft.Left;
                    Contracts.AssertValue(rootRightLeftLeft);
                    var newLeftLeft = Create(root._key, root._value, left, rootRightLeftLeft.Left, Color.Black, rootRightLeftLeft.LeftColor);
                    var newLeftRight = Create(rootRightLeft._key, rootRightLeft._value, rootRightLeftLeft.Right, rootRightLeft.Right, rootRightLeftLeft.RightColor, Color.Black);
                    newLeft = new InteriorNode(rootRightLeftLeft._key, rootRightLeftLeft._value, newLeftLeft, newLeftRight, Color.Black, Color.Black);
                    newRight = rootRight.Right;
                    root = new InteriorNode(rootRight._key, rootRight._value, newLeft, newRight, Color.Red, Color.Black);
                    return RemoveCoreResult.ItemRemoved;
                }

                // Case 3 -> Case 4
                //        [B]                           [D]                        [D]       
                //     /       \                     /       \                  /       \    
                // [[A]]          D                B          [E]            [B]         [E] 
                //  / \         /   \    =>     /     \       / \   =>     /     \       / \ 
                // 1   2     [C]    [E]      [[A]]   [C]     3   4       [A]     C      3   4
                //           / \    / \       /\     /  \                /\     /  \         
                //         [CA][CZ] 3 4      1  2   [CA][CZ]            1  2   [CA][CZ]      
                newLeft = Create(root._key, root._value, left, rootRightLeft, Color.Black, Color.Red);
                newRight = rootRight.Right;
                root = new InteriorNode(rootRight._key, rootRight._value, newLeft, newRight, Color.Black, Color.Black);
                return RemoveCoreResult.NewNodeIsBlack;
            }

            // CASE 4: The 'double black' node's parent is red.
            // Move the parent's red to the sibling and color the parent black and the tree becomes valid.
            if (rootColor == Color.Red)
            {
                Contracts.Assert(root.RightColor == Color.Black);
                root = new InteriorNode(root._key, root._value, left, rootRight, Color.Black, Color.Red);
                return RemoveCoreResult.NewNodeIsBlack;
            }

            // CASE 5: All of the 'double black' node's relations are black (parent, sibling, both nephews)
            // Color the sibling red and make the root the new 'double black' node.
            Contracts.Assert(rootColor == Color.Black);
            Contracts.Assert(root.RightColor == Color.Black);
            Contracts.Assert(rootRight.LeftColor == Color.Black);
            Contracts.Assert(rootRight.RightColor == Color.Black);
            root = new InteriorNode(root._key, root._value, left, rootRight, Color.Black, Color.Red);
            return RemoveCoreResult.NewNodeIsDoubleBlack;
        }

        // Performs red-black tree fixup for the root whose new right child (right)
        // is a 'double black' node.
        // Takes two 'root' parameters (root, rootData) instead of one like RemoveFixupLeft
        // because the data for the new root might be coming from the result of a RemoveLeftMost
        private static RemoveCoreResult RemoveFixupRight(ref RedBlackNode<T> root, RedBlackNode<T> rootData, Color rootColor, RedBlackNode<T> right)
        {
            Contracts.AssertValue(root);
            Contracts.AssertValue(root.Left);
            Contracts.AssertValue(rootData);
            Contracts.AssertValueOrNull(right);

            var rootLeft = root.Left;

            // There are 4 main cases, order matters to avoid introducing red-red violations.
            // See RemoveFixupLeft for diagrams, just reverse left and right.

            // CASE 1: The 'double black' node's furthest nephew is red.
            // Perform a right rotation and recolor. Tree is valid after this step.
            if (rootLeft.LeftColor == Color.Red)
            {
                Contracts.Assert(root.LeftColor == Color.Black);
                var newLeft = rootLeft.Left;
                var newRight = Create(rootData._key, rootData._value, rootLeft.Right, right, rootLeft.RightColor, Color.Black);
                root = new InteriorNode(rootLeft._key, rootLeft._value, newLeft, newRight, Color.Black, Color.Black);
                return RemoveCoreResult.ItemRemoved;
            }

            // CASE 2: The 'double black' node's nearest nephew is red.
            // Performing a left rotation and recolor on the root's left child transforms this to case 1.
            // Then performing a right rotation and recolor on the root makes a valid tree.
            // Both rotations and recolorings are performed in one step to save on node creation.
            if (rootLeft.RightColor == Color.Red)
            {
                Contracts.Assert(root.LeftColor == Color.Black);
                Contracts.Assert(rootLeft.LeftColor == Color.Black);
                var rootLeftRight = rootLeft.Right;
                var newLeft = Create(rootLeft._key, rootLeft._value, rootLeft.Left, rootLeftRight.Left, Color.Black, rootLeftRight.LeftColor);
                var newRight = Create(rootData._key, rootData._value, rootLeftRight.Right, right, rootLeftRight.RightColor, Color.Black);
                root = new InteriorNode(rootLeftRight._key, rootLeftRight._value, newLeft, newRight, Color.Black, Color.Black);
                return RemoveCoreResult.ItemRemoved;
            }

            // CASE 3: The 'double black' node's sibling is red.
            // Performing a right rotation and recolor on the root transforms this into EITHER case 1, 2 OR 4,
            // depending on the values of root.Left.Right.LeftColor and root.Left.Right.RightColor
            // All rotations and recolorings are performed in one step to save on node creation.
            // In all cases, the tree is valid after this step.
            if (root.LeftColor == Color.Red)
            {
                Contracts.Assert(rootColor == Color.Black);
                Contracts.Assert(rootLeft.LeftColor == Color.Black);
                Contracts.Assert(rootLeft.RightColor == Color.Black);

                RedBlackNode<T> newLeft;
                RedBlackNode<T> newRight;
                var rootLeftRight = rootLeft.Right;
                Contracts.AssertValue(rootLeftRight);

                // Case 3 -> Case 1
                if (rootLeftRight.LeftColor == Color.Red)
                {
                    var newRightLeft = rootLeftRight.Left;
                    var newRightRight = Create(rootData._key, rootData._value, rootLeftRight.Right, right, rootLeftRight.RightColor, Color.Black);
                    newLeft = rootLeft.Left;
                    newRight = new InteriorNode(rootLeftRight._key, rootLeftRight._value, newRightLeft, newRightRight, Color.Black, Color.Black);
                    root = new InteriorNode(rootLeft._key, rootLeft._value, newLeft, newRight, Color.Black, Color.Red);
                    return RemoveCoreResult.ItemRemoved;
                }

                // Case 3 -> Case 2 -> Case 1
                if (rootLeftRight.RightColor == Color.Red)
                {
                    var rootLeftRightRight = rootLeftRight.Right;
                    Contracts.AssertValue(rootLeftRightRight);
                    var newRightLeft = Create(rootLeftRight._key, rootLeftRight._value, rootLeftRight.Left, rootLeftRightRight.Left, Color.Black, rootLeftRightRight.LeftColor);
                    var newRightRight = Create(rootData._key, rootData._value, rootLeftRightRight.Right, right, rootLeftRightRight.RightColor, Color.Black);
                    newLeft = rootLeft.Left;
                    newRight = new InteriorNode(rootLeftRightRight._key, rootLeftRightRight._value, newRightLeft, newRightRight, Color.Black, Color.Black);
                    root = new InteriorNode(rootLeft._key, rootLeft._value, newLeft, newRight, Color.Black, Color.Red);
                    return RemoveCoreResult.ItemRemoved;
                }

                // Case 3 -> Case 4
                newLeft = rootLeft.Left;
                newRight = new InteriorNode(rootData._key, rootData._value, rootLeftRight, right, Color.Red, Color.Black);
                root = new InteriorNode(rootLeft._key, rootLeft._value, newLeft, newRight, Color.Black, Color.Black);
                return RemoveCoreResult.NewNodeIsBlack;
            }

            // CASE 4: The 'double black' node's parent is red.
            // Move the parent's red to the sibling and color the parent black and the tree becomes valid.
            if (rootColor == Color.Red)
            {
                Contracts.Assert(root.LeftColor == Color.Black);
                root = new InteriorNode(rootData._key, rootData._value, rootLeft, right, Color.Red, Color.Black);
                return RemoveCoreResult.NewNodeIsBlack;
            }

            // CASE 5: All of the 'double black' node's relations are black (parent, sibling, both nephews)
            // Color the sibling red and make the root the new 'double black' node.
            Contracts.Assert(rootColor == Color.Black);
            Contracts.Assert(root.LeftColor == Color.Black);
            Contracts.Assert(rootLeft.LeftColor == Color.Black);
            Contracts.Assert(rootLeft.RightColor == Color.Black);
            root = new InteriorNode(rootData._key, rootData._value, rootLeft, right, Color.Red, Color.Black);
            return RemoveCoreResult.NewNodeIsDoubleBlack;
        }
    }
}
