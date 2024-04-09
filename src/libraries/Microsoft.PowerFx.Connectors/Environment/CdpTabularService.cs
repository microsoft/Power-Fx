﻿// Copyright (c) Microsoft Corporation.
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

        protected HttpClient _httpClient;

        protected string _uriPrefix;

        public CdpTabularService(string dataset, string table, HttpClient httpClient, string uriPrefix = null)
        {
            DataSetName = dataset ?? throw new ArgumentNullException(nameof(dataset));
            TableName = table ?? throw new ArgumentNullException(nameof(table));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _uriPrefix = uriPrefix;
        }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        public override async Task<RecordType> GetSchemaAsync(CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _uriPrefix + $"/$metadata.json/datasets/{DataSetName}/tables/{TableName}?api-version=2015-09-01");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            string text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            int statusCode = (int)response.StatusCode;

            return statusCode < 300 ? GetSchema(text) : null;            
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
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _uriPrefix + $"/datasets/{DataSetName}/tables/{TableName}/items?api-version=2015-09-01");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            string text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            int statusCode = (int)response.StatusCode;

            return statusCode < 300 ? GetResult(text) : Array.Empty<DValue<RecordValue>>();
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