// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Extension methods for CDP-related types.
    /// </summary>
    public static class CdpExtensions
    {
        /// <summary>
        /// Tries to get the external table name and foreign key for a field in a RecordType.
        /// </summary>
        public static bool TryGetFieldExternalTableName(this RecordType recordType, string fieldName, out string tableName, out string foreignKey)
        {
            if (recordType is not CdpRecordType cdpRecordType)
            {
                tableName = null;
                foreignKey = null;
                return false;
            }

            return cdpRecordType.TryGetFieldExternalTableName(fieldName, out tableName, out foreignKey);
        }
    }
}
