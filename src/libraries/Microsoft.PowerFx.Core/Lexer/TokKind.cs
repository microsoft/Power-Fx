// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Kinds of tokens.
    /// </summary>
    public enum TokKind
    {
        None, // TODO: Don't know what this is

        // Miscellaneous

        /// <summary>
        /// End of file.
        /// </summary>
        Eof,

        /// <summary>
        /// Unknown/lexing error.
        /// </summary>
        Error,

        // Identifiers and literals

        /// <summary>
        /// Identifier/name.
        /// </summary>
        Ident,

        /// <summary>
        /// Numeric literal.
        /// <code>3.14</code>
        /// </summary>
        NumLit,

        /// <summary>
        /// String literal.
        /// <code>"Hello world"</code>
        /// </summary>
        StrLit,

        /// <summary>
        /// Comment.
        /// </summary>
        Comment,

        /// <summary>
        /// Whitespace.
        /// </summary>
        Whitespace,

        // Punctuators

        /// <summary>
        /// Addition.
        /// <code>+</code>
        /// </summary>
        Add,

        /// <summary>
        /// Subtraction.
        /// <code>-</code>
        /// </summary>
        Sub,

        /// <summary>
        /// Multiplication.
        /// <code>*</code>
        /// </summary>
        Mul,

        /// <summary>
        /// Division.
        /// <code>/</code>
        /// </summary>
        Div,

        /// <summary>
        /// Power/exponentiation.
        /// <code>^</code>
        /// </summary>
        Caret,

        /// <summary>
        /// Open parenthesis.
        /// <code>(</code>
        /// </summary>
        ParenOpen,

        /// <summary>
        /// Closed parenthesis.
        /// <code>)</code>
        /// </summary>
        ParenClose,

        /// <summary>
        /// Open curly brace.
        /// <code>{</code>
        /// </summary>
        CurlyOpen,

        /// <summary>
        /// Closed curly brace.
        /// <code>}</code>
        /// </summary>
        CurlyClose,

        /// <summary>
        /// Open bracket.
        /// <code>[</code>
        /// </summary>
        BracketOpen,

        /// <summary>
        /// Closed bracket.
        /// <code>]</code>
        /// </summary>
        BracketClose,

        /// <summary>
        /// Equals.
        /// <code>=</code>
        /// </summary>
        Equ,

        /// <summary>
        /// Less than.
        /// <code>&lt;</code>
        /// </summary>
        Lss,

        /// <summary>
        /// Less than or equal.
        /// <code>&lt;=</code>
        /// </summary>
        LssEqu,

        /// <summary>
        /// Greater than.
        /// <code>&gt;</code>
        /// </summary>
        Grt,

        /// <summary>
        /// Greater than or equal.
        /// <code>&gt;=</code>
        /// </summary>
        GrtEqu,

        /// <summary>
        /// Less than or greater than.
        /// <code>&lt;&gt;</code>
        /// </summary>
        LssGrt,

        /// <summary>
        /// Comma.
        /// <code>,</code>
        /// </summary>
        Comma,

        /// <summary>
        /// Dot.
        /// <code>.</code>
        /// </summary>
        Dot,

        /// <summary>
        /// Colon.
        /// <code>:</code>
        /// </summary>
        Colon,

        /// <summary>
        /// Ampersand (concatenation).
        /// <code>&amp;</code>
        /// </summary>
        Ampersand,

        /// <summary>
        /// Percent sign.
        /// <code>%</code>
        /// </summary>
        PercentSign,

        /// <summary>
        /// Semicolon.
        /// <code>;</code>
        /// </summary>
        Semicolon,

        /// <summary>
        /// At symbol.
        /// <code>@</code>
        /// </summary>
        At,

        // Keywords

        /// <summary>
        /// Or operator.
        /// <code>||</code>
        /// </summary>
        Or,

        /// <summary>
        /// And operator
        /// <code>&amp;&amp;</code>.
        /// </summary>
        And,

        /// <summary>
        /// Bang (not).
        /// <code>!</code>
        /// </summary>
        Bang,

        /// <summary>
        /// Boolean true constant.
        /// </summary>
        True,

        /// <summary>
        /// Boolean false constant.
        /// </summary>
        False,

        /// <summary>
        /// In keyword.
        /// </summary>
        In,

        /// <summary>
        /// Exact in keyword.
        /// <code>exactin</code>
        /// </summary>
        Exactin,

        /// <summary>
        /// Self identifier.
        /// </summary>
        Self,

        /// <summary>
        /// Parent identifier.
        /// </summary>
        Parent,

        /// <summary>
        /// Or keyword.
        /// </summary>
        KeyOr,

        /// <summary>
        /// And keyword.
        /// </summary>
        KeyAnd,

        /// <summary>
        /// Not keyword.
        /// </summary>
        KeyNot,

        /// <summary>
        /// As keyword.
        /// </summary>
        As,

        // Interpolation

        /// <summary>
        /// Start of the string interpolation.
        /// <code>$"</code>
        /// </summary>
        StrInterpStart,

        /// <summary>
        /// End of the string interpolation.
        /// </summary>
        StrInterpEnd,

        /// <summary>
        /// Start of the string interpolation part (island).
        /// <code>{</code>
        /// </summary>
        IslandStart,

        /// <summary>
        /// End of the string interpolation part (island).
        /// <code>}</code>
        /// </summary>
        IslandEnd,
        
        /// <summary>
        /// Start of body for user defined functions.
        /// <code>=></code>
        /// </summary>
        DoubleBarrelArrow,
    }
}
