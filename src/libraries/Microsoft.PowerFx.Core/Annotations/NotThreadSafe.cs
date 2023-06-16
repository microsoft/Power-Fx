// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// This type is Not thread safe - even if the base class is marked as thread safe. 
    /// This can be used to mark a known type as single-threaded.
    /// Or cases when the derived type is not thread safe, but the base type is.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal class NotThreadSafeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotThreadSafeAttribute"/> class.
        /// </summary>
        public NotThreadSafeAttribute()
        {
        }
    }
}
