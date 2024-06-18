// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Implements CDP protocol for Tabular connectors
    public sealed class CdpTable : CdpService
    {
        public string TableName { get; private set; }

        public string DisplayName { get; internal set; }

        public string DatasetName { get; private set; }

        public override bool IsDelegable => TableCapabilities?.IsDelegable ?? false;

        public override ConnectorType ConnectorType => TabularTableDescriptor.ConnectorType;

        internal ServiceCapabilities TableCapabilities => TabularTableDescriptor.TableCapabilities;

        internal DatasetMetadata DatasetMetadata;

        internal CdpTableDescriptor TabularTableDescriptor;

        private string _uriPrefix;

        public CdpTable(string dataset, string table)
        {
            DatasetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
            TableName = table ?? throw new ArgumentNullException(nameof(table));
        }

        internal CdpTable(string dataset, string table, DatasetMetadata datasetMetadata)
            : this(dataset, table)
        {
            DatasetMetadata = datasetMetadata;
        }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        public async Task InitAsync(HttpClient httpClient, string uriPrefix, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsInitialized)
            {
                throw new InvalidOperationException("TabularService already initialized");
            }

            // $$$ This is a hack to generate ADS
            bool _adsHack = false;            
            if (uriPrefix.StartsWith("*"))
            {
                _adsHack = true;
                uriPrefix = uriPrefix.Substring(1);
            }

            if (DatasetMetadata == null)
            {
                await InitializeDatasetMetadata(httpClient, uriPrefix, logger, cancellationToken).ConfigureAwait(false);
            }

            _uriPrefix = uriPrefix;

            CdpTableResolver tableResolver = new CdpTableResolver(this, httpClient, uriPrefix, DatasetMetadata.IsDoubleEncoding, logger) { GenerateADS = _adsHack };
            TabularTableDescriptor = await tableResolver.ResolveTableAsync(TableName, cancellationToken).ConfigureAwait(false);

            SetRecordType((RecordType)TabularTableDescriptor.ConnectorType?.FormulaType);
        }

        private async Task InitializeDatasetMetadata(HttpClient httpClient, string uriPrefix, ConnectorLogger logger, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CdpDataSource cds = new CdpDataSource(DatasetName);
            await cds.GetDatasetsMetadataAsync(httpClient, uriPrefix, cancellationToken, logger).ConfigureAwait(false);

            DatasetMetadata = cds.DatasetMetadata ?? throw new InvalidOperationException("Dataset metadata is not available");
        }

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        protected override async Task<IReadOnlyCollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, ODataParameters odataParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectorLogger executionLogger = serviceProvider?.GetService<ConnectorLogger>();
            HttpClient httpClient = serviceProvider?.GetService<HttpClient>() ?? throw new InvalidOperationException("HttpClient is required on IServiceProvider");

            string queryParams = (odataParameters != null) ? "&" + odataParameters.ToQueryString() : string.Empty;

            Uri uri = new Uri(
                   (_uriPrefix ?? string.Empty) +
                   (_uriPrefix.Contains("/sql/") ? "/v2" : string.Empty) +
                   $"/datasets/{(DatasetMetadata.IsDoubleEncoding ? DoubleEncode(DatasetName) : DatasetName)}/tables/{HttpUtility.UrlEncode(TableName)}/items?api-version=2015-09-01" + queryParams, UriKind.Relative);

            string text = await GetObject(httpClient, $"List items ({nameof(GetItemsInternalAsync)})", uri.ToString(), cancellationToken, executionLogger).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(text) ? GetResult(text) : Array.Empty<DValue<RecordValue>>();
        }

        private IReadOnlyCollection<DValue<RecordValue>> GetResult(string text)
        {
            // $$$ Is this always this type?
            RecordValue rv = FormulaValueJSON.FromJson(text, RecordType.Empty().Add("value", TableType)) as RecordValue;
            TableValue tv = rv.Fields.FirstOrDefault(field => field.Name == "value").Value as TableValue;

            // The call we make contains more fields and we want to remove them here ('@odata.etag')
            return new InMemoryTableValue(IRContext.NotInSource(TableType), tv.Rows).Rows.ToArray();
        }

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}
    }
}
