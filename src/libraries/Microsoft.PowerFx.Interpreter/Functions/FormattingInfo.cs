// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.PowerFx.Functions
{
    [ThreadSafeImmutable]
    internal class FormattingInfo
    {
        public readonly CultureInfo CultureInfo;
        public readonly TimeZoneInfo TimeZoneInfo;

        public FormattingInfo(CultureInfo cultureInfo, TimeZoneInfo timeZoneInfo)            
        {
            CultureInfo = cultureInfo;
            TimeZoneInfo = timeZoneInfo;
        }
    }
}
