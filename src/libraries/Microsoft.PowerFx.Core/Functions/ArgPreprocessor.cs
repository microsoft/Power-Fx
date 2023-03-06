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
        ReplaceBlankWithFloatZeroAndTruncate = 2,
        ReplaceBlankWithEmptyString = 3,
        ReplaceBlankWithFuncResultTypedZero = 4,
        ReplaceBlankWithDecimalZero = 5,
    }
}
