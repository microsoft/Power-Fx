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

        public static async Task<DatasetMetadata> GetDatasetsMetadataAsync(HttpClient httpClient, string uriPrefix, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            string uri = (uriPrefix ?? string.Empty)
                + (CdpTableResolver.UseV2(uriPrefix) ? "/v2" : string.Empty)
                + $"/$metadata.json/datasets";

            return await GetObject<DatasetMetadata>(httpClient, "Get datasets metadata", uri, null, cancellationToken, logger).ConfigureAwait(false);
        }

        public virtual async Task<IEnumerable<CdpTable>> GetTablesAsync(HttpClient httpClient, string uriPrefix, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            if (DatasetMetadata == null)
            {
                DatasetMetadata = await GetDatasetsMetadataAsync(httpClient, uriPrefix, cancellationToken, logger).ConfigureAwait(false);
            }

            _uriPrefix = uriPrefix;

            string uri = (_uriPrefix ?? string.Empty)
                + (CdpTableResolver.UseV2(uriPrefix) ? "/v2" : string.Empty)
                + $"/datasets/{(DatasetMetadata.IsDoubleEncoding ? DoubleEncode(DatasetName) : SingleEncode(DatasetName))}"
                + (uriPrefix.Contains("/sharepointonline/") ? "/alltables" : "/tables");

            GetTables tables = await GetObject<GetTables>(httpClient, "Get tables", uri, null, cancellationToken, logger).ConfigureAwait(false);
            return tables?.Value?.Select((RawTable rawTable) => new CdpTable(DatasetName, rawTable.Name, DatasetMetadata, tables?.Value) { DisplayName = rawTable.DisplayName });
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

        /// <summary>
        /// Retrieves a single CdpTable without testing its existence and initializes it.
        /// </summary>
        /// <param name="httpClient">HttpClient.</param>
        /// <param name="uriPrefix">Connector Uri prefix.</param>
        /// <param name="logicalTableName">Logical Table Name.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <param name="logger">Llogger.</param>
        /// <returns>Initialized CdpTable or null is table doesn't exist.</returns>
        public virtual async Task<CdpTable> GetTableAsync(HttpClient httpClient, string uriPrefix, string logicalTableName, CancellationToken cancellation, ConnectorLogger logger = null)
        {
            cancellation.ThrowIfCancellationRequested();

            CdpTable table = new CdpTable(DatasetName, logicalTableName, DatasetMetadata, null);
            await table.InitAsync(httpClient, uriPrefix, cancellation, logger).ConfigureAwait(false);

            return table;
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
