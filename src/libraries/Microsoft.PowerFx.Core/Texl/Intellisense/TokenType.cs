// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    // Keep in sync with src/AppMagic/js/AppMagic.WebAuthoring/Constants/Texl.ts
    internal enum TokenType
    {
        // default(TokenType) will resolve to Unknown.
        Unknown,
        Min = Unknown,

        // Control entity.
        Control,

        // Global data source, such as an Excel table, or a Sharepoint list.
        Data,

        // Function.
        Function,

        // Screen attribute or alias.
        Alias,

        // Enum, such as Color or SortOrder.
        Enum,

        // A service namespace, such as Facebook or Bing.
        Service,

        // The ThisItem keyword
        ThisItem,

        // Punctuators (currently only those that should be hidden).
        Punctuator,

        // A word that is a part of a dotted name (eg. the words "Selected" and "Text" in "Gallery1.Selected.TextBox1.Text").
        DottedNamePart,

        // An Operator that takes two operands (Eg. + )
        BinaryOp,

        // An Operator that gets applied to one operand only (eg. -, Not)
        UnaryOp,

        // An Operator that takes a variable number of operands/args (eg. semi-colon)
        VariadicOp,

        // A Constant Boolean value (True, False)
        BoolLit,

        // A Constant numeric value (eg. 5, 4.2)
        NumLit,

        // A Constant String Value (eg. "Hello")
        StrLit,

        // An argument delemiter (eg. the comma character)
        Delimiter,

        // App/Component variable.
        ScopeVariable,

        // A Comment
        Comment,

        // Self reference
        Self,

        // Parent reference
        Parent,
        Lim
    }
}