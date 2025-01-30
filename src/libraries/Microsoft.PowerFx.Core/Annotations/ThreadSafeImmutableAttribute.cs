// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// This type is thread safe because it's immutable. 
    /// This means that:
    /// - all of its properties/fields must also be immutable. 
    /// - all derived classes are assumed to also be immutable, unless otherwise marked 
    /// as <see cref="NotThreadSafeAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal class ThreadSafeImmutableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeImmutableAttribute"/> class.
        /// </summary>
        public ThreadSafeImmutableAttribute()
        {
        }
    }
}
