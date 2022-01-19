// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Lexer;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies
{
    internal sealed class DefaultUnaryOpDelegationStrategy : UnaryOpDelegationStrategy
    {
        public DefaultUnaryOpDelegationStrategy(UnaryOp op, TexlFunction function)
            : base(op, function)
        {
        }
    }
}
