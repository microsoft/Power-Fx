// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public static class TabularExtensions
    {
        public static RecordType AddAssociatedDataSource(this RecordType recordType, DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, IEnumerable<(string logicalName, string displayName, FormulaType type)> fields)
        {
            ExternalCdpDataSource eds = new ExternalCdpDataSource(recordType, name, datasetName, serviceCapabilities, isReadOnly, fields);
            HashSet<IExternalTabularDataSource> dataSource = new HashSet<IExternalTabularDataSource>() { eds };
            DType newDType = DType.CreateDTypeWithConnectedDataSourceInfoMetadata(recordType._type, dataSource, null);
            eds.SetType(newDType);
            return new KnownRecordType(newDType);
        }
    }
}
