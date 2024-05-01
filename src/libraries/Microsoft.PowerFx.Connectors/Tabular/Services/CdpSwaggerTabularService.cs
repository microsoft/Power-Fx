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

namespace Microsoft.PowerFx.Connectors.Tabular
{
    // Swagger based CDP tabular service
    public sealed class CdpSwaggerTabularService : CdpTabularService
    {
        public string Namespace => $"_tbl_{ConnectionId}";

        public string ConnectionId => _connectionId;

        private readonly string _connectionId;
        private readonly TabularFunctionIdentification _tabularFunctionIdentification;
        private IReadOnlyList<ConnectorFunction> _tabularFunctions;
        private readonly OpenApiDocument _openApiDocument;
        private readonly IReadOnlyDictionary<string, FormulaValue> _globalValues;        

        public CdpSwaggerTabularService(OpenApiDocument openApiDocument, IReadOnlyDictionary<string, FormulaValue> globalValues)
            : base()
        {
            _globalValues = globalValues;
            _openApiDocument = openApiDocument;
            _tabularFunctionIdentification = GetTabularFunctions(openApiDocument);

            foreach (string mandatoryGlobalValue in _tabularFunctionIdentification.GlobalValueNames)
            {
                if (!globalValues.ContainsKey(mandatoryGlobalValue))
                {
                    throw new InvalidOperationException($"Missing global value {mandatoryGlobalValue}");
                }
            }

            _connectionId = GetString("connectionId", globalValues);

            DatasetName = _tabularFunctionIdentification.DatasetNames.Count == 0 ? string.Join(",", _tabularFunctionIdentification.DatasetNames.Select(dsn => GetString(dsn, globalValues))) : _tabularFunctionIdentification.DefaultDatasetName;
            TableName = GetString(_tabularFunctionIdentification.TableName, globalValues);
        }

