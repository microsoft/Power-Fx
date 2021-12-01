﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Syntax
{
    // Node kinds. Primarily used by Intellisense.
    internal enum NodeKind
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
        Replaceable,

        Error
    }
}
