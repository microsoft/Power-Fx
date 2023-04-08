// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Functions
{
    internal enum ArgPreprocessor
    {
        None = 0,

        ReplaceBlankWithFloatZero = 1,
        ReplaceBlankWithDecimalZero = 2,
        ReplaceBlankWithFloatZeroAndTruncate = 3,
        ReplaceBlankWithEmptyString = 4,

        // CallZero is a Zero in the same type as the return type of the function call.
        // Scalar and SingleColumnTable is for a function that returns a scalar number or a single column table of numbers, respectively.
        ReplaceBlankWithCallZero_Scalar = 5,
        ReplaceBlankWithCallZero_SingleColumnTable = 6,
    }
}
