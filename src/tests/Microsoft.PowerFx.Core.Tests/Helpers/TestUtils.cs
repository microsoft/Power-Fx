// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.Helpers
{
    internal class TestUtils
    {        
        // Parse a type in string form to a DType
        public static DType DT(string type)
        {
            Assert.True(DType.TryParse(type, out DType dtype));
            Assert.True(dtype.IsValid);
            return dtype;
        }
    }
}
