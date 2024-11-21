// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public static class CdpExtensions
    {
        public static bool TryGetFieldRelationships(this RecordType recordType, string fieldName, out IEnumerable<ConnectorRelationship> relationships)
        {
            if (recordType is not CdpRecordType cdpRecordType || (!cdpRecordType.TryGetFieldRelationships(fieldName, out relationships) && relationships.Any()))
            {
                relationships = null;
                return false;
            }           

            return true;
        }
    }
}
