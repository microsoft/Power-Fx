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

namespace Microsoft.PowerFx.Connectors.Tabular
{
    // Implements CDP protocol for Tabular connectors
    public sealed class ConnectorTable : TabularService
    {        
        public string DisplayName { get; internal set; }

        public string DatasetName { get; private set; }

        public string TableName { get; private set; }

        public override bool IsDelegable => TableCapabilities?.IsDelegable ?? false;

        public override ConnectorType ConnectorType => _connectorType;

        internal ServiceCapabilities TableCapabilities { get; private set; }

        internal DatasetMetadata DatasetMetadata;

        private string _uriPrefix;
        private ConnectorType _connectorType;            

        public ConnectorTable(string dataset, string table)
        {
            DatasetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
            TableName = table ?? throw new ArgumentNullException(nameof(table));
        }

        internal ConnectorTable(string dataset, string table, DatasetMetadata datasetMetadata)
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

            if (DatasetMetadata == null)
            {
                ConnectorDataSource cds = new ConnectorDataSource(DatasetName);
                await cds.GetDatasetsMetadataAsync(httpClient, uriPrefix, cancellationToken, logger).ConfigureAwait(false);

                DatasetMetadata = cds.DatasetMetadata;

                if (DatasetMetadata == null)
                {
                    throw new InvalidOperationException("Dataset metadata is not available");
                }
            }

            _uriPrefix = uriPrefix;

            string uri = (_uriPrefix ?? string.Empty) +
                (uriPrefix.Contains("/sql/") ? "/v2" : string.Empty) +
                $"/$metadata.json/datasets/{(DatasetMetadata.IsDoubleEncoding ? DoubleEncode(DatasetName) : DatasetName)}/tables/{DoubleEncode(TableName)}?api-version=2015-09-01";

            string text = await GetObject(httpClient, $"Get table metadata", uri, cancellationToken, logger).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(text))
            {
                SetTableType(GetSchema(text));
            }            
        }

        internal RecordType GetSchema(string text)
        {
            _connectorType = ConnectorFunction.GetConnectorTypeAndTableCapabilities("Schema/Items", FormulaValue.New(text), ConnectorCompatibility.SwaggerCompatibility, DatasetName, out string name, out string displayName, out ServiceCapabilities tableCapabilities);
            TableName = name;
            DisplayName = displayName;
            TableCapabilities = tableCapabilities;

            // Note that connectorType contains columns' capabilities but not the FormulaType (as of current developement)
            return _connectorType?.FormulaType as RecordType;
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
