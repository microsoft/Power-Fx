// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Hosts can enable these flags to try out early features. 
    /// Any flags will eventually default to true and then get removed once the feature is finalized. 
    /// </summary>
    internal static class FeatureFlags
    {
        /// <summary>
        /// Enable String Interpolation feature. 
        /// Added 12/3/2021.
        /// </summary>
        public static bool StringInterpolation = false;
    }
}
