// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.PowerFx.Functions
{
    internal class FormattingInfo
    {
        internal CultureInfo CultureInfo { get; set; }

        internal CancellationToken CancellationToken { get; set; }

        internal TimeZoneInfo TimeZoneInfo { get; set; }
    }
}
