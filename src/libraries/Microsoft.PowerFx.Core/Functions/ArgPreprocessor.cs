// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Functions
{
    // This enum provides the different ways that an argument to a function can be pre processed before entering the function.
    // The function classes typically define public override ArgPreprocessor GetArgPreprocessor(int index, CallNode node) to provide which
    // pro-processor should be used for each argument by index.
    // 
    // For example, an argument may speficy ReplaceBlankWithFloatZero.  All of these "ReplaceBlank" by injecting a Coalesce call 
    // into the IR.  The only question is what to Coalesce to, in particular which type.  In the case of the Truncate variations,
    // the result is further processed to truncate to an integer, but still remains in a Number type.
    internal enum ArgPreprocessor
    {
        None = 0,

        ReplaceBlankWithFloatZero = 1,
        ReplaceBlankWithDecimalZero = 2,
        ReplaceBlankWithEmptyString = 3,

        // These are used for integer arguments, such as the number of characters for the Left function.
        // A call to the Trunc function is injected into the IR
        ReplaceBlankWithFloatZeroAndTruncate = 4,
        ReplaceBlankWithDecimalZeroAndTruncate = 5,

        // CallZero is a Zero in the same type as the return type of the function call.
        // Scalar and SingleColumnTable is for a function that returns a scalar number or a single column table of numbers, respectively.
        ReplaceBlankWithCallZero_Scalar = 6,
        ReplaceBlankWithCallZero_SingleColumnTable = 7,

        // Untyped object to untyped object preprocessors
        UntypedStringToUntypedNumber = 8,

        MutationCopy = 9,
    }
}
