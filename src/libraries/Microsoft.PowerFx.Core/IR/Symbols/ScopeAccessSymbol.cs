// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Symbols
{
    internal class ScopeAccessSymbol : IScopeSymbol
    {
        public ScopeSymbol Parent { get; }

        public int Index { get; }

        public DName Name => Parent.AccessedFields.ElementAtOrDefault(Index);

        public ScopeAccessSymbol(ScopeSymbol parent, int index)
        {
            Contracts.AssertValue(parent);
            Contracts.AssertIndex(index, parent.AccessedFields.Count);

            Parent = parent;
            Index = index;
        }

        public override string ToString()
        {
            return $"{Parent}, {Name}";
        }
    }
}
