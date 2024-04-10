// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Implements CDP protocol for Tabular connectors
    public class CdpTabularService : TabularService
    {
        public string DataSetName { get; }

        public string TableName { get; }

        protected Func<HttpClient> _getHttpClient;
        protected string _uriPrefix;
        protected readonly ConnectorLogger _configurationLogger;

        private readonly bool _v2;

        public CdpTabularService(string dataset, string table, Func<HttpClient> getHttpClient, bool useV2 = false, string uriPrefix = null, ConnectorLogger logger = null)
        {
            DataSetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
            TableName = table ?? throw new ArgumentNullException(nameof(table));
            _getHttpClient = getHttpClient ?? throw new ArgumentNullException(nameof(getHttpClient));
            _uriPrefix = uriPrefix;
            _v2 = useV2;
            _configurationLogger = logger;
        }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        protected override async Task<RecordType> GetSchemaAsync(CancellationToken cancellationToken)
        {
            try
            {
                _configurationLogger?.LogInformation($"Entering in {nameof(CdpTabularService)} {nameof(GetSchemaAsync)} for {DataSetName}, {TableName}");

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _uriPrefix + (_v2 ? "/v2" : string.Empty) + $"/$metadata.json/datasets/{DataSetName}/tables/{TableName}?api-version=2015-09-01");
                using HttpResponseMessage response = await _getHttpClient().SendAsync(request, cancellationToken).ConfigureAwait(false);

                string text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int statusCode = (int)response.StatusCode;

                string reasonPhrase = string.IsNullOrEmpty(response.ReasonPhrase) ? string.Empty : $" ({response.ReasonPhrase})";
                _configurationLogger?.LogInformation($"Exiting {nameof(CdpTabularService)} {nameof(GetSchemaAsync)} for {DataSetName}, {TableName} with Http Status {statusCode}{reasonPhrase}{(statusCode < 300 ? string.Empty : text)}");

                return statusCode < 300 ? GetSchema(text) : null;
            }
            catch (Exception ex)
            {
                _configurationLogger?.LogException(ex, $"Exception in {nameof(CdpTabularService)} {nameof(GetSchemaAsync)} for {DataSetName}, {TableName}, v2: {_v2}, {ConnectorHelperFunctions.LogException(ex)}");
                throw;
            }
        }

        internal static RecordType GetSchema(string text)
        {
            ConnectorType connectorType = ConnectorFunction.GetConnectorType("Schema/Items", FormulaValue.New(text), ConnectorCompatibility.SwaggerCompatibility);
            return connectorType?.FormulaType as RecordType;
        }

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        public override async Task<ICollection<DValue<RecordValue>>> GetItemsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            try
            {
                ConnectorLogger executionLogger = serviceProvider.GetService<ConnectorLogger>();
                executionLogger?.LogInformation($"Entering in {nameof(CdpTabularService)} {nameof(GetItemsAsync)} for {DataSetName}, {TableName}");

                Uri uri = new Uri(_uriPrefix + (_v2 ? "/v2" : string.Empty) + $"/datasets/{DataSetName}/tables/{TableName}/items?api-version=2015-09-01", UriKind.Relative);

                ODataParameters odataParameters = serviceProvider.GetService<ODataParameters>();
                if (odataParameters != null)
                {
                    uri = odataParameters.GetUri(uri);
                }

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                using HttpResponseMessage response = await _getHttpClient().SendAsync(request, cancellationToken).ConfigureAwait(false);

                string text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int statusCode = (int)response.StatusCode;

                string reasonPhrase = string.IsNullOrEmpty(response.ReasonPhrase) ? string.Empty : $" ({response.ReasonPhrase})";
                executionLogger?.LogInformation($"Exiting {nameof(CdpTabularService)} {nameof(GetItemsAsync)} for {DataSetName}, {TableName} with Http Status {statusCode}{reasonPhrase}{(statusCode < 300 ? string.Empty : text)}");

                return statusCode < 300 ? GetResult(text) : Array.Empty<DValue<RecordValue>>();
            }
            catch (Exception ex)
            {
                _configurationLogger?.LogException(ex, $"Exception in {nameof(CdpTabularService)} {nameof(GetItemsAsync)} for {DataSetName}, {TableName}, v2: {_v2}, {ConnectorHelperFunctions.LogException(ex)}");
                throw;
            }
        }

        protected ICollection<DValue<RecordValue>> GetResult(string text)
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
