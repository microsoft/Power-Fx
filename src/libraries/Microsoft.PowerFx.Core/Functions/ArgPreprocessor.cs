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
        ReplaceBlankWithZero = 1,
        ReplaceBlankWithZeroAndTruncate = 2,
        ReplaceBlankWithEmptyString = 3,
    }
}
