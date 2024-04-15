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
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Swagger based CDP tabular service
    public sealed class SwaggerTabularService : CdpTabularService
    {
        public string Namespace => $"_tbl_{ConnectionId}";

        public string ConnectionId => _connectionId;

        private readonly string _connectionId;
        private IReadOnlyList<ConnectorFunction> _tabularFunctions;
        private ConnectorFunction _metadataService;
        private ConnectorFunction _createItems;
        private ConnectorFunction _getItems;
        private ConnectorFunction _updateItem;
        private ConnectorFunction _deleteItem;
        private readonly IReadOnlyDictionary<string, FormulaValue> _globalValues;
        private readonly bool _defaultDataset = false;

        public SwaggerTabularService(IReadOnlyDictionary<string, FormulaValue> globalValues)
            : base(GetDataSetName(globalValues, out bool useDefaultDataset), GetTableName(globalValues))
        {
            _globalValues = globalValues;
            _defaultDataset = useDefaultDataset;
            _connectionId = TryGetString("connectionId", globalValues, out string connectorId, out _) ? connectorId : throw new InvalidOperationException("Cannot determine connectionId.");
        }

        public static bool IsTabular(IReadOnlyDictionary<string, FormulaValue> globalValues, OpenApiDocument openApiDocument, out string error)
        {
            try
            {
                error = null;
                return new SwaggerTabularService(globalValues).LoadSwaggerAndIdentifyKeyMethods(new PowerFxConfig(), openApiDocument);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // Only used for testing
        internal static (ConnectorFunction s, ConnectorFunction c, ConnectorFunction r, ConnectorFunction u, ConnectorFunction d) GetFunctions(IReadOnlyDictionary<string, FormulaValue> globalValues, OpenApiDocument openApiDocument)
        {
            return new SwaggerTabularService(globalValues).GetFunctions(new PowerFxConfig(), openApiDocument);
        }

        /* Schema [GET] */

        private const string MetadataServiceRegex = @"/\$metadata\.json/datasets/({[^{}]+}(,{[^{}]+})?|default)/tables/{[^{}]+}$";

        /* Create [POST], Read [GET] */

        private const string CreateOrGetItemsRegex = @"/datasets/({[^{}]+}(,{[^{}]+})?|default)/tables/{[^{}]+}/items$";

        /* Read [GET], Update [PATCH], Delete [DELETE] */

        private const string GetUpdateOrDeleteItemRegex = @"/datasets/({[^{}]+}(,{[^{}]+})?|default)/tables/{[^{}]+}/items/{[^{}]+}$";

        /* Version (like V2) */

        private const string NameVersionRegex = @"V(?<n>[0-9]{0,2})$";

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        internal ConnectorFunction MetadataService => _metadataService ??= GetFunction(HttpMethod.Get, MetadataServiceRegex, 0, "metadata service");

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01
        internal ConnectorFunction CreateItems => _createItems ??= GetFunction(HttpMethod.Post, CreateOrGetItemsRegex, 1, "Create Items");

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01
        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        internal ConnectorFunction GetItems => _getItems ??= GetFunction(HttpMethod.Get, CreateOrGetItemsRegex, 0, "Get Items");

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}
        internal ConnectorFunction UpdateItem => _updateItem ??= GetFunction(new HttpMethod("PATCH"), GetUpdateOrDeleteItemRegex, 2, "Update Item");

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}
        internal ConnectorFunction DeleteItem => _deleteItem ??= GetFunction(new HttpMethod("DELETE"), GetUpdateOrDeleteItemRegex, 1, "Delete Item");

        public override Task InitAsync(HttpClient httpClient, string uriPrefix, bool useV2, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            throw new PowerFxConnectorException("Use InitAsync with OpenApiDocument");
        }

        public async Task InitAsync(PowerFxConfig config, OpenApiDocument openApiDocument, HttpClient httpClient, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger?.LogInformation($"Entering in {nameof(SwaggerTabularService)} {nameof(InitAsync)} for {DataSetName}, {TableName}");

                if (!LoadSwaggerAndIdentifyKeyMethods(config, openApiDocument, logger))
                {
                    throw new PowerFxConnectorException("Cannot identify tabular methods.");
                }

                BaseRuntimeConnectorContext runtimeConnectorContext = new RawRuntimeConnectorContext(httpClient);
                FormulaValue schema = await MetadataService.InvokeAsync(Array.Empty<FormulaValue>(), runtimeConnectorContext, cancellationToken).ConfigureAwait(false);

                logger?.LogInformation($"Exiting {nameof(SwaggerTabularService)} {nameof(InitAsync)} for {DataSetName}, {TableName} {(schema is ErrorValue ev ? string.Join(", ", ev.Errors.Select(er => er.Message)) : string.Empty)}");

                if (schema is StringValue str)
                {
                    SetTableType(GetSchema(str.Value));
                }
            }
            catch (Exception ex)
            {
                logger?.LogException(ex, $"Exception in {nameof(SwaggerTabularService)} {nameof(InitAsync)} for {DataSetName}, {TableName}, {ConnectorHelperFunctions.LogException(ex)}");
                throw;
            }
        }

        internal bool LoadSwaggerAndIdentifyKeyMethods(PowerFxConfig config, OpenApiDocument openApiDocument, ConnectorLogger logger = null)
        {
            var (s, c, r, u, d) = GetFunctions(config, openApiDocument, logger);
            return s != null && c != null && r != null && u != null && d != null;
        }

        internal (ConnectorFunction s, ConnectorFunction c, ConnectorFunction r, ConnectorFunction u, ConnectorFunction d) GetFunctions(PowerFxConfig config, OpenApiDocument openApiDocument, ConnectorLogger logger = null)
        {
            ConnectorSettings connectorSettings = new ConnectorSettings(Namespace)
            {
                IncludeInternalFunctions = true,
                Compatibility = ConnectorCompatibility.SwaggerCompatibility
            };

            // Swagger based tabular connectors
            _tabularFunctions = config.AddActionConnector(connectorSettings, openApiDocument, _globalValues, logger);

            return (MetadataService, CreateItems, GetItems, UpdateItem, DeleteItem);
        }

        protected override async Task<ICollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, ODataParameters oDataParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectorLogger executionLogger = serviceProvider?.GetService<ConnectorLogger>();

            try
            {
                executionLogger?.LogInformation($"Entering in {nameof(SwaggerTabularService)} {nameof(GetItemsAsync)} for {DataSetName}, {TableName}");

                BaseRuntimeConnectorContext runtimeConnectorContext = serviceProvider.GetService<BaseRuntimeConnectorContext>() ?? throw new InvalidOperationException("Cannot determine runtime connector context.");                
                IReadOnlyList<NamedValue> optionalParameters = oDataParameters != null ? oDataParameters.GetNamedValues() : Array.Empty<NamedValue>();

                FormulaValue[] parameters = optionalParameters.Any() ? new FormulaValue[] { FormulaValue.NewRecordFromFields(optionalParameters.ToArray()) } : Array.Empty<FormulaValue>();

                // Notice that there is no paging here, just get 1 page
                // Use WithRawResults to ignore _getItems return type which is in the form of ![value:*[dynamicProperties:![]]] (ie. without the actual type)
                FormulaValue rowsRaw = await GetItems.InvokeAsync(parameters, runtimeConnectorContext.WithRawResults(), CancellationToken.None).ConfigureAwait(false);

                executionLogger?.LogInformation($"Exiting {nameof(SwaggerTabularService)} {nameof(GetItemsAsync)} for {DataSetName}, {TableName} {(rowsRaw is ErrorValue ev ? string.Join(", ", ev.Errors.Select(er => er.Message)) : string.Empty)}");
                return rowsRaw is StringValue sv ? GetResult(sv.Value) : Array.Empty<DValue<RecordValue>>();
            }
            catch (Exception ex)
            {
                executionLogger?.LogException(ex, $"Exception in {nameof(SwaggerTabularService)} {nameof(GetItemsAsync)} for {DataSetName}, {TableName}, {ConnectorHelperFunctions.LogException(ex)}");
                throw;
            }
        }

        private static string GetDataSetName(IReadOnlyDictionary<string, FormulaValue> globalValues, out bool useDefaultDataset)
        {
            bool b1 = TryGetString("server", globalValues, out string server, out _);
            bool b2 = TryGetString("database", globalValues, out string database, out _);

            if (b1 && b2)
            {
                useDefaultDataset = false;
                return $"{server},{database}";
            }

            return TryGetString("dataset", globalValues, out string dataset, out useDefaultDataset, true)
                    ? dataset
                    : throw new InvalidOperationException("Cannot determine dataset name.");
        }

        private static string GetTableName(IReadOnlyDictionary<string, FormulaValue> globalValues) =>
            TryGetString("table", globalValues, out string table, out _)
            ? table
            : throw new InvalidOperationException("Cannot determine table name.");

        private static bool TryGetString(string name, IReadOnlyDictionary<string, FormulaValue> globalValues, out string str, out bool useDefaultDataset, bool allowDefault = false)
        {
            useDefaultDataset = false;

            if (globalValues.TryGetValue(name, out FormulaValue fv) && fv is StringValue sv)
            {
                str = sv.Value;
                return !string.IsNullOrEmpty(str);
            }

            if (allowDefault)
            {
                useDefaultDataset = true;
                str = "default";
                return true;
            }

            str = null;
            return false;
        }

        private ConnectorFunction GetFunction(HttpMethod httpMethod, string regex, int numArgs, string log)
        {
            if (_tabularFunctions == null)
            {
                throw new PowerFxConnectorException("Tabular functions are not initialized.");
            }

            ConnectorFunction[] functions = _tabularFunctions.Where(tf => tf.HttpMethod == httpMethod && tf.RequiredParameters.Length == numArgs && new Regex(regex).IsMatch(tf.OperationPath)).ToArray();

            if (functions.Length == 0)
            {
                if (_defaultDataset)
                {
                    throw new PowerFxConnectorException($"Cannot determine {log} function. 'dataset' value probably missing.");
                }

                throw new PowerFxConnectorException($"Cannot determine {log} function.");
            }

            if (functions.Length > 1)
            {
                // When GetTableTabularV2, GetTableTabular exist, return highest version
                return functions[functions.Select((cf, i) => (index: i, version: int.Parse("0" + new Regex(NameVersionRegex).Match(cf.Name).Groups["n"].Value, CultureInfo.InvariantCulture))).OrderByDescending(x => x.version).First().index];
            }

            return functions[0];
        }

        private class RawRuntimeConnectorContext : BaseRuntimeConnectorContext
        {
            private readonly HttpMessageInvoker _invoker;

            internal RawRuntimeConnectorContext(HttpMessageInvoker invoker)
            {
                _invoker = invoker;
            }

            internal override bool ReturnRawResults => true;

            public override TimeZoneInfo TimeZoneInfo => TimeZoneInfo.Utc;

            public override HttpMessageInvoker GetInvoker(string @namespace) => _invoker;
        }
    }
}
