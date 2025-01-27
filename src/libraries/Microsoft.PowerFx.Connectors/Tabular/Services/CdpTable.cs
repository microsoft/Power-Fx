// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Implements CDP protocol for Tabular connectors
    public class CdpTable : CdpService
    {
        public string TableName { get; private set; }

        public string DisplayName { get; internal set; }

        public string DatasetName { get; private set; }

        public override HttpClient HttpClient => _httpClient;

        public override bool IsDelegable => (DelegationInfo?.SortRestriction != null) || (DelegationInfo?.FilterRestriction != null) || (DelegationInfo?.FilterSupportedFunctions != null);

        public IEnumerable<OptionSet> OptionSets { get; private set; }

        internal TableDelegationInfo DelegationInfo => ((DataSourceInfo)TabularTableDescriptor.FormulaType._type.AssociatedDataSources.First()).DelegationInfo;

        internal override IReadOnlyDictionary<string, Relationship> Relationships => _relationships;

        internal DatasetMetadata DatasetMetadata;

        internal ConnectorType TabularTableDescriptor;

        internal IReadOnlyCollection<RawTable> Tables;

        [Obsolete]
        private string _uriPrefix = null;

        private HttpClient _httpClient;

        private IReadOnlyDictionary<string, Relationship> _relationships;

        internal CdpTable(string dataset, string table, IReadOnlyCollection<RawTable> tables)
        {
            DatasetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
            TableName = table ?? throw new ArgumentNullException(nameof(table));
            Tables = tables;
        }

        internal CdpTable(string dataset, string table, DatasetMetadata datasetMetadata, IReadOnlyCollection<RawTable> tables)
            : this(dataset, table, tables)
        {
            DatasetMetadata = datasetMetadata;
        }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        public virtual async Task InitAsync(HttpClient httpClient, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsInitialized)
            {
                throw new InvalidOperationException("TabularService already initialized");
            }

            _httpClient = httpClient;

            if (DatasetMetadata == null)
            {
                await InitializeDatasetMetadata(httpClient, logger, cancellationToken).ConfigureAwait(false);
            }            

            CdpTableResolver tableResolver = new CdpTableResolver(this, httpClient, DatasetMetadata.IsDoubleEncoding, logger);
            TabularTableDescriptor = await tableResolver.ResolveTableAsync(TableName, cancellationToken).ConfigureAwait(false);

            _relationships = TabularTableDescriptor.Relationships;
            OptionSets = tableResolver.OptionSets;

            RecordType = (RecordType)TabularTableDescriptor.FormulaType;
        }

        [Obsolete("Use InitAsync without uriPrefix")]
        public virtual async Task InitAsync(HttpClient httpClient, string uriPrefix, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsInitialized)
            {
                throw new InvalidOperationException("TabularService already initialized");
            }

            _httpClient = httpClient;

            if (DatasetMetadata == null)
            {
                await InitializeDatasetMetadata(httpClient, uriPrefix, logger, cancellationToken).ConfigureAwait(false);
            }

            _uriPrefix = uriPrefix;

            CdpTableResolver tableResolver = new CdpTableResolver(this, httpClient, uriPrefix, DatasetMetadata.IsDoubleEncoding, logger);
            TabularTableDescriptor = await tableResolver.ResolveTableAsync(TableName, cancellationToken).ConfigureAwait(false);

            _relationships = TabularTableDescriptor.Relationships;
            OptionSets = tableResolver.OptionSets;

            RecordType = (RecordType)TabularTableDescriptor.FormulaType;
        }

        private async Task InitializeDatasetMetadata(HttpClient httpClient, ConnectorLogger logger, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(httpClient, cancellationToken, logger).ConfigureAwait(false);

            DatasetMetadata = dm ?? throw new InvalidOperationException("Dataset metadata is not available");
        }

        [Obsolete]
        private async Task InitializeDatasetMetadata(HttpClient httpClient, string uriPrefix, ConnectorLogger logger, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(httpClient, uriPrefix, cancellationToken, logger).ConfigureAwait(false);

            DatasetMetadata = dm ?? throw new InvalidOperationException("Dataset metadata is not available");
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

            string queryParams = (odataParameters != null) ? "&" + odataParameters.ToQueryString() : string.Empty;

#pragma warning disable CS0612 // Type or member is obsolete
            string prefix = string.IsNullOrEmpty(_uriPrefix) ? string.Empty : (_uriPrefix ?? string.Empty) + (CdpTableResolver.UseV2(_uriPrefix) ? "/v2" : string.Empty);
#pragma warning restore CS0612 // Type or member is obsolete

            Uri uri = new Uri($"{prefix}/datasets/{(DatasetMetadata.IsDoubleEncoding ? DoubleEncode(DatasetName) : DatasetName)}/tables/{Uri.EscapeDataString(TableName)}/items?api-version=2015-09-01" + queryParams, UriKind.Relative);

            string text = await GetObject(_httpClient, $"List items ({nameof(GetItemsInternalAsync)})", uri.ToString(), null, cancellationToken, executionLogger).ConfigureAwait(false);
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
