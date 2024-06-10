// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Connectors
{
    internal class ExternalTabularDataSource : IExternalTabularDataSource
    {
        public ExternalTabularDataSource(DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, BidirectionalDictionary<string, string> displayNameMapping = null)
        {
            EntityName = name;
            ServiceCapabilities = serviceCapabilities;
            IsWritable = !isReadOnly;

            CdpEntityMetadataProvider metadataProvider = new CdpEntityMetadataProvider();
            TabularDataSourceMetadata tabularDataSourceMetadata = new TabularDataSourceMetadata(name.Value, datasetName);
            tabularDataSourceMetadata.LoadClientSemantics();
            metadataProvider.AddSource(name.Value, tabularDataSourceMetadata);

            DataEntityMetadataProvider = metadataProvider;
            IsConvertingDisplayNameMapping = false;
            DisplayNameMapping = displayNameMapping ?? new BidirectionalDictionary<string, string>();
            PreviousDisplayNameMapping = null;
        }

        internal ServiceCapabilities ServiceCapabilities;

        public TabularDataQueryOptions QueryOptions => new TabularDataQueryOptions(this);

        public string Name => EntityName.Value;

        public bool IsSelectable => ServiceCapabilities.IsSelectable;

        public bool IsDelegatable => ServiceCapabilities.IsDelegable;

        public bool IsPageable => ServiceCapabilities.IsPagable;

        public bool IsClearable => throw new NotImplementedException();

        public bool IsRefreshable => true;

        public bool RequiresAsync => true;

        public bool IsWritable { get; }

        public IExternalDataEntityMetadataProvider DataEntityMetadataProvider { get; }

        public DataSourceKind Kind => DataSourceKind.Connected;

        public IExternalTableMetadata TableMetadata => null; /* _tableMetadata; */

        public IDelegationMetadata DelegationMetadata => throw new NotImplementedException();

        public DName EntityName { get; }

        public DType Type => throw new NotImplementedException();

        public bool IsConvertingDisplayNameMapping { get; protected set; }

        public BidirectionalDictionary<string, string> DisplayNameMapping { get; protected set; }

        public BidirectionalDictionary<string, string> PreviousDisplayNameMapping { get; protected set; }

        public bool CanIncludeExpand(IExpandInfo expandToAdd) => true;

        public bool CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd) => true;

        public bool CanIncludeSelect(string selectColumnName) => TableMetadata != null; /* && TableMetadata.CanIncludeSelect(selectColumnName);*/

        public bool CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName) => true;

        public IReadOnlyList<string> GetKeyColumns()
        {            
            return /*TableMetadata?.KeyColumns ??*/ new List<string>();
        }

        public IEnumerable<string> GetKeyColumns(IExpandInfo expandInfo)
        {
            throw new NotImplementedException();
        }
    }
}
