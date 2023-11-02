// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal interface IConvertToUTC
    {
        DateTime ToUTC(DateTimeValue d);
    }
}
