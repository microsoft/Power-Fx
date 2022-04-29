// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    internal sealed class DottedNameInfo
    {
        public readonly DottedNameNode Node;

        // Optional data associated with a DottedNameNode. May be null.
        public readonly object Data;

        public DottedNameInfo(DottedNameNode node, object data = null)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValueOrNull(data);

            Node = node;
            Data = data;
        }
    }
}
