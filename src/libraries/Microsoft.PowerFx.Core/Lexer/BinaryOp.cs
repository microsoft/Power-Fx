// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Lexer
{
    /// <summary>
    /// Kinds of binary operators.
    /// </summary>
    public enum BinaryOp
    {
        /// <summary>
        /// Logical disjunction (||).
        /// </summary>
        Or,

        /// <summary>
        /// Logical conjunction (&amp;&amp;).
        /// </summary>
        And,
        
        /// <summary>
        /// String concatenation (&amp;).
        /// </summary>
        Concat,

        /// <summary>
        /// Arithmetic addition (+).
        /// </summary>
        Add,

        /// <summary>
        /// Arithmetic multiplication (*).
        /// </summary>
        Mul,

        /// <summary>
        /// Arithmetic division (/).
        /// </summary>
        Div,

        /// <summary>
        /// Arithmetic exponentiation (^).
        /// </summary>
        Power,

        /// <summary>
        /// Equal to comparison (=).
        /// </summary>
        Equal,

        /// <summary>
        /// Not equal to comparison (&lt;&gt;).
        /// </summary>
        NotEqual,

        /// <summary>
        /// Less than comparison (&lt;).
        /// </summary>
        Less,

        /// <summary>
        /// Less than or equal to comparison (&lt;=).
        /// </summary>
        LessEqual,

        /// <summary>
        /// Greater than comparison (&gt;).
        /// </summary>
        Greater,

        /// <summary>
        /// Greater than or equal comparison (&gt;=).
        /// </summary>
        GreaterEqual,

        /// <summary>
        /// Substring (case-insensitive) or collection/table membership test.
        /// </summary>
        In,

        /// <summary>
        /// Substring (case-sensitive) or collection/table membership test.
        /// </summary>
        Exactin,

        /// <summary>
        /// Binary operator parsing error.
        /// </summary>
        Error
    }
}
