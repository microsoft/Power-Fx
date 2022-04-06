// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Lexer
{
    /// <summary>
    /// Kinds of unary operators.
    /// </summary>
    public enum UnaryOp
    {
        /// <summary>
        /// Logical negation (!).
        /// </summary>
        Not,

        /// <summary>
        /// Arithmetic subtraction and sign (-).
        /// </summary>
        Minus,

        /// <summary>
        /// Percentage (%).
        /// </summary>
        Percent,
    }
}
