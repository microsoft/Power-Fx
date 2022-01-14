// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer;

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
