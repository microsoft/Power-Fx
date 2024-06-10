// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Connectors
{
    public class TabularDataSource : TabularServiceBase
    {
        public string DatasetName { get; protected set; }

        private string _uriPrefix;        

        internal DatasetMetadata DatasetMetadata { get; private set; }

        public TabularDataSource(string dataset)
        {
            DatasetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
        }
        
        internal async Task GetDatasetsMetadataAsync(HttpClient httpClient, string uriPrefix, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {            
            _uriPrefix = uriPrefix;

            string uri = (_uriPrefix ?? string.Empty) 
                + (uriPrefix.Contains("/sql/") ? "/v2" : string.Empty) 
                + $"/$metadata.json/datasets";

            DatasetMetadata = await GetObject<DatasetMetadata>(httpClient, "Get datasets metadata", uri, cancellationToken, logger).ConfigureAwait(false);            
        }
        
        public async Task<IEnumerable<TabularTable>> GetTablesAsync(HttpClient httpClient, string uriPrefix, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            if (DatasetMetadata == null)
            {
                await GetDatasetsMetadataAsync(httpClient, uriPrefix, cancellationToken, logger).ConfigureAwait(false);
            }            

            string uri = (_uriPrefix ?? string.Empty) 
                + (uriPrefix.Contains("/sql/") ? "/v2" : string.Empty) 
                + $"/datasets/{(DatasetMetadata.IsDoubleEncoding ? DoubleEncode(DatasetName) : DatasetName)}" 
                + (uriPrefix.Contains("/sharepointonline/") ? "/alltables" : "/tables");

            GetTables tables = await GetObject<GetTables>(httpClient, "Get tables", uri, cancellationToken, logger).ConfigureAwait(false);
            return tables?.Value?.Select(rt => new TabularTable(DatasetName, rt.Name, DatasetMetadata) { DisplayName = rt.DisplayName });
        }
    }
}
