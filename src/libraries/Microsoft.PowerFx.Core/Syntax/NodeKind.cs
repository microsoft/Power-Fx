// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Syntax
{
    // Node kinds. Primarily used by Intellisense.
    public enum NodeKind
    {
        Blank,

        BinaryOp,
        UnaryOp,
        VariadicOp,

        Call,

        List,
        Record,
        Table,

        DottedName,
        FirstName,
        As,
        Parent,
        Self,

        BoolLit,
        NumLit,
        StrLit,

        Error,
        StrInterp
    }
}
