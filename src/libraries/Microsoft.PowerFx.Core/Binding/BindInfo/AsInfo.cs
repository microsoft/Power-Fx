﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    internal sealed class AsInfo
    {
        public readonly AsNode Node;
        public readonly DName AsIdentifier;

        public AsInfo(AsNode node, DName identifier)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValid(identifier);

            AsIdentifier = identifier;
            Node = node;
        }
    }
}
