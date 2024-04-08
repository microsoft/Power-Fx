// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class SwaggerTabularService : TabularService
    {
        //public string Namespace { get; private set; }

        //public string TableName { get; private set; }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        internal ConnectorFunction MetadataService => _metadataService ??= GetMetadataService();

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        internal ConnectorFunction GetItems => _getItems ??= GetItemsFunction();

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}

        private IReadOnlyList<ConnectorFunction> _tabularFunctions;
        private ConnectorFunction _metadataService;
        private ConnectorFunction _getItems;

        private readonly OpenApiDocument _openApiDocument;
        private readonly IReadOnlyDictionary<string, FormulaValue> _globalValues;
        private readonly HttpClient _httpClient;
        private readonly ConnectorLogger _connectorLogger;

        public SwaggerTabularService(OpenApiDocument openApiDocument, IReadOnlyDictionary<string, FormulaValue> globalValues, HttpClient client, ConnectorLogger configurationLogger = null)
            : base(globalValues)
        {
            _openApiDocument = openApiDocument;
            _globalValues = globalValues;
            _httpClient = client;
            _connectorLogger = configurationLogger;
        }

        internal override async Task<RecordType> InitInternalAsync(PowerFxConfig config, string tableName, BaseRuntimeConnectorContext runtimeConnectorContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ConnectorSettings connectorSettings = new ConnectorSettings(Namespace)
            {
                IncludeInternalFunctions = true,
                Compatibility = ConnectorCompatibility.SwaggerCompatibility
            };

            // Swagger based tabular connectors
            _tabularFunctions = config.AddActionConnector(connectorSettings, _openApiDocument, _connectorLogger, _globalValues);

            return await GetSchemaAsync(new SimpleRuntimeConnectorContext(_httpClient), cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<RecordType> GetSchemaAsync(BaseRuntimeConnectorContext runtimeConnectorContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FormulaValue schema = await MetadataService.InvokeAsync(Array.Empty<FormulaValue>(), runtimeConnectorContext.WithRawResults(), cancellationToken).ConfigureAwait(false);
            ConnectorType connectorType = ConnectorFunction.GetConnectorType("Schema/Items", schema as StringValue, ConnectorCompatibility.SwaggerCompatibility);

            return connectorType?.FormulaType as RecordType;
        }

        public override async Task<IEnumerable<DValue<RecordValue>>> GetItemsAsync(BaseRuntimeConnectorContext runtimeConnectorContext, CancellationToken cancellationToken)
        {
            // Notice that there is no paging here, just get 1 page
            // Use WithRawResults to ignore _getItems return type which is in the form of ![value:*[dynamicProperties:![]]] (ie. without the actual type)
            FormulaValue rowsRaw = await GetItems.InvokeAsync(Array.Empty<FormulaValue>(), runtimeConnectorContext.WithRawResults(), CancellationToken.None).ConfigureAwait(false);

            if (rowsRaw is ErrorValue ev)
            {
                return Enumerable.Empty<DValue<RecordValue>>();
            }

            StringValue rowsStr = rowsRaw as StringValue;

            // $$$ Is this always this type?
            RecordValue rv = FormulaValueJSON.FromJson(rowsStr.Value, RecordType.Empty().Add("value", TableType)) as RecordValue;
            TableValue tv = rv.Fields.FirstOrDefault(field => field.Name == "value").Value as TableValue;

            // The call we make contains more fields and we want to remove them here ('@odata.etag')
            return new InMemoryTableValue(IRContext.NotInSource(TableType), tv.Rows).Rows;
        }

        private const string MetadataServiceRegex = @"/\$metadata\.json/datasets/{[^{}]+}(,{[^{}]+})?/tables/{[^{}]+}$";
        private const string GetItemsRegex = @"/datasets/{[^{}]+}(,{[^{}]+})?/tables/{[^{}]+}/items$";
        private const string NameVersionRegex = @"V(?<n>[0-9]{0,2})$";

        private ConnectorFunction GetMetadataService()
        {
            ConnectorFunction[] functions = _tabularFunctions.Where(tf => tf.RequiredParameters.Length == 0 && new Regex(MetadataServiceRegex).IsMatch(tf.OperationPath)).ToArray();

            if (functions.Length == 0)
            {
                throw new PowerFxConnectorException("Cannot determine metadata service function.");
            }

            if (functions.Length > 1)
            {
                // When GetTableTabularV2, GetTableTabular exist, return highest version
                return functions[functions.Select((cf, i) => (index: i, version: int.Parse("0" + new Regex(NameVersionRegex).Match(cf.Name).Groups["n"].Value, CultureInfo.InvariantCulture))).OrderByDescending(x => x.version).First().index];
            }

            return functions[0];
        }

        private ConnectorFunction GetItemsFunction()
        {
            ConnectorFunction[] functions = _tabularFunctions.Where(tf => tf.RequiredParameters.Length == 0 && new Regex(GetItemsRegex).IsMatch(tf.OperationPath)).ToArray();

            if (functions.Length == 0)
            {
                throw new PowerFxConnectorException("Cannot determine GetItems function.");
            }

            if (functions.Length > 1)
            {
                throw new PowerFxConnectorException("Multiple GetItems functions found.");
            }

            return functions[0];
        }

        private class SimpleRuntimeConnectorContext : BaseRuntimeConnectorContext
        {
            private readonly HttpMessageInvoker _invoker;

            internal SimpleRuntimeConnectorContext(HttpMessageInvoker invoker)
            {
                _invoker = invoker;
            }

            public override TimeZoneInfo TimeZoneInfo => TimeZoneInfo.Utc;

            public override HttpMessageInvoker GetInvoker(string @namespace) => _invoker;
        }
    }
}
