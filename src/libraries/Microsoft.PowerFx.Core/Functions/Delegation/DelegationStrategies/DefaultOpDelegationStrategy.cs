﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies
{
    internal sealed class DefaultBinaryOpDelegationStrategy : BinaryOpDelegationStrategy
    {
        public DefaultBinaryOpDelegationStrategy(BinaryOp op, TexlFunction function)
            : base(op, function)
        {
        }
    }
}
