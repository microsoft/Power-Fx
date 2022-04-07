// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.AppMagic.Transport
{
    /// <summary>
    /// Describes which batching policy should be applied to the api.
    /// </summary>
    /// <remarks>
    /// Must be manually kept in sync with Transport.d.ts.
    /// </remarks>
    public enum TransportBatchingMode
    {
        /// <summary>
        /// Default. Uses the current default policy.
        /// </summary>
        /// <remarks>
        /// Initially, the default policy is "Immediate" to maintain backwards compatibility. At some future point, prefer 
        /// "Batched" mode, as it's more efficient, but can greatly disturb application timing.
        /// </remarks>
        Default = 0,

        /// <summary>
        /// Forces the request to be sent immediately.
        /// </summary>
        Immediate = 1,

        /// <summary>
        /// Queues the request until the end of the current JavaScript turn. Increases the chance that another call will be batched
        /// with this one, while reducing the change of regressing user-perceived latency.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Deferred priority. The call is queued for a short period of time before sending, to increase the chance of 
        /// batching with other calls.
        /// </summary>
        Deferred = 3,

        /// <summary>
        /// Deferred, possibly indefinitely. The call will only be sent if a later one is sent with higher priority, or if the maximum
        /// queue size is exceeded.
        /// </summary>
        LowPriority = 4,
    }
}
