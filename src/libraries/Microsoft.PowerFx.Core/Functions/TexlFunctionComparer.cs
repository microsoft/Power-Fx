// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class TexlFunctionComparer : IEqualityComparer<TexlFunction>
    {
        public bool Equals(TexlFunction x, TexlFunction y)
        {
            return object.Equals(x, y);
        }

        public int GetHashCode(TexlFunction obj)
        {
            return obj.GetHashCode();
        }
    }
}
