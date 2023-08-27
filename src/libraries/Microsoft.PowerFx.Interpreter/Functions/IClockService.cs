// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Functions
{
    /// <summary>
    /// Service for any time function like Now() or Today().
    /// </summary>
    [ThreadSafeImmutable]
    public interface IClockService
    {
        /// <summary>
        /// Returns the current time, in UTC. 
        /// </summary>
        DateTime UtcNow { get; }
    }

    internal class DefaultClockService : IClockService
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