        internal static TabularFunctionIdentification GetTabularFunctions(OpenApiDocument openApiDocument)
        {
            List<SwaggerOperation> swaggerOperations = openApiDocument.Paths.SelectMany((KeyValuePair<string, OpenApiPathItem> path) => 
                        path.Value.Operations.Where((KeyValuePair<OperationType, OpenApiOperation> operation) => !operation.Value.Deprecated)
                                             .Select((KeyValuePair<OperationType, OpenApiOperation> operation) => new SwaggerOperation() { OperationType = operation.Key, OperationPath = path.Key, Operation = operation.Value })).ToList();

            // Metadata Service
            List<SwaggerOperation> metadataServicePotentialOps = new List<SwaggerOperation>();
            foreach (SwaggerOperation swop in swaggerOperations.Where(o => o.OperationType == OperationType.Get))
            {
                Match m = new Regex(@"(/v(?<v>[0-9]{1,2}))?/\$metadata\.json(?<suffix>/datasets/(?<ds>[^/]+)/tables/{(?<tab>[^/]+)})$").Match(swop.OperationPath);

                if (m.Success)
                {
                    string v = m.Groups["v"].Value;
                    int i = string.IsNullOrEmpty(v) ? 1 : int.Parse(v, CultureInfo.InvariantCulture);
                    swop.Version = i;

                    string ds = m.Groups["ds"].Value;
                    MatchCollection m2 = new Regex(@"{(?<var>[^{}]+)}").Matches(ds);
                    if (m2.Count == 0)
                    {
                        swop.UseDefaultDataset = true;
                        swop.DefaultDatasetName = ds;
                        swop.DatasetNames = null;
                    }
                    else
                    {
                        swop.UseDefaultDataset = false;
                        swop.DefaultDatasetName = null;
                        swop.DatasetNames = m2.Cast<Match>().Select(im => im.Groups["var"].Value).ToArray();
                    }

                    swop.TableName = m.Groups["tab"].Value;
                    swop.ServiceBase = m.Groups["suffix"].Value;

                    metadataServicePotentialOps.Add(swop);
                }
            }

            SwaggerOperation metadataService = GetMaxOperation(metadataServicePotentialOps, "metadata service");

            TabularFunctionIdentification tf = new TabularFunctionIdentification
            {
                MetadataServiceOpPath = metadataService.OperationPath,
                MetadataServiceOpName = OpenApiHelperFunctions.NormalizeOperationId(metadataService.Operation.OperationId),
                GlobalValueNames = new List<string>() { "connectionId" }
            };

            if (metadataService.UseDefaultDataset)
            {
                tf.DatasetNames = new List<string>();
                tf.DefaultDatasetName = metadataService.DefaultDatasetName;
            }
            else
            {
                tf.DatasetNames = metadataService.DatasetNames.ToList();
                tf.DefaultDatasetName = null;
                tf.GlobalValueNames.AddRange(metadataService.DatasetNames);
            }

            tf.TableName = metadataService.TableName;
            tf.GlobalValueNames.Add(metadataService.TableName);

            string serviceBase = metadataService.ServiceBase;

            // Create
            List<SwaggerOperation> createPotentialOps = new List<SwaggerOperation>();
            foreach (SwaggerOperation swop in swaggerOperations.Where(o => o.OperationType == OperationType.Post))
            {
                Match m = new Regex(@$"(/v(?<v>[0-9]{{1,2}}))?{EscapeRegex(serviceBase)}/items$").Match(swop.OperationPath);

                if (m.Success)
                {
                    string v = m.Groups["v"].Value;
                    int i = string.IsNullOrEmpty(v) ? 1 : int.Parse(v, CultureInfo.InvariantCulture);
                    swop.Version = i;

                    createPotentialOps.Add(swop);
                }
            }

            SwaggerOperation createOperation = GetMaxOperation(createPotentialOps, "create");

            tf.CreateOpPath = createOperation.OperationPath;
            tf.CreateOpName = OpenApiHelperFunctions.NormalizeOperationId(createOperation.Operation.OperationId);

            // Read - GetItems
            List<SwaggerOperation> getItemsPotentialOps = new List<SwaggerOperation>();
            foreach (SwaggerOperation swop in swaggerOperations.Where(o => o.OperationType == OperationType.Get))
            {
                Match m = new Regex(@$"(/v(?<v>[0-9]{{1,2}}))?{EscapeRegex(serviceBase)}/items$").Match(swop.OperationPath);

                if (m.Success)
                {
                    string v = m.Groups["v"].Value;
                    int i = string.IsNullOrEmpty(v) ? 1 : int.Parse(v, CultureInfo.InvariantCulture);
                    swop.Version = i;

                    getItemsPotentialOps.Add(swop);
                }
            }

            SwaggerOperation getItemsOperation = GetMaxOperation(getItemsPotentialOps, "get items");

            tf.GetItemsOpPath = getItemsOperation.OperationPath;
            tf.GetItemsOpName = OpenApiHelperFunctions.NormalizeOperationId(getItemsOperation.Operation.OperationId);

            // Update
            List<SwaggerOperation> updatePorentialOps = new List<SwaggerOperation>();
            foreach (SwaggerOperation swop in swaggerOperations.Where(o => o.OperationType == OperationType.Patch))
            {
                Match m = new Regex(@$"(/v(?<v>[0-9]{{1,2}}))?{EscapeRegex(serviceBase)}/items/{{(?<item>[^/]+)}}$").Match(swop.OperationPath);

                if (m.Success)
                {
                    string v = m.Groups["v"].Value;
                    int i = string.IsNullOrEmpty(v) ? 1 : int.Parse(v, CultureInfo.InvariantCulture);
                    swop.Version = i;

                    swop.ItemName = m.Groups["item"].Value;

                    updatePorentialOps.Add(swop);
                }
            }

            SwaggerOperation updateOperation = GetMaxOperation(updatePorentialOps, "update item");

            tf.UpdateOpPath = updateOperation.OperationPath;
            tf.UpdateOpName = OpenApiHelperFunctions.NormalizeOperationId(updateOperation.Operation.OperationId);
            tf.ItemName = updateOperation.ItemName;

            // Delete
            List<SwaggerOperation> deletePorentialOps = new List<SwaggerOperation>();
            foreach (SwaggerOperation swop in swaggerOperations.Where(o => o.OperationType == OperationType.Delete))
            {
                Match m = new Regex(@$"(/v(?<v>[0-9]{{1,2}}))?{EscapeRegex(serviceBase)}/items/{{(?<item>[^/]+)}}$").Match(swop.OperationPath);

                if (m.Success)
                {
                    string v = m.Groups["v"].Value;
                    int i = string.IsNullOrEmpty(v) ? 1 : int.Parse(v, CultureInfo.InvariantCulture);
                    swop.Version = i;

                    if (m.Groups["item"].Value == updateOperation.ItemName)
                    {
                        deletePorentialOps.Add(swop);
                    }
                }
            }

            SwaggerOperation deleteOperation = GetMaxOperation(deletePorentialOps, "delete item");

            tf.DeleteOpPath = deleteOperation.OperationPath;
            tf.DeleteOpName = OpenApiHelperFunctions.NormalizeOperationId(deleteOperation.Operation.OperationId);

            return tf;
        }

