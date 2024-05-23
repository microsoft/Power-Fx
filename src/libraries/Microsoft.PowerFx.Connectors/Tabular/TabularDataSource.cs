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

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal class TabularDataSource : IExternalTabularDataSource
    {
        public TabularDataSource(DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, BidirectionalDictionary<string, string> displayNameMapping = null)
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

        public bool IsRefreshable => true;

        public bool RequiresAsync => true;

        public bool IsWritable { get; }

        public IExternalDataEntityMetadataProvider DataEntityMetadataProvider { get; }

        public DataSourceKind Kind => DataSourceKind.Connected;

        public IExternalTableMetadata TableMetadata => throw new NotImplementedException();

        public IDelegationMetadata DelegationMetadata => throw new NotImplementedException();

        public DName EntityName { get; }

        public DType Type => throw new NotImplementedException();

        public bool IsConvertingDisplayNameMapping { get; protected set; }

        public BidirectionalDictionary<string, string> DisplayNameMapping { get; protected set; }

        public BidirectionalDictionary<string, string> PreviousDisplayNameMapping { get; protected set; }

        public bool CanIncludeExpand(IExpandInfo expandToAdd)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeSelect(string selectColumnName)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<string> GetKeyColumns()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetKeyColumns(IExpandInfo expandInfo)
        {
            throw new NotImplementedException();
        }
    }
}
