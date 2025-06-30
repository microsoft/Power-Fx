// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    // Keep in sync with src/AppMagic/js/AppMagic.WebAuthoring/Constants/Texl.ts
    // Keep in sync with the @microsoft/power-fx-formulabar package

    /// <summary>
    /// Represents the types of tokens used in Power Fx IntelliSense.
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Unknown token type. Default value.
        /// </summary>
        Unknown,

        /// <summary>
        /// Minimum token type value (alias for Unknown).
        /// </summary>
        Min = Unknown,

        /// <summary>
        /// Control entity token type.
        /// </summary>
        Control,

        /// <summary>
        /// Global data source token type, such as an Excel table or SharePoint list.
        /// </summary>
        Data,

        /// <summary>
        /// Function token type.
        /// </summary>
        Function,

        /// <summary>
        /// Screen attribute or alias token type.
        /// </summary>
        Alias,

        /// <summary>
        /// Enum token type, such as Color or SortOrder.
        /// </summary>
        Enum,

        /// <summary>
        /// Service namespace token type, such as Facebook or Bing.
        /// </summary>
        Service,

        /// <summary>
        /// The ThisItem keyword token type.
        /// </summary>
        ThisItem,

        /// <summary>
        /// Punctuator token type (currently only those that should be hidden).
        /// </summary>
        Punctuator,

        /// <summary>
        /// A word that is a part of a dotted name (e.g., "Selected" and "Text" in "Gallery1.Selected.TextBox1.Text").
        /// </summary>
        DottedNamePart,

        /// <summary>
        /// Binary operator token type (takes two operands, e.g., +).
        /// </summary>
        BinaryOp,

        /// <summary>
        /// Unary operator token type (applied to one operand only, e.g., -, Not).
        /// </summary>
        UnaryOp,

        /// <summary>
        /// Variadic operator token type (takes a variable number of operands/args, e.g., semicolon).
        /// </summary>
        VariadicOp,

        /// <summary>
        /// Constant Boolean value token type (True, False).
        /// </summary>
        BoolLit,

        /// <summary>
        /// Constant float value token type (e.g., ~5.1, 4.2, 1e300).
        /// </summary>
        NumLit,

        /// <summary>
        /// Constant decimal value token type (e.g., 5.00000000000000000000000000000000001, 4.2, 1e28).
        /// </summary>
        DecLit,        

        /// <summary>
        /// Constant string value token type (e.g., "Hello").
        /// </summary>
        StrLit,

        /// <summary>
        /// Argument delimiter token type (e.g., the comma character).
        /// </summary>
        Delimiter,

        /// <summary>
        /// App/Component variable token type.
        /// </summary>
        ScopeVariable,

        /// <summary>
        /// Comment token type.
        /// </summary>
        Comment,

        /// <summary>
        /// Self reference token type.
        /// </summary>
        Self,

        /// <summary>
        /// Parent reference token type.
        /// </summary>
        Parent,

        /// <summary>
        /// String interpolation start token type.
        /// </summary>
        StrInterpStart,

        /// <summary>
        /// String interpolation end token type.
        /// </summary>
        StrInterpEnd,

        /// <summary>
        /// Island start token type.
        /// </summary>
        IslandStart,

        /// <summary>
        /// Island end token type.
        /// </summary>
        IslandEnd,

        /// <summary>
        /// Type token type.
        /// </summary>
        Type, 

        /// <summary>
        /// Limit value for token types.
        /// </summary>
        Lim
    }
}