        internal class SwaggerOperation
        {
            public string ServiceBase;
            public OperationType OperationType;
            public string OperationPath;
            public int Version;
            public bool UseDefaultDataset;
            public string DefaultDatasetName;
            public string[] DatasetNames;
            public string TableName;
            public string ItemName;

            internal OpenApiOperation Operation;
        }

        internal class TabularFunctionIdentification
        {
            public List<string> GlobalValueNames;
            public string MetadataServiceOpPath;
            public string MetadataServiceOpName;
            public List<string> DatasetNames;
            public string DefaultDatasetName;
            public string TableName;
            public string CreateOpPath;
            public string CreateOpName;
            public string GetItemsOpPath;
            public string GetItemsOpName;
            public string UpdateOpPath;
            public string UpdateOpName;
            public string DeleteOpPath;
            public string DeleteOpName;
            public string ItemName;
        }

        private static string EscapeRegex(string rex) => rex.Replace("{", @"\{").Replace("}", @"\}");

        private static SwaggerOperation GetMaxOperation(List<SwaggerOperation> potentialOperations, string type)
        {
            if (potentialOperations == null || potentialOperations.Count == 0)
            {
                throw new Exception($"Unsupported swagger file, cannot identify {type}, function not found");
            }

            int maxVer = potentialOperations.Max(o => o.Version);
            IEnumerable<SwaggerOperation> maxOperations = potentialOperations.Where(o => o.Version == maxVer);

            if (maxOperations.Count() != 1)
            {
                throw new Exception($"Unsupported swagger file, cannot identify unique {type}");
            }

            return maxOperations.First();
        }

        public static string[] GetGlobalValueNames(OpenApiDocument openApiDocument)
        {
            return GetTabularFunctions(openApiDocument).GlobalValueNames.ToArray();
        }

        public static bool IsTabular(IReadOnlyDictionary<string, FormulaValue> globalValues, OpenApiDocument openApiDocument, out string error)
        {
            try
            {
                error = null;
                return new CdpSwaggerTabularService(openApiDocument, globalValues).LoadSwaggerAndIdentifyKeyMethods(new PowerFxConfig());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        [Obsolete("Only used for testing")]
        internal static (ConnectorFunction s, ConnectorFunction c, ConnectorFunction r, ConnectorFunction u, ConnectorFunction d) GetFunctions(IReadOnlyDictionary<string, FormulaValue> globalValues, OpenApiDocument openApiDocument)
        {
            return new CdpSwaggerTabularService(openApiDocument, globalValues).GetFunctions(new PowerFxConfig());
        }

        public async Task InitAsync(PowerFxConfig config, HttpClient httpClient, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger?.LogInformation($"Entering in {nameof(CdpSwaggerTabularService)} {nameof(InitAsync)} for {DatasetName}, {TableName}");

                if (!LoadSwaggerAndIdentifyKeyMethods(config, logger))
                {
                    throw new PowerFxConnectorException("Cannot identify tabular methods.");
                }

                BaseRuntimeConnectorContext runtimeConnectorContext = new RawRuntimeConnectorContext(httpClient);
                FormulaValue schema = await MetadataService.InvokeAsync(Array.Empty<FormulaValue>(), runtimeConnectorContext, cancellationToken).ConfigureAwait(false);

                logger?.LogInformation($"Exiting {nameof(CdpSwaggerTabularService)} {nameof(InitAsync)} for {DatasetName}, {TableName} {(schema is ErrorValue ev ? string.Join(", ", ev.Errors.Select(er => er.Message)) : string.Empty)}");

                if (schema is StringValue str)
                {
                    SetTableType(GetSchema(str.Value));
                }
            }
            catch (Exception ex)
            {
                logger?.LogException(ex, $"Exception in {nameof(CdpSwaggerTabularService)} {nameof(InitAsync)} for {DatasetName}, {TableName}, {ConnectorHelperFunctions.LogException(ex)}");
                throw;
            }
        }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        internal ConnectorFunction MetadataService;

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01
        internal ConnectorFunction CreateItems;

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01
        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        internal ConnectorFunction GetItems;

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}
        internal ConnectorFunction UpdateItem;

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}
        internal ConnectorFunction DeleteItem;

        public override Task InitAsync(HttpClient httpClient, string uriPrefix, bool useV2, CancellationToken cancellationToken, ConnectorLogger logger = null)
        {
            throw new PowerFxConnectorException("Use InitAsync with OpenApiDocument");
        }

