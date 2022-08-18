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
    public interface IRandomService
    {
        /// <summary>
        /// Get the next random number. 
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
