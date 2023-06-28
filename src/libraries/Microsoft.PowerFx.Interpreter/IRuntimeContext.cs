// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.PowerFx.Interpreter
{
    internal interface IRuntimeContext
    {
        CultureInfo CultureInfo { get; }

        TimeZoneInfo TimeZoneInfo { get; }

        DateTimeKind DateTimeKind { get; }
       
    }
}
