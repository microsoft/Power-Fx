// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core.Lexer;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// A list of simple names (<see cref="DName" />), starting at "root" (<see cref="Root" />).
    /// </summary>
    [ThreadSafeImmutable]
    public struct DPath : IEquatable<DPath>, ICheckable
    {
        private class Node : ICheckable
        {
            public const int HashNull = 0x340CA819;

            public readonly Node Parent;
            public readonly DName Name;
            public readonly int Length;

            // Computed lazily and cached. This only hashes the strings, NOT the length.
            private volatile int _hash;

            public bool IsValid => Name.IsValid && (Parent == null ? Length == 1 : Length == Parent.Length + 1);

            public Node(Node par, DName name)
            {
                Contracts.AssertValueOrNull(par);
                Contracts.Assert(name.IsValid);

                Parent = par;
                Name = name;
                Length = par == null ? 1 : 1 + par.Length;
            }

            // Simple recursion avoids memory allocation at the expense of stack space.
            // Only use this for "small" chains.
            public Node Append(Node node)
            {
                AssertValid();
                Contracts.AssertValueOrNull(node);
                if (node == null)
                {
                    return this;
                }

                return new Node(Append(node.Parent), node.Name);
            }

            [Conditional("DEBUG")]
            internal void AssertValid()
            {
                Contracts.AssertValueOrNull(Parent);
                Contracts.Assert(IsValid);
            }

            public override int GetHashCode()
            {
                if (_hash == 0)
                {
                    EnsureHash();
                }

                return _hash;
            }

            private void EnsureHash()
            {
                var hash = Hashing.CombineHash(Parent == null ? HashNull : Parent.GetHashCode(), Name.GetHashCode());
                if (hash == 0)
                {
                    hash = 1;
                }

                Interlocked.CompareExchange(ref _hash, hash, 0);
            }
        }

        // The "root" is indicated by null.
        private readonly Node _node;

        /// <summary>
        /// The "root" path.
        /// </summary>
        public static DPath Root { get; } = default;

        private DPath(Node node)
        {
            Contracts.AssertValueOrNull(node);
            _node = node;
            AssertValid();
        }

        private DPath(DPath par, DName name)
        {
            par.AssertValid();
            Contracts.Assert(name.IsValid);
            _node = new Node(par._node, name);
            AssertValid();
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            Contracts.Assert(IsValid);
        }

        internal DPath Parent => _node == null ? this : new DPath(_node.Parent);

        internal DName Name => _node == null ? default : _node.Name;

        internal bool IsRoot => _node == null;

        /// <summary>
        /// Whether this path is valid.
        /// </summary>
        public bool IsValid => _node == null || _node.IsValid;

        /// <summary>
        /// The length (number of simple names) of the path.
        /// </summary>
        public int Length => _node == null ? 0 : _node.Length;

        /// <summary>
        /// A name at some index.
        /// </summary>
        /// <param name="index">Index of the name in the path.</param>
        /// <returns></returns>
        public DName this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var node = _node;
                while (node.Length > index + 1)
                {
                    node = node.Parent;
                }

                return node.Name;
            }
        }

        /// <summary>
        /// Creates a new path by appending a new simple name.
        /// </summary>
        /// <param name="name">The simple name to append.</param>
        /// <returns></returns>
        public readonly DPath Append(DName name)
        {
            Contracts.CheckValid<DName>(name, nameof(name));

            return new DPath(this, name);
        }

        /// <summary>
        /// Creates a new path by appending another path to this one.
        /// </summary>
        /// <param name="path">The path to append.</param>
        /// <returns></returns>
        public DPath Append(DPath path)
        {
            AssertValid();
            Contracts.CheckValid<DPath>(path, nameof(path));

            if (IsRoot)
            {
                return path;
            }

            // Simple recursion avoids excess memory allocation at the expense of stack space.
            if (path.Length <= 20)
            {
                return new DPath(_node.Append(path._node));
            }

            // For long paths, don't recurse.
            var nodes = new Node[path.Length];
            var inode = 0;
            Node node;
            for (node = path._node; node != null; node = node.Parent)
            {
                nodes[inode++] = node;
            }

            Contracts.Assert(inode == nodes.Length);

            node = _node;
            while (inode > 0)
            {
                var nodeCur = nodes[--inode];
                node = new Node(node, nodeCur.Name);
            }

            return new DPath(node);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToDottedSyntax();
        }

        /// <summary>
        /// Converts this path to a dotted syntax (e.g., Name1.Name2...)
        /// </summary>
        public string ToDottedSyntax()
        {
            if (IsRoot)
            {
                return string.Empty;
            }

            Contracts.Assert(Length > 0);
            var count = 0;
            for (var node = _node; node != null; node = node.Parent)
            {
                count += node.Name.Value.Length;
            }

            var sb = new StringBuilder(count + Length - 1);

            var sep = string.Empty;
            for (var i = 0; i < Length; i++)
            {
                sb.Append(sep);
                var escapedName = TexlLexer.EscapeName(this[i]);
                sb.Append(escapedName);
                sep = TexlLexer.PunctuatorDot;
            }

            return sb.ToString();
        }

        /// <summary>
        /// A sequence of name segments.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DName> Segments()
        {
            var segments = new Stack<DName>();
            for (var node = _node; node != null; node = node.Parent)
            {
                segments.Push(node.Name);
            }

            return segments.AsEnumerable();
        }

        /// <summary>
        /// Check whether two paths are equal.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static bool operator ==(DPath path1, DPath path2)
        {
            var node1 = path1._node;
            var node2 = path2._node;

            for (; ;)
            {
                if (node1 == node2)
                {
                    return true;
                }

                Contracts.Assert(node1 != null || node2 != null);
                if (node1 == null || node2 == null)
                {
                    return false;
                }

                if (node1.GetHashCode() != node2.GetHashCode())
                {
                    return false;
                }

                if (node1.Name != node2.Name)
                {
                    return false;
                }

                node1 = node1.Parent;
                node2 = node2.Parent;
            }
        }

        /// <summary>
        /// Check whether two paths are not equal.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static bool operator !=(DPath path1, DPath path2) => !(path1 == path2);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (_node == null)
            {
                return Node.HashNull;
            }

            return _node.GetHashCode();
        }

        /// <summary>
        /// Whether this path is equal to another path.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(DPath other)
        {
            return this == other;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            Contracts.AssertValueOrNull(obj);
            if (!(obj is DPath))
            {
                return false;
            }

            return this == (DPath)obj;
        }
    }
}
