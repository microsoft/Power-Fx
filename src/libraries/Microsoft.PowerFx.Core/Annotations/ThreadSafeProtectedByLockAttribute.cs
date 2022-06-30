// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Mark this field is threadsafe because it's protected by a lock.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ThreadSafeProtectedByLockAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeProtectedByLockAttribute"/> class.
        /// </summary>        
        public ThreadSafeProtectedByLockAttribute()
        {
        }
    }
}
