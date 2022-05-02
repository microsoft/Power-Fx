﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies
{
    internal interface IOpDelegationStrategy
    {
        bool IsOpSupportedByColumn(OperationCapabilityMetadata metadata, TexlNode column, DPath columnPath, TexlBinding binder);

        bool IsOpSupportedByTable(OperationCapabilityMetadata metadata, TexlNode node, TexlBinding binder);

        bool IsSupportedOpNode(TexlNode node, OperationCapabilityMetadata metadata, TexlBinding binding);
    }
}
