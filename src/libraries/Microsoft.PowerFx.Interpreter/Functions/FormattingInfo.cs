// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.PowerFx.Functions
{
    public class FormattingInfo
    {
        public CultureInfo CultureInfo { get; set; }

        public TimeZoneInfo TimeZoneInfo { get; set; }
    }
}
