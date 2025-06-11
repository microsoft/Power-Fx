// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Defines permission levels for a connector.
    /// </summary>
    public enum ConnectorPermission
    {
        /// <summary>
        /// Undefined permission.
        /// </summary>
        Undefined = -1,

        /// <summary>
        /// Read-only permission.
        /// </summary>
        PermissionReadOnly,

        /// <summary>
        /// Read-write permission.
        /// </summary>
        PermissionReadWrite
    }
}
