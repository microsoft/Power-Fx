// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Mark this field is threadsafe because it's protected by a lock.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class ThreadSafeProtectedByLockAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeProtectedByLockAttribute"/> class.
        /// </summary>
        /// <param name="lockName">name of field in this class that's used as a lock to protect this field.</param>
        public ThreadSafeProtectedByLockAttribute(string lockName)
        {
        }
    }
}
