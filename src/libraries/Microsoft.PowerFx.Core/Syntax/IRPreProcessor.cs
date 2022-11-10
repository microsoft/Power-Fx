// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Syntax
{
    internal enum IRPreProcessor
    {
        None,

        BlankToZero,
        BlankToEmptyString,
        NumberTruncate,
    }
}
