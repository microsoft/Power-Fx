// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.PowerFx.Functions
{
    public class FormattingInfo
    {
        public CultureInfo CultureInfo { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public TimeZoneInfo TimeZoneInfo { get; set; }
    }
}
