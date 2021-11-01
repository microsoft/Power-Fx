// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Syntax
{
    // Operator precedence.
    internal enum Precedence : byte
    {
        None,
        SingleExpr,
        Or,
        And,
        In,
        Compare,
        Concat,
        Add,
        Mul,
        Error,
        As,
        PrefixUnary,
        Power,
        PostfixUnary,
        Primary,
        Atomic,
    }
}
