// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Syntax
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
