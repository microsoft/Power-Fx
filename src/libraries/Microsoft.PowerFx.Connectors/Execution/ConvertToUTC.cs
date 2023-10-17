// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class ConvertToUTC : IConvertToUTC
    {
        private readonly TimeZoneInfo _tzi;

        public ConvertToUTC(TimeZoneInfo tzi)
        {
            _tzi = tzi;
        }

        public DateTime ToUTC(DateTimeValue dtv)
        {
            return dtv.GetConvertedValue(_tzi);
        }
    }
}
