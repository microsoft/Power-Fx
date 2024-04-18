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
    public class CdpTabularService : TabularService
    {
        public string Name { get; private set; }

        public string DisplayName { get; private set; }

        public string DatasetName { get; protected set; }

        public string TableName { get; protected set; }

        private string _uriPrefix;
        private bool _v2;

        public CdpTabularService(string dataset, string table)
        {
            DatasetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
            TableName = table ?? throw new ArgumentNullException(nameof(table));
        }

        protected CdpTabularService()
        {
        }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        public virtual async Task InitAsync(HttpClient httpClient, string uriPrefix, bool useV2, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger?.LogInformation($"Entering in {nameof(CdpTabularService)} {nameof(InitAsync)} for {DatasetName}, {TableName}");

                if (IsInitialized)
                {
                    throw new InvalidOperationException("TabularService already initialized");
                }

                _v2 = useV2;
                _uriPrefix = uriPrefix;

                string uri = (_uriPrefix ?? string.Empty) + (_v2 ? "/v2" : string.Empty) + $"/$metadata.json/datasets/{DoubleEncode(DatasetName, "dataset")}/tables/{DoubleEncode(TableName, "table")}?api-version=2015-09-01";

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                string text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int statusCode = (int)response.StatusCode;

                string reasonPhrase = string.IsNullOrEmpty(response.ReasonPhrase) ? string.Empty : $" ({response.ReasonPhrase})";
                logger?.LogInformation($"Exiting {nameof(CdpTabularService)} {nameof(InitAsync)} for {DatasetName}, {TableName} with Http Status {statusCode}{reasonPhrase}{(statusCode < 300 ? string.Empty : text)}");

                if (statusCode < 300)
                {
                    SetTableType(GetSchema(text));
                }
            }
            catch (Exception ex)
            {
                logger?.LogException(ex, $"Exception in {nameof(CdpTabularService)} {nameof(InitAsync)} for {DatasetName}, {TableName}, v2: {_v2}, {ConnectorHelperFunctions.LogException(ex)}");
                throw;
            }
        }

        protected virtual string DoubleEncode(string param, string paramName)
        {
            // we force double encoding here (in swagger, we have "x-ms-url-encoding": "double")
            return HttpUtility.UrlEncode(HttpUtility.UrlEncode(param));
        }

        internal RecordType GetSchema(string text)
        {
            ConnectorType connectorType = ConnectorFunction.GetConnectorType("Schema/Items", FormulaValue.New(text), ConnectorCompatibility.SwaggerCompatibility, out string name, out string displayName);
            Name = name;
            DisplayName = displayName;

            return connectorType?.FormulaType as RecordType;
        }

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        protected override async Task<ICollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, ODataParameters odataParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectorLogger executionLogger = serviceProvider?.GetService<ConnectorLogger>();
            HttpClient httpClient = serviceProvider?.GetService<HttpClient>() ?? throw new InvalidOperationException("HttpClient is required on IServiceProvider");

            try
            {
                executionLogger?.LogInformation($"Entering in {nameof(CdpTabularService)} {nameof(GetItemsAsync)} for {DatasetName}, {TableName}");

                Uri uri = new Uri((_uriPrefix ?? string.Empty) + (_v2 ? "/v2" : string.Empty) + $"/datasets/{HttpUtility.UrlEncode(DatasetName)}/tables/{HttpUtility.UrlEncode(TableName)}/items?api-version=2015-09-01", UriKind.Relative);

                if (odataParameters != null)
                {
                    uri = odataParameters.GetUri(uri);
                }

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                string text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int statusCode = (int)response.StatusCode;

                string reasonPhrase = string.IsNullOrEmpty(response.ReasonPhrase) ? string.Empty : $" ({response.ReasonPhrase})";
                executionLogger?.LogInformation($"Exiting {nameof(CdpTabularService)} {nameof(GetItemsAsync)} for {DatasetName}, {TableName} with Http Status {statusCode}{reasonPhrase}{(statusCode < 300 ? string.Empty : text)}");

                return statusCode < 300 ? GetResult(text) : Array.Empty<DValue<RecordValue>>();
            }
            catch (Exception ex)
            {
                executionLogger?.LogException(ex, $"Exception in {nameof(CdpTabularService)} {nameof(GetItemsAsync)} for {DatasetName}, {TableName}, v2: {_v2}, {ConnectorHelperFunctions.LogException(ex)}");
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
