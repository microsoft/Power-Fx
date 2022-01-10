// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#pragma warning disable 420

using System;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core.Lexer;

namespace Microsoft.PowerFx.Core.Utils
{
    using Conditional = System.Diagnostics.ConditionalAttribute;

    // A path is essentially a list of simple names, starting at "root".
    // TASK: 67008 - Make this public, or expose a public shim in Document.
    internal struct DPath : IEquatable<DPath>, ICheckable
    {
        public const char RootChar = '\u2202';
        private const string RootString = "\u2202";

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
                    return this;
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
                    EnsureHash();
                return _hash;
            }

            private void EnsureHash()
            {
                var hash = Hashing.CombineHash(Parent == null ? HashNull : Parent.GetHashCode(), Name.GetHashCode());
                if (hash == 0)
                    hash = 1;
                Interlocked.CompareExchange(ref _hash, hash, 0);
            }
        }

        // The "root" is indicated by null.
        private readonly Node _node;

        public static readonly DPath Root = default;

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

        public DPath Parent => _node == null ? this : new DPath(_node.Parent);
        public DName Name => _node == null ? default : _node.Name;
        public int Length => _node == null ? 0 : _node.Length;
        public bool IsRoot => _node == null;
        public bool IsValid => _node == null || _node.IsValid;

        public DName this[int index]
        {
            get
            {
                Contracts.AssertIndex(index, Length);
                var node = _node;
                while (node.Length > index + 1)
                    node = node.Parent;
                return node.Name;
            }
        }

        public readonly DPath Append(DName name)
        {
            Contracts.Assert(name.IsValid);
            return new DPath(this, name);
        }

        public DPath Append(DPath path)
        {
            AssertValid();
            path.AssertValid();

            if (IsRoot)
                return path;

            // Simple recursion avoids excess memory allocation at the expense of stack space.
            if (path.Length <= 20)
                return new DPath(_node.Append(path._node));

            // For long paths, don't recurse.
            var nodes = new Node[path.Length];
            var inode = 0;
            Node node;
            for (node = path._node; node != null; node = node.Parent)
                nodes[inode++] = node;
            Contracts.Assert(inode == nodes.Length);

            node = _node;
            while (inode > 0)
            {
                var nodeCur = nodes[--inode];
                node = new Node(node, nodeCur.Name);
            }

            return new DPath(node);
        }

        public DPath GoUp(int count)
        {
            Contracts.AssertIndexInclusive(count, Length);
            return new DPath(GoUpCore(count));
        }

        private Node GoUpCore(int count)
        {
            Contracts.AssertIndexInclusive(count, Length);
            var node = _node;
            while (--count >= 0)
            {
                Contracts.AssertValue(node);
                node = node.Parent;
            }
            return node;
        }

        public override string ToString()
        {
            if (IsRoot)
                return RootString;

            var cch = 1;
            for (var node = _node; node != null; node = node.Parent)
                cch += node.Name.Value.Length + 1;

            var sb = new StringBuilder(cch);
            sb.Length = cch;
            for (var node = _node; node != null; node = node.Parent)
            {
                string str = node.Name;
                var ich = str.Length;
                Contracts.Assert(ich < cch);
                while (ich > 0)
                    sb[--cch] = str[--ich];
                sb[--cch] = '.';
            }
            Contracts.Assert(cch == 1);
            sb[--cch] = RootChar;
            return sb.ToString();
        }

        // Convert this DPath to a string in dotted syntax, such as "screen1.group6.label3"
        public string ToDottedSyntax(string punctuator = ".", bool escapeInnerName = false)
        {
            Contracts.AssertNonEmpty(punctuator);
            Contracts.Assert(punctuator.Length == 1);
            Contracts.Assert(".!".IndexOf(punctuator[0]) >= 0);

            if (IsRoot)
                return string.Empty;

            Contracts.Assert(Length > 0);
            var count = 0;
            for (var node = _node; node != null; node = node.Parent)
                count += node.Name.Value.Length;

            var sb = new StringBuilder(count + Length - 1);

            var sep = string.Empty;
            for (var i = 0; i < Length; i++)
            {
                sb.Append(sep);
                var escapedName = escapeInnerName ? TexlLexer.EscapeName(this[i]) : this[i];
                sb.Append(escapedName);
                sep = punctuator;
            }

            return sb.ToString();
        }

        // Convert a path specified as a string to a DPath.
        // Does not support individual path segments that contain '.' and '!' characters.
        public static bool TryParse(string dotted, out DPath path)
        {
            Contracts.AssertValue(dotted);

            path = DPath.Root;

            if (dotted == string.Empty)
                return true;

            foreach (var name in dotted.Split('.', '!'))
            {
                if (!DName.IsValidDName(name))
                {
                    path = DPath.Root;
                    return false;
                }
                path = path.Append(new DName(name));
            }

            return true;
        }

        public static bool operator ==(DPath path1, DPath path2)
        {
            var node1 = path1._node;
            var node2 = path2._node;

            for (; ; )
            {
                if (node1 == node2)
                    return true;

                Contracts.Assert(node1 != null || node2 != null);
                if (node1 == null || node2 == null)
                    return false;

                if (node1.GetHashCode() != node2.GetHashCode())
                    return false;
                if (node1.Name != node2.Name)
                    return false;
                node1 = node1.Parent;
                node2 = node2.Parent;
            }
        }

        public static bool operator !=(DPath path1, DPath path2)
        {
            return !(path1 == path2);
        }

        public override int GetHashCode()
        {
            if (_node == null)
                return Node.HashNull;
            return _node.GetHashCode();
        }

        public bool Equals(DPath other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            Contracts.AssertValueOrNull(obj);
            if (!(obj is DPath))
                return false;
            return this == (DPath)obj;
        }
    }
}
