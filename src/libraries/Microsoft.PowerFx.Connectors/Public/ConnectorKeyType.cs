// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Specifies the key type for a connector (x-ms-keyType).
    /// </summary>
    public enum ConnectorKeyType
    {
        /// <summary>
        /// Undefined key type.
        /// </summary>
        Undefined = -1,

        /// <summary>
        /// Primary key type.
        /// </summary>
        Primary,

        /// <summary>
        /// No key type.
        /// </summary>
        None
    }
}