        internal bool LoadSwaggerAndIdentifyKeyMethods(PowerFxConfig config, ConnectorLogger logger = null)
        {
            var (s, c, r, u, d) = GetFunctions(config, logger);
            return s != null && c != null && r != null && u != null && d != null;
        }

        internal (ConnectorFunction s, ConnectorFunction c, ConnectorFunction r, ConnectorFunction u, ConnectorFunction d) GetFunctions(PowerFxConfig config, ConnectorLogger logger = null)
        {
            ConnectorSettings connectorSettings = new ConnectorSettings(Namespace)
            {
                IncludeInternalFunctions = true,
                Compatibility = ConnectorCompatibility.SwaggerCompatibility
            };

            // Swagger based tabular connectors
            _tabularFunctions = config.AddActionConnector(connectorSettings, _openApiDocument, _globalValues, logger);

            MetadataService = _tabularFunctions.FirstOrDefault(tf => tf.Name == _tabularFunctionIdentification.MetadataServiceOpName) ?? throw new PowerFxConnectorException("Metadata service not found.");
            CreateItems = _tabularFunctions.FirstOrDefault(tf => tf.Name == _tabularFunctionIdentification.CreateOpName) ?? throw new PowerFxConnectorException("Create items service not found.");
            GetItems = _tabularFunctions.FirstOrDefault(tf => tf.Name == _tabularFunctionIdentification.GetItemsOpName) ?? throw new PowerFxConnectorException("Get items service not found.");
            UpdateItem = _tabularFunctions.FirstOrDefault(tf => tf.Name == _tabularFunctionIdentification.UpdateOpName) ?? throw new PowerFxConnectorException("Update item service not found.");
            DeleteItem = _tabularFunctions.FirstOrDefault(tf => tf.Name == _tabularFunctionIdentification.DeleteOpName) ?? throw new PowerFxConnectorException("Delete item service not found.");

            return (MetadataService, CreateItems, GetItems, UpdateItem, DeleteItem);
        }

        protected override async Task<IReadOnlyCollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, ODataParameters oDataParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectorLogger executionLogger = serviceProvider?.GetService<ConnectorLogger>();

            try
            {
                executionLogger?.LogInformation($"Entering in {nameof(CdpSwaggerTabularService)} {nameof(GetItemsAsync)} for {DatasetName}, {TableName}");

                BaseRuntimeConnectorContext runtimeConnectorContext = serviceProvider.GetService<BaseRuntimeConnectorContext>() ?? throw new InvalidOperationException("Cannot determine runtime connector context.");
                IReadOnlyList<NamedValue> optionalParameters = oDataParameters != null ? oDataParameters.GetNamedValues() : Array.Empty<NamedValue>();

                FormulaValue[] parameters = optionalParameters.Any() ? new FormulaValue[] { FormulaValue.NewRecordFromFields(optionalParameters.ToArray()) } : Array.Empty<FormulaValue>();

                // Notice that there is no paging here, just get 1 page
                // Use WithRawResults to ignore _getItems return type which is in the form of ![value:*[dynamicProperties:![]]] (ie. without the actual type)
                FormulaValue rowsRaw = await GetItems.InvokeAsync(parameters, runtimeConnectorContext.WithRawResults(), CancellationToken.None).ConfigureAwait(false);

                executionLogger?.LogInformation($"Exiting {nameof(CdpSwaggerTabularService)} {nameof(GetItemsAsync)} for {DatasetName}, {TableName} {(rowsRaw is ErrorValue ev ? string.Join(", ", ev.Errors.Select(er => er.Message)) : string.Empty)}");
                return rowsRaw is StringValue sv ? GetResult(sv.Value) : Array.Empty<DValue<RecordValue>>();
            }
            catch (Exception ex)
            {
                executionLogger?.LogException(ex, $"Exception in {nameof(CdpSwaggerTabularService)} {nameof(GetItemsAsync)} for {DatasetName}, {TableName}, {ConnectorHelperFunctions.LogException(ex)}");
                throw;
            }
        }

        private static bool TryGetString(string name, IReadOnlyDictionary<string, FormulaValue> globalValues, out string str)
        {            
            if (globalValues.TryGetValue(name, out FormulaValue fv) && fv is StringValue sv)
            {
                str = sv.Value;
                return !string.IsNullOrEmpty(str);
            }

            str = null;
            return false;
        }

        private static string GetString(string name, IReadOnlyDictionary<string, FormulaValue> globalValues)
        {
            return TryGetString(name, globalValues, out string str) ? str : throw new InvalidOperationException($"Cannot determine {name} in global values.");
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
