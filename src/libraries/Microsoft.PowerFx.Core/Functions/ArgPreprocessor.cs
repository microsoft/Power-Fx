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
        ReplaceWithZero = 1,
        
        // This also includes ReplaceWithZero
        Trurncate = 2,
    }
}
