// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    public enum AiSensitivity : int
    {
        // "x-ms-ai-sensitivity" is not corresponding to any valid value (normally, only "low", "high")
        Unknown = -1,

        // "x-ms-ai-sensitivity" is not defined
        None = 0,

        // "x-ms-ai-sensitivity" is "low"
        Low,

        // "x-ms-ai-sensitivity" is "high"
        High
    }
}
