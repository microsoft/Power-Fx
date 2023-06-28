// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.PowerFx.Interpreter
{
    internal interface IRuntimeContext
    {
        CultureInfo CultureInfo { get; }

        TimeZoneInfo TimeZoneInfo { get; }
    }
}
