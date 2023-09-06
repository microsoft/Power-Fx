// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;

namespace Microsoft.PowerFx
{
    public interface IRuntimeConnectorContext
    {
        HttpMessageInvoker GetInvoker(string @namespace);

        TimeZoneInfo TimeZoneInfo { get; }

        CultureInfo CultureInfo { get; }
    }
}
