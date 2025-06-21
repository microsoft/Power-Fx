// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// "x-ms-visibility" enum.
    /// </summary>
    public enum Visibility : int
    {
        /// <summary>
        /// "x-ms-visibility" is not corresponding to any valid value (normally, only "important", "advanced" or "internal")
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// "x-ms-visibility" is not defined
        /// </summary>
        None = 0,

        /// <summary>
        /// "x-ms-visibility" is "important"
        /// </summary>
        Important,

        /// <summary>
        /// "x-ms-visibility" is "advanced"
        /// </summary>
        Advanced,

        /// <summary>
        /// "x-ms-visibility" is "internal"
        /// </summary>
        Internal
    }
}
