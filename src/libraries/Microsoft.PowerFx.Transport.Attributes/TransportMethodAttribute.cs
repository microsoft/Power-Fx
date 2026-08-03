// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.AppMagic.Transport
{
    /// <summary>
    /// Enables transport on the specified method. See <see cref="TransportTypeAttribute"/> for more information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TransportMethodAttribute : Attribute
    {
        public TransportMethodAttribute(TransportBatchingMode batchingMode = TransportBatchingMode.Default, bool isParallel = false)
        {
            BatchingMode = batchingMode;
            IsParallel = isParallel;
        }

        /// <summary>
        /// Deferred methods must return 'void' or 'Task', and are bundled with the next non-deferred api call.
        /// </summary>
        public bool Deferred { get; }

        /// <summary>
        /// Defines batching.
        /// </summary>
        public TransportBatchingMode BatchingMode { get; }

        /// <summary>
        /// Whether the method can be executed in parallel with other methods marked the same way.
        /// </summary>
        public bool IsParallel { get; }
    }
}
