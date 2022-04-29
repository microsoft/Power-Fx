// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    /// <summary>
    /// Binding information associated with a name.
    /// </summary>
    internal abstract class NameInfo
    {
        public readonly BindKind Kind;
        public readonly NameNode Node;

        public abstract DName Name { get; }

        protected NameInfo(BindKind kind, NameNode node)
        {
            Contracts.Assert(kind >= BindKind.Min && kind < BindKind.Lim);
            Contracts.AssertValue(node);

            Kind = kind;
            Node = node;
        }

        /// <summary>
        /// Asserts that the object is in fact of type T before casting.
        /// </summary>
        public T As<T>()
            where T : NameInfo
        {
            Contracts.Assert(this is T);

            return (T)this;
        }
    }
}
