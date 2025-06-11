// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// "x-ms-ai-sensitivity" enum.
    /// </summary>
    public enum AiSensitivity : int
    {
        /// <summary>
        /// "x-ms-ai-sensitivity" is not corresponding to any valid value (normally, only "low", "high")
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// "x-ms-ai-sensitivity" is not defined
        /// </summary>
        None = 0,

        /// <summary>
        /// "x-ms-ai-sensitivity" is "low"
        /// </summary>
        Low,

        /// <summary>
        /// "x-ms-ai-sensitivity" is "high"
        /// </summary>
        High
    }
}
