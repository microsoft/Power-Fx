// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public static class TabularExtensions
    {
        public static RecordType AddAssociatedDataSource(this RecordType recordType, DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, IDictionary<string, string> displayNameMapping = null)
        {
            return recordType.AddAssociatedDataSource(name, datasetName, serviceCapabilities, isReadOnly, displayNameMapping == null ? new BidirectionalDictionary<string, string>() : new BidirectionalDictionary<string, string>(displayNameMapping));
        }

        internal static RecordType AddAssociatedDataSource(this RecordType recordType, DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, BidirectionalDictionary<string, string> bidirectionalDictionary = null)
        {
            ExternalCdpDataSource eds = new ExternalCdpDataSource(recordType, name, datasetName, serviceCapabilities, isReadOnly, bidirectionalDictionary);
            HashSet<IExternalTabularDataSource> dataSource = new HashSet<IExternalTabularDataSource>() { eds };
            DType newDType = DType.CreateDTypeWithConnectedDataSourceInfoMetadata(recordType._type, dataSource, null);
            eds.SetType(newDType);
            return new KnownRecordType(newDType);
        }
    }
}
