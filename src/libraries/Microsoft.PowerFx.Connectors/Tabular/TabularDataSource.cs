// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class TabularDataSource : DelegationHost
    {
        public TabularDataSource(RecordType recordType, DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, IEnumerable<(string logicalName, string displayName, FormulaType type)> fields)
        {
            TabularDataSource = new ExternalCdpDataSource(recordType, name, datasetName, serviceCapabilities, isReadOnly, fields);
        }

        public override void SetType(RecordType type)
        {
            ((ExternalCdpDataSource)TabularDataSource).SetType(type._type);
        }
    }
}
