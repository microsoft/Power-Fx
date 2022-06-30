// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// This type is thread safe because it's immutable. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal sealed class ThreadSafeImmutableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeImmutableAttribute"/> class.
        /// </summary>
        public ThreadSafeImmutableAttribute()
        {
        }
    }
}
