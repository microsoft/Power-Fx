// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public static class CdpExtensions
    {
        [Obsolete("This API is CDP-only and doens't work for 'shim'-based tabular connectors and is subject to removal/refactoring when we'll fix relationship management for tabular connectors.")]
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
