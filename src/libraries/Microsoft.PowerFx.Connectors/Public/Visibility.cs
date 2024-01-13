// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// "x-ms-visibility" enum.
    /// </summary>
    public enum Visibility : int
    {
        // "x-ms-visibility" is not corresponding to any valid value (normally, only "important", "advanced" or "internal")
        Unknown = -1,

        // "x-ms-visibility" is not defined
        None = 0,

        // "x-ms-visibility" is "important"
        Important,

        // "x-ms-visibility" is "advanced"
        Advanced,

        // "x-ms-visibility" is "internal"
        Internal
    }
}
