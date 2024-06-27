// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public static class CdpExtensions
    {
        public static bool TryGetFieldExternalTableName(this RecordType recordType, string fieldName, out string tableName)
        {
            if (recordType is not CdpRecordType cdpRecordType)
            {
                tableName = null;
                return false;
            }

            return cdpRecordType.TryGetFieldExternalTableName(fieldName, out tableName);
        }
    }
}
