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
    public class CdpDataSource : CdpServiceBase
    {
        public string DatasetName { get; protected set; }

        private string _uriPrefix;

        public DatasetMetadata DatasetMetadata { get; private set; }

        public CdpDataSource(string dataset)
        {
            DatasetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
        }

        public static async Task<DatasetMetadata> GetDatasetsMetadataAsync(HttpClient httpClient, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            string uri = $"/$metadata.json/datasets";
            return await GetObject<DatasetMetadata>(httpClient, "Get datasets metadata", uri, null, cancellationToken, logger).ConfigureAwait(false);
        }

        [Obsolete("Use GetDatasetsMetadataAsync without urlPrefix")]
        public static async Task<DatasetMetadata> GetDatasetsMetadataAsync(HttpClient httpClient, string uriPrefix, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            string uri = (uriPrefix ?? string.Empty)
                + (CdpTableResolver.UseV2(uriPrefix) ? "/v2" : string.Empty)
                + $"/$metadata.json/datasets";

            return await GetObject<DatasetMetadata>(httpClient, "Get datasets metadata", uri, null, cancellationToken, logger).ConfigureAwait(false);            
        }

        public virtual async Task<IEnumerable<CdpTable>> GetTablesAsync(HttpClient httpClient, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            if (DatasetMetadata == null)
            {
                DatasetMetadata = await GetDatasetsMetadataAsync(httpClient, cancellationToken, logger).ConfigureAwait(false);
            }

            string queryName = httpClient is PowerPlatformConnectorClient ppcc && ppcc.RequestUrlPrefix.Contains("/sharepointonline/") ? "/alltables" : "/tables";
            string uri = $"/datasets/{(DatasetMetadata.IsDoubleEncoding ? DoubleEncode(DatasetName) : DatasetName)}" + queryName;

            GetTables tables = await GetObject<GetTables>(httpClient, "Get tables", uri, null, cancellationToken, logger).ConfigureAwait(false);
            return tables?.Value?.Select((RawTable rawTable) => new CdpTable(DatasetName, rawTable.Name, DatasetMetadata, tables?.Value) { DisplayName = rawTable.DisplayName });
        }

        [Obsolete("Use GetTablesAsync without urlPrefix")]
        public virtual async Task<IEnumerable<CdpTable>> GetTablesAsync(HttpClient httpClient, string uriPrefix, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            if (DatasetMetadata == null)
            {
                DatasetMetadata = await GetDatasetsMetadataAsync(httpClient, uriPrefix, cancellationToken, logger).ConfigureAwait(false);
            }

            _uriPrefix = uriPrefix;

            string uri = (_uriPrefix ?? string.Empty)
                + (CdpTableResolver.UseV2(uriPrefix) ? "/v2" : string.Empty)
                + $"/datasets/{(DatasetMetadata.IsDoubleEncoding ? DoubleEncode(DatasetName) : DatasetName)}"
                + (uriPrefix.Contains("/sharepointonline/") ? "/alltables" : "/tables");

            GetTables tables = await GetObject<GetTables>(httpClient, "Get tables", uri, null, cancellationToken, logger).ConfigureAwait(false);
            return tables?.Value?.Select((RawTable rawTable) => new CdpTable(DatasetName, rawTable.Name, DatasetMetadata, tables?.Value) { DisplayName = rawTable.DisplayName });
        }

        /// <summary>
        /// Retrieves a single CdpTable.
        /// </summary>
        /// <param name="httpClient">HttpClient.</param>        
        /// <param name="tableName">Table name to search.</param>
        /// <param name="logicalOrDisplay">bool? value: true = logical only, false = display name only, null = logical or display name. All comparisons are case sensitive.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>CdpTable class.</returns>
        /// <exception cref="InvalidOperationException">When no or more than one tables are identified.</exception>        
        public virtual async Task<CdpTable> GetTableAsync(HttpClient httpClient, string tableName, bool? logicalOrDisplay, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<CdpTable> tables = await GetTablesAsync(httpClient, cancellationToken, logger).ConfigureAwait(false);
            IEnumerable<CdpTable> filtered = tables.Where(ct => IsNameMatching(ct.TableName, ct.DisplayName, tableName, logicalOrDisplay));

            if (!filtered.Any())
            {
                throw new InvalidOperationException("Cannot find any table with the specified name");
            }

            if (filtered.Count() > 1)
            {
                throw new InvalidOperationException($"Too many tables correspond to the specified name - Found {filtered.Count()} tables");
            }

            return filtered.First();
        }

        /// <summary>
        /// Retrieves a single CdpTable.
        /// </summary>
        /// <param name="httpClient">HttpClient.</param>
        /// <param name="uriPrefix">Connector Uri prefix.</param>
        /// <param name="tableName">Table name to search.</param>
        /// <param name="logicalOrDisplay">bool? value: true = logical only, false = display name only, null = logical or display name. All comparisons are case sensitive.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>CdpTable class.</returns>
        /// <exception cref="InvalidOperationException">When no or more than one tables are identified.</exception>
        [Obsolete("Use GetTableAsync without urlPrefix")]
        public virtual async Task<CdpTable> GetTableAsync(HttpClient httpClient, string uriPrefix, string tableName, bool? logicalOrDisplay, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<CdpTable> tables = await GetTablesAsync(httpClient, uriPrefix, cancellationToken, logger).ConfigureAwait(false);
            IEnumerable<CdpTable> filtered = tables.Where(ct => IsNameMatching(ct.TableName, ct.DisplayName, tableName, logicalOrDisplay));

            if (!filtered.Any())
            {
                throw new InvalidOperationException("Cannot find any table with the specified name");
            }

            if (filtered.Count() > 1)
            {
                throw new InvalidOperationException($"Too many tables correspond to the specified name - Found {filtered.Count()} tables");
            }

            return filtered.First();
        }

        // logicalOrDisplay
        // true  = logical only
        // false = display name only
        // null  = any
        private bool IsNameMatching(string logicalName, string displayName, string expectedName, bool? logicalOrDisplay)
        {
            if (logicalOrDisplay != false && logicalName == expectedName)
            {
                return true;
            }

            if (logicalOrDisplay != true && displayName == expectedName)
            {
                return true;
            }

            return false;
        }
    }
}
