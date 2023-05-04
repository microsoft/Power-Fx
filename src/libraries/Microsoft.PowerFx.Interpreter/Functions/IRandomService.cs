// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Functions
{
    /// <summary>
    /// Service for any functions needing Random behavior, 
    /// such as Rand(), RandBetween(), or Shuffle().
    /// </summary>
    [ThreadSafeImmutable]
    public interface IRandomService
    {
        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// Should be from a linear distribution. 
        /// Expected this call is thread-safe and may be called concurrently from multiple threads.
        /// </summary>
        /// <returns></returns>
        double NextDouble();
    }

    // Default implementation of IRandomService
    internal class DefaultRandomService : IRandomService
    {
        private static readonly object _randomizerLock = new object();

        [ThreadSafeProtectedByLock(nameof(_randomizerLock))]
        private static readonly Random _random = new Random();

        public virtual double NextDouble()
        {
            lock (_randomizerLock)
            {
                return _random.NextDouble();
            }
        }
    }
}
