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
        public static RecordType AddAssociatedDataSource(this RecordType recordType, DName name, string datasetName, ServiceCapabilities2 serviceCapabilities, bool isReadOnly, IEnumerable<(string logicalName, string displayName, FormulaType type)> fields)
        {
            InternalTableCapabilities internalDataSource = new InternalTableCapabilities(name, serviceCapabilities, isReadOnly, recordType._type, datasetName);
            HashSet<IExternalTabularDataSource> dataSource = new HashSet<IExternalTabularDataSource>() { internalDataSource };
            DType newDType = DType.CreateDTypeWithConnectedDataSourceInfoMetadata(recordType._type, dataSource, null);                        
            return new KnownRecordType(newDType);
        }
    }
}
