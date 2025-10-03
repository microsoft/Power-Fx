// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors.Tabular;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Implements CDP protocol for Tabular connectors
    public class CdpTable : CdpService
    {
        // Logical name
        public string TableName { get; private set; }

        public string DisplayName { get; internal set; }

        public string DatasetName { get; private set; }

        public override HttpClient HttpClient => _httpClient;

        public ConnectorType ConnnectorType => TabularTableDescriptor;

        public override bool IsDelegable => (DelegationInfo?.SortRestriction != null) || (DelegationInfo?.FilterRestriction != null) || (DelegationInfo?.FilterSupportedFunctions != null);

        public IEnumerable<OptionSet> OptionSets { get; private set; }

        internal TableDelegationInfo DelegationInfo => ((DataSourceInfo)TabularTableDescriptor.FormulaType._type.AssociatedDataSources.First()).DelegationInfo;

        internal override IReadOnlyDictionary<string, Relationship> Relationships => _relationships;

        internal DatasetMetadata DatasetMetadata;

        internal ConnectorType TabularTableDescriptor;

        internal IReadOnlyCollection<RawTable> Tables;

        private string _uriPrefix;

        private HttpClient _httpClient;

        private IReadOnlyDictionary<string, Relationship> _relationships;

        private readonly ConnectorSettings _connectorSettings;

        public override ConnectorSettings ConnectorSettings => _connectorSettings;

        internal CdpTable(string dataset, string table, IReadOnlyCollection<RawTable> tables, ConnectorSettings connectorSettings)
        {
            DatasetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
            TableName = table ?? throw new ArgumentNullException(nameof(table));
            Tables = tables;
            _connectorSettings = connectorSettings ?? ConnectorSettings.NewCDPConnectorSettings();
        }

        internal CdpTable(string dataset, string table, DatasetMetadata datasetMetadata, IReadOnlyCollection<RawTable> tables, ConnectorSettings connectorSettings)
            : this(dataset, table, tables, connectorSettings)
        {
            DatasetMetadata = datasetMetadata;
        }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01        
        // get MIP Data here.
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

            CdpTableResolver tableResolver = new CdpTableResolver(this, httpClient, uriPrefix, DatasetMetadata.IsDoubleEncoding, _connectorSettings, logger);
            TabularTableDescriptor = await tableResolver.ResolveTableAsync(TableName, cancellationToken).ConfigureAwait(false);
            
            if (TabularTableDescriptor.HasErrors)
            {
                throw new PowerFxConnectorException($"Table has errors in its schema - {string.Join(", ", TabularTableDescriptor.Errors)}");
            }
            
            _relationships = TabularTableDescriptor.Relationships;
            DisplayName ??= TabularTableDescriptor.DisplayName;
            OptionSets = tableResolver.OptionSets;

            RecordType = (RecordType)TabularTableDescriptor.FormulaType;
        }

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
        protected override async Task<IReadOnlyCollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, DelegationParameters parameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string text = await Query(serviceProvider, parameters, cancellationToken).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(text) ? GetResult(text) : Array.Empty<DValue<RecordValue>>();
        }

        protected override async Task<FormulaValue> GetItemInternalAsync(IServiceProvider serviceProvider, DelegationParameters parameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string text = await Query(serviceProvider, parameters, cancellationToken).ConfigureAwait(false);
            var result = FormulaValueJSON.FromJson(text);
            return result;
        }

        private async Task<string> Query(IServiceProvider serviceProvider, DelegationParameters parameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectorLogger executionLogger = serviceProvider?.GetService<ConnectorLogger>();
            parameters ??= new DefaultCDPDelegationParameter(ConnnectorType.FormulaType, _connectorSettings.MaxRows);
            string queryParams = parameters.GetODataQueryString(_connectorSettings.QueryMarshallerSettings);
            if (!string.IsNullOrEmpty(queryParams))
            {
                queryParams = "&" + queryParams;
            }

            var uri = (_uriPrefix ?? string.Empty) +
                   (CdpTableResolver.UseV2(_uriPrefix) ? "/v2" : string.Empty) +
                   $"/datasets/{(DatasetMetadata.IsDoubleEncoding ? DoubleEncode(DatasetName) : SingleEncode(DatasetName))}/tables/{Uri.EscapeDataString(TableName)}/items?api-version=2015-09-01{queryParams}";

            string text = await GetObject(_httpClient, $"List items ({nameof(GetItemsInternalAsync)})", uri, null, cancellationToken, executionLogger).ConfigureAwait(false);
            return text;
        }

        private IReadOnlyCollection<DValue<RecordValue>> GetResult(string text)
        {
            RecordType rt = RecordType.Empty().Add("value", TableType);
            FormulaValue fv = FormulaValueJSON.FromJson(text, rt);

            if (fv is ErrorValue ev)
            {
                return new List<DValue<RecordValue>>() { DValue<RecordValue>.Of(ev) };
            }

            if (fv is not RecordValue rv)
            {
                ErrorValue err = new ErrorValue(IRContext.NotInSource(rt), new ExpressionError()
                {
                    Message = $"FormulaValueJSON.FromJson doesn't return a RecordValue - Received {fv.GetType().Name}",
                    Span = new Syntax.Span(0, 0),
                    Kind = ErrorKind.InvalidJSON
                });

                return new List<DValue<RecordValue>>() { DValue<RecordValue>.Of(err) };
            }

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
