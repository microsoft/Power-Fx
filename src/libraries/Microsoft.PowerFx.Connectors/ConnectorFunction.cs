// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Represents a connector function.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public class ConnectorFunction
    {
        /// <summary>
        /// Normalized name of the function.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Namespace of the function (not contained in swagger file).
        /// </summary>
        public string Namespace => ConnectorSettings.Namespace;

        /// <summary>
        /// Numbers are defined as "double" type (otherwise "decimal").
        /// </summary>        
#pragma warning disable CS0618 // Type or member is obsolete
        public bool NumberIsFloat => ConnectorSettings.NumberIsFloat;
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// ConnectorSettings. Contains the Namespace and other parameters.
        /// </summary>
        public ConnectorSettings ConnectorSettings { get; }

        /// <summary>
        /// Defines if the function is supported or contains unsupported elements.
        /// </summary>
        public bool IsSupported
        {
            get
            {
                EnsureInitialized();
                return _isSupported;
            }
        }

        /// <summary>
        /// Reason for which the function isn't supported.
        /// </summary>
        public string NotSupportedReason
        {
            get
            {
                EnsureInitialized();
                return _notSupportedReason;
            }
        }

        internal bool Filtered { get; }

        /// <summary>
        /// Defines if the function is deprecated.
        /// </summary>
        public bool IsDeprecated => Operation.Deprecated;

        /// <summary>
        /// Page Link as defined in the x-ms-pageable extension.
        /// This is the name of the property that will host the URL, not the URL itself.
        /// </summary>
        public string PageLink => Operation.PageLink();

        /// <summary>
        /// Name as it appears in the swagger file.
        /// </summary>
        public string OriginalName => Operation.OperationId;

        /// <summary>
        /// Operation's description.
        /// </summary>
        public string Description => Operation.Description ?? $"Invoke {Name}";

        /// <summary>
        /// Operation's summary.
        /// </summary>
        public string Summary => Operation.Summary;

        /// <summary>
        /// Operation's path. Includes the base path if any.
        /// </summary>
        public string OperationPath { get; }

        /// <summary>
        /// HTTP method.
        /// </summary>
        public HttpMethod HttpMethod { get; }

        /// <summary>
        /// Defines behavioral functions.
        /// HTTP/1.1 spec states that only GET and HEAD requests are 'safe' by default.
        /// https://www.w3.org/Protocols/rfc2616/rfc2616-sec9.html.
        /// </summary>
        public bool IsBehavior => HttpMethod != HttpMethod.Get && HttpMethod != HttpMethod.Head;

        /// <summary>
        /// Defines if the function is pageable (using x-ms-pageable extension).
        /// </summary>
        public bool IsPageable => !string.IsNullOrEmpty(PageLink);

        /// <summary>
        /// Visibility defined as "x-ms-visibility" string content.
        /// </summary>
        public string Visibility => Operation.GetVisibility();

        /// <summary>
        /// When "x-ms-visibility" is set to "internal".
        /// https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-visibility.
        /// </summary>
        public bool IsInternal => Operation.IsInternal();

        /// <summary>
        /// Defined as "x-ms-require-user-confirmation" boolean content.
        /// </summary>
        public bool RequiresUserConfirmation => Operation.GetRequiresUserConfirmation();

        /// <summary>
        /// Return type of the function.
        /// </summary>
        public FormulaType ReturnType => Operation.GetReturnType(NumberIsFloat);

        /// <summary>
        /// Connector return type of the function (contains inner "x-ms-summary" and descriptions).
        /// </summary>
        internal ConnectorType ConnectorReturnType => Operation.GetConnectorReturnType(NumberIsFloat).ConnectorType;

        /// <summary>
        /// Minimum number of arguments.
        /// </summary>
        public int ArityMin
        {
            get
            {
                EnsureInitialized();
                return _arityMin;
            }
        }

        /// <summary>
        /// Maximum number of arguments.
        /// </summary>
        public int ArityMax
        {
            get
            {
                EnsureInitialized();
                return _arityMax;
            }
        }

        /// <summary>
        /// Required parameters.
        /// </summary>
        public ConnectorParameter[] RequiredParameters
        {
            get
            {
                EnsureInitialized();
                return _requiredParameters;
            }
        }

        /// <summary>
        /// Optional parameters.
        /// </summary>
        public ConnectorParameter[] OptionalParameters
        {
            get
            {
                EnsureInitialized();
                return _optionalParameters;
            }
        }

        /// <summary>
        /// Swagger's operation.
        /// </summary>
        internal OpenApiOperation Operation { get; }

        /// <summary>
        /// Hidden required parameters.
        /// Defined as 
        /// - part "required" swagger array
        /// - "x-ms-visibility" string set to "internal" 
        /// - has a default value.
        /// </summary>
        internal ConnectorParameter[] HiddenRequiredParameters
        {
            get
            {
                EnsureInitialized();
                return _hiddenRequiredParameters;
            }
        }

        /// <summary>
        /// OpenApiServers defined in the document.
        /// </summary>
        internal IEnumerable<OpenApiServer> Servers { get; init; }

        /// <summary>
        /// Dynamic schema extension on return type (response).
        /// </summary>
        internal ConnectorDynamicSchema DynamicReturnSchema => EnsureConnectorFunction(ReturnParameterType?.DynamicSchema, GlobalContext.FunctionList);

        /// <summary>
        /// Dynamic schema extension on return type (response).
        /// </summary>
        internal ConnectorDynamicProperty DynamicReturnProperty => EnsureConnectorFunction(ReturnParameterType?.DynamicProperty, GlobalContext.FunctionList);

        /// <summary>
        /// Return type when determined at runtime/by dynamic intellisense.
        /// </summary>
        public ConnectorType ReturnParameterType
        {
            get
            {
                EnsureInitialized();
                return _returnParameterType;
            }
        }

        /// <summary>
        /// Parameter types used for TexlFunction.
        /// </summary>
        internal DType[] ParameterTypes => _parameterTypes ?? GetParamTypes();

        private DType[] _parameterTypes;

        internal ConnectorGlobalContext GlobalContext { get; }

        // Those parameters are protected by EnsureInitialized
        private int _arityMin;
        private int _arityMax;
        private ConnectorParameter[] _requiredParameters;
        private ConnectorParameter[] _hiddenRequiredParameters;
        private ConnectorParameter[] _optionalParameters;
        private ConnectorType _returnParameterType;
        private bool _isSupported;
        private string _notSupportedReason;

        // Those properties are only used by HttpFunctionInvoker
        internal ConnectorParameterInternals _internals = null;

        internal ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, ConnectorSettings connectorSettings, List<ConnectorFunction> functionList, IReadOnlyDictionary<string, FormulaValue> namedValues)
        {
            Operation = openApiOperation ?? throw new ArgumentNullException(nameof(openApiOperation));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OperationPath = operationPath ?? throw new ArgumentNullException(nameof(operationPath));
            HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
            ConnectorSettings = connectorSettings;
            GlobalContext = new ConnectorGlobalContext(functionList ?? throw new ArgumentNullException(nameof(functionList)), namedValues);

            _isSupported = isSupported || connectorSettings.AllowUnsupportedFunctions;
            _notSupportedReason = notSupportedReason ?? (isSupported ? string.Empty : throw new ArgumentNullException(nameof(notSupportedReason)));

            int nvCount = namedValues?.Count(nv => nv.Key != "connectionId") ?? 0;
            if (nvCount > 0)
            {
                EnsureInitialized();
                Filtered = _requiredParameters.Length < nvCount || !_requiredParameters.Take(nvCount).All(rp => namedValues.Keys.Contains(rp.Name));

                if (!Filtered)
                {
                    _requiredParameters = _requiredParameters.Skip(nvCount).ToArray();
                    _arityMin -= nvCount;
                    _arityMax -= nvCount;
                }
            }
        }

        /// <summary>
        /// Get connector function parameter suggestions.
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="connectorParameter">Parameter for which we need a set of suggestions.</param>
        /// <param name="runtimeContext">Runtime connector context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>ConnectorParameters class with suggestions.</returns>
        public async Task<ConnectorParameters> GetParameterSuggestionsAsync(NamedValue[] knownParameters, ConnectorParameter connectorParameter, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<ConnectorParameterWithSuggestions> parametersWithSuggestions = new List<ConnectorParameterWithSuggestions>();
            ConnectorEnhancedSuggestions suggestions = GetConnectorSuggestionsAsync(knownParameters, connectorParameter, runtimeContext, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

            foreach (ConnectorParameter parameter in RequiredParameters.Union(OptionalParameters))
            {
                NamedValue namedValue = knownParameters.FirstOrDefault(p => p.Name == parameter.Name);
                ConnectorParameterWithSuggestions cpws = new ConnectorParameterWithSuggestions(parameter, namedValue?.Value, connectorParameter.Name, suggestions, knownParameters);
                parametersWithSuggestions.Add(cpws);
            }

            return new ConnectorParameters()
            {
                IsCompleted = suggestions != null && parametersWithSuggestions.All(p => !p.Suggestions.Any()),
                ParametersWithSuggestions = parametersWithSuggestions.ToArray()
            };
        }

        /// <summary>
        /// Dynamic intellisense (dynamic property/schema) on a given parameter.
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="connectorParameter">Parameter for which we need the (dynamic) type.</param>
        /// <param name="context">Connector context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorTypeAsync(NamedValue[] knownParameters, ConnectorParameter connectorParameter, BaseRuntimeConnectorContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetConnectorTypeAsync(knownParameters, connectorParameter.ConnectorType ?? ReturnParameterType, context, cancellationToken).ConfigureAwait(false);           
        }

        /// <summary>
        /// Dynamic intellisense (dynamic property/schema) on a given connector type (coming from a parameter or returned from GetConnectorTypeAsync).
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="connectorType">Connector type for which we need the (dynamic) type.</param>
        /// <param name="context">Connector context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorTypeAsync(NamedValue[] knownParameters, ConnectorType connectorType, BaseRuntimeConnectorContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (connectorType.DynamicProperty != null && !string.IsNullOrEmpty(connectorType.DynamicProperty.ItemValuePath))
            {
                return await GetConnectorSuggestionsFromDynamicPropertyAsync(knownParameters, context, connectorType.DynamicProperty, cancellationToken).ConfigureAwait(false);
            }
            else if (connectorType.DynamicSchema != null && !string.IsNullOrEmpty(connectorType.DynamicSchema.ValuePath))
            {
                return await GetConnectorSuggestionsFromDynamicSchemaAsync(knownParameters, context, connectorType.DynamicSchema, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Dynamic intellisense on return value.
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="context">Connector context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorReturnTypeAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetConnectorTypeAsync(knownParameters, ReturnParameterType, context, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Generates a Power Fx expression that will invoke this function with the given parameters. 
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Power Fx expression.</returns>
        public string GetExpression(ConnectorParameters parameters)
        {
            if (!parameters.IsCompleted)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder($@"{Namespace}.{Name}({string.Join(", ", parameters.ParametersWithSuggestions.Take(Math.Min(parameters.ParametersWithSuggestions.Length, ArityMax - 1)).Select(p => p.Value.ToExpression()))}");

            if (parameters.ParametersWithSuggestions.Length > ArityMax - 1)
            {
                ConnectorParameterWithSuggestions lastParam = parameters.ParametersWithSuggestions.Last();
                sb.Append($@", {{ ");

                List<(string name, FormulaValue fv)> nameValueAssociations = lastParam.ParameterNames.Zip(lastParam.Values, (name, fv) => (name, fv)).ToList();
                int i = 0;

                foreach ((string name, FormulaValue fv) in nameValueAssociations)
                {
                    sb.Append(name);
                    sb.Append(": ");
                    fv.ToExpression(sb, new FormulaValueSerializerSettings());

                    if (++i != nameValueAssociations.Count)
                    {
                        sb.Append(", ");
                    }
                }

                sb.Append(" }");
            }

            sb.Append(")");

            return sb.ToString();
        }

        /// <summary>
        /// Call connector function.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <param name="runtimeContext">RuntimeConnectorContext.</param>        
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Function result.</returns>
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureInitialized();
            ScopedHttpFunctionInvoker invoker = new ScopedHttpFunctionInvoker(DPath.Root.Append(DName.MakeValid(Namespace, out _)), Name, Namespace, new HttpFunctionInvoker(this, runtimeContext), runtimeContext.ThrowOnError);
            FormulaValue result = await invoker.InvokeAsync(args, runtimeContext, cancellationToken).ConfigureAwait(false);
            return await PostProcessResultAsync(result, runtimeContext, invoker, cancellationToken).ConfigureAwait(false);
        }

        private async Task<FormulaValue> PostProcessResultAsync(FormulaValue result, BaseRuntimeConnectorContext context, ScopedHttpFunctionInvoker invoker, CancellationToken cancellationToken)
        {
            ExpressionError er = null;

            if (result is ErrorValue ev && (er = ev.Errors.FirstOrDefault(e => e.Kind == ErrorKind.Network)) != null)
            {
                result = FormulaValue.NewError(new ExpressionError() { Kind = er.Kind, Severity = er.Severity, Message = $"{DPath.Root.Append(new DName(Namespace)).ToDottedSyntax()}.{Name} failed: {er.Message}" }, ev.Type);
            }

            if (IsPageable && result is RecordValue rv)
            {
                FormulaValue pageLink = rv.GetField(PageLink);
                string nextLink = (pageLink as StringValue)?.Value;

                // If there is no next link, we'll return a "normal" RecordValue as no paging is needed
                if (!string.IsNullOrEmpty(nextLink))
                {
                    result = new PagedRecordValue(rv, async () => await GetNextPageAsync(nextLink, context, invoker, cancellationToken).ConfigureAwait(false), ConnectorSettings.MaxRows, cancellationToken);
                }
            }

            return result;
        }

        // Can return 3 possible FormulaValues
        // - PagesRecordValue if the next page has a next link
        // - RecordValue if there is no next link
        // - ErrorValue
        private async Task<FormulaValue> GetNextPageAsync(string nextLink, BaseRuntimeConnectorContext context, ScopedHttpFunctionInvoker invoker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FormulaValue result = await invoker.InvokeAsync(nextLink, context, cancellationToken).ConfigureAwait(false);
            result = await PostProcessResultAsync(result, context, invoker, cancellationToken).ConfigureAwait(false);

            return result;
        }        

        internal async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsAsync(NamedValue[] knownParameters, ConnectorParameter parameter, BaseRuntimeConnectorContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();            

            if (parameter != null)
            {
                ConnectorExtensions connectorExtensions = parameter.ConnectorExtensions;

                if (connectorExtensions.ConnectorDynamicList != null)
                {
                    return await GetConnectorSuggestionsFromDynamicListAsync(knownParameters, context, connectorExtensions.ConnectorDynamicList, cancellationToken).ConfigureAwait(false);
                }

                if (connectorExtensions.ConnectorDynamicValue != null && string.IsNullOrEmpty(connectorExtensions.ConnectorDynamicValue.Capability))
                {
                    return await GetConnectorSuggestionsFromDynamicValueAsync(knownParameters, context, connectorExtensions.ConnectorDynamicValue, cancellationToken).ConfigureAwait(false);
                }

                ConnectorType connectorType = null;
                SuggestionMethod suggestionMethod = SuggestionMethod.None;

                if (connectorExtensions.ConnectorDynamicProperty != null && !string.IsNullOrEmpty(connectorExtensions.ConnectorDynamicProperty.ItemValuePath))
                {
                    connectorType = await GetConnectorSuggestionsFromDynamicPropertyAsync(knownParameters, context, connectorExtensions.ConnectorDynamicProperty, cancellationToken).ConfigureAwait(false);
                    suggestionMethod = SuggestionMethod.DynamicProperty;
                }
                else
                if (connectorExtensions.ConnectorDynamicSchema != null && !string.IsNullOrEmpty(connectorExtensions.ConnectorDynamicSchema.ValuePath))
                {
                    connectorType = await GetConnectorSuggestionsFromDynamicSchemaAsync(knownParameters, context, connectorExtensions.ConnectorDynamicSchema, cancellationToken).ConfigureAwait(false);
                    suggestionMethod = SuggestionMethod.DynamicSchema;
                }

                if (connectorType != null && connectorType.FormulaType is RecordType rt)
                {
                    return new ConnectorEnhancedSuggestions(suggestionMethod, rt.FieldNames.Select(fn => new ConnectorSuggestion(FormulaValue.NewBlank(rt.GetFieldType(fn)), fn)).ToList(), connectorType);
                }

                return null;
            }

            return null;
        }       

        private static JsonElement ExtractFromJson(StringValue sv, string location)
        {
            if (sv == null || string.IsNullOrEmpty(sv.Value))
            {
                return default;
            }

            return ExtractFromJson(JsonDocument.Parse(sv.Value).RootElement, location);
        }

        private static JsonElement ExtractFromJson(JsonElement je, string location)
        {
            foreach (string vpPart in location.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (je.ValueKind != JsonValueKind.Object)
                {
                    return default;
                }

                je = je.EnumerateObject().FirstOrDefault(jp => vpPart.Equals(jp.Name, StringComparison.OrdinalIgnoreCase)).Value;
            }

            return je;
        }

        private async Task<ConnectorType> GetConnectorSuggestionsFromDynamicSchemaAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext context, ConnectorDynamicSchema cds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cds, knownParameters);

            if (newParameters == null)
            {
                return null;
            }

            FormulaValue result = await ConnectorDynamicCallAsync(cds, newParameters, context, cancellationToken).ConfigureAwait(false);

            if (result is not StringValue sv)
            {
                return null;
            }

            JsonElement je = ExtractFromJson(sv, cds.ValuePath);
            OpenApiSchema schema = new OpenApiStringReader().ReadFragment<OpenApiSchema>(je.ToString(), Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0, out OpenApiDiagnostic diag);

            return new ConnectorType(schema, NumberIsFloat);
        }

        private async Task<ConnectorType> GetConnectorSuggestionsFromDynamicPropertyAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext context, ConnectorDynamicProperty cdp, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdp, knownParameters);

            if (newParameters == null)
            {
                return null;
            }

            FormulaValue result = await ConnectorDynamicCallAsync(cdp, newParameters, context, cancellationToken).ConfigureAwait(false);

            if (result is not StringValue sv)
            {
                return null;
            }

            JsonElement je = ExtractFromJson(sv, cdp.ItemValuePath);
            OpenApiSchema schema = new OpenApiStringReader().ReadFragment<OpenApiSchema>(je.ToString(), Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0, out OpenApiDiagnostic diag);

            return new ConnectorType(schema, NumberIsFloat);
        }

        private async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsFromDynamicValueAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext context, ConnectorDynamicValue cdv, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdv, knownParameters);

            if (newParameters == null)
            {
                return null;
            }

            FormulaValue result = await ConnectorDynamicCallAsync(cdv, newParameters, context, cancellationToken).ConfigureAwait(false);
            List<ConnectorSuggestion> suggestions = new List<ConnectorSuggestion>();

            if (result is not StringValue sv)
            {
                return null;
            }

            JsonElement je = ExtractFromJson(sv, cdv.ValueCollection ?? "value");

            if (je.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement jElement in je.EnumerateArray())
                {
                    JsonElement title = ExtractFromJson(jElement, cdv.ValueTitle);
                    JsonElement value = ExtractFromJson(jElement, cdv.ValuePath);

                    if (title.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Undefined)
                    {
                        continue;
                    }

                    suggestions.Add(new ConnectorSuggestion(FormulaValueJSON.FromJson(value), title.ToString()));
                }
            }

            return new ConnectorEnhancedSuggestions(SuggestionMethod.DynamicValue, suggestions);
        }

        private async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsFromDynamicListAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext context, ConnectorDynamicList cdl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdl, knownParameters);

            if (newParameters == null)
            {
                return null;
            }

            FormulaValue result = await ConnectorDynamicCallAsync(cdl, newParameters, context, cancellationToken).ConfigureAwait(false);
            List<ConnectorSuggestion> suggestions = new List<ConnectorSuggestion>();

            if (result is not StringValue sv)
            {
                return null;
            }

            JsonElement je = ExtractFromJson(sv, cdl.ItemPath);

            foreach (JsonElement jElement in je.EnumerateArray())
            {
                JsonElement title = ExtractFromJson(jElement, cdl.ItemTitlePath);
                JsonElement value = ExtractFromJson(jElement, cdl.ItemValuePath);

                if (title.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Undefined)
                {
                    continue;
                }

                suggestions.Add(new ConnectorSuggestion(FormulaValueJSON.FromJson(value), title.ToString()));
            }

            return new ConnectorEnhancedSuggestions(SuggestionMethod.DynamicList, suggestions);
        }

        private async Task<FormulaValue> ConnectorDynamicCallAsync(ConnectionDynamicApi dynamicApi, FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await EnsureConnectorFunction(dynamicApi, GlobalContext.FunctionList).ConnectorFunction.InvokeAsync(arguments, runtimeContext.WithRawResults(), cancellationToken).ConfigureAwait(false);
        }

        private void EnsureInitialized()
        {
            _internals ??= Initialize();
        }

        private T EnsureConnectorFunction<T>(T dynamicApi, IReadOnlyList<ConnectorFunction> functionList)
            where T : ConnectionDynamicApi
        {
            if (dynamicApi == null)
            {
                return dynamicApi;
            }

            dynamicApi.ConnectorFunction ??= functionList.FirstOrDefault(cf => dynamicApi.OperationId == cf.Name);
            return dynamicApi;
        }

        // Only used by ConnectorTexlFunction
        private DType[] GetParamTypes()
        {
            IEnumerable<DType> parameterTypes = RequiredParameters.Select(parameter => parameter.FormulaType._type);
            if (OptionalParameters.Any())
            {
                DType optionalParameterType = DType.CreateRecord(OptionalParameters.Select(cpi => new TypedName(cpi.FormulaType._type, new DName(cpi.Name))));
                optionalParameterType.AreFieldsOptional = true;

                _parameterTypes = parameterTypes.Append(optionalParameterType).ToArray();
                return _parameterTypes;
            }

            _parameterTypes = parameterTypes.ToArray();
            return _parameterTypes;
        }

        // This method only returns null when an error occurs
        // Otherwise it will return an empty array
        private FormulaValue[] GetArguments(ConnectionDynamicApi dynamicApi, NamedValue[] knownParameters)
        {
            List<FormulaValue> arguments = new List<FormulaValue>();

            ConnectorFunction functionToBeCalled = EnsureConnectorFunction(dynamicApi, GlobalContext.FunctionList).ConnectorFunction;

            foreach (ConnectorParameter connectorParameter in functionToBeCalled.RequiredParameters)
            {
                string requiredParameterName = connectorParameter.Name;

                if (dynamicApi.ParameterMap.FirstOrDefault(kvp => kvp.Value is StaticConnectorExtensionValue && kvp.Key == requiredParameterName).Value is StaticConnectorExtensionValue sValue)
                {
                    arguments.Add(sValue.Value);
                    continue;
                }

                KeyValuePair<string, IConnectorExtensionValue> dValue = dynamicApi.ParameterMap.FirstOrDefault(kvp => kvp.Value is DynamicConnectorExtensionValue dv && dv.Reference == requiredParameterName);
                NamedValue newParam = knownParameters.FirstOrDefault(nv => nv.Name == dValue.Key);

                if (newParam == null)
                {
                    // We're missing a required parameters, nothing we can do
                    return null;
                }

                arguments.Add(newParam.Value);
            }

            return arguments.ToArray();
        }

        private ConnectorParameterInternals Initialize()
        {
            // Hidden-Required parameters exist in the following conditions:
            // 1. required parameter
            // 2. has default value
            // 3. is marked "internal" in schema extension named "x-ms-visibility"
            List<ConnectorParameter> requiredParameters = new ();
            List<ConnectorParameter> hiddenRequiredParameters = new ();
            List<ConnectorParameter> optionalParameters = new ();

            // parameters used in ConnectorParameterInternals
            Dictionary<string, (bool, FormulaValue, DType)> parameterDefaultValues = new ();
            List<OpenApiParameter> openApiBodyParameters = new ();
            string bodySchemaReferenceId = null;
            bool schemaLessBody = false;
            string contentType = OpenApiExtensions.ContentType_ApplicationJson;

            try
            {
                foreach (OpenApiParameter parameter in Operation.Parameters)
                {
                    bool hiddenRequired = false;

                    if (parameter == null)
                    {
                        throw new PowerFxConnectorException("OpenApiParameters cannot be null, this swagger file is probably containing errors");
                    }

                    if (parameter.IsInternal())
                    {
                        if (parameter.Required)
                        {
                            if (parameter.Schema.Default == null)
                            {
                                // Ex: connectionId
                                continue;
                            }

                            // Ex: Api-Version 
                            hiddenRequired = true;
                        }
                    }

                    HttpFunctionInvoker.VerifyCanHandle(parameter.In);
                    ConnectorParameter connectorParameter = new ConnectorParameter(parameter, NumberIsFloat);

                    if (connectorParameter.HiddenRecordType != null)
                    {
                        throw new NotImplementedException("Unexpected value for a parameter");
                    }

                    if (parameter.Schema.TryGetDefaultValue(connectorParameter.FormulaType, out FormulaValue defaultValue, numberIsFloat: NumberIsFloat))
                    {
                        parameterDefaultValues[parameter.Name] = (connectorParameter.ConnectorType.IsRequired, defaultValue, connectorParameter.FormulaType._type);
                    }

                    List<ConnectorParameter> parameterList = !parameter.Required ? optionalParameters : hiddenRequired ? hiddenRequiredParameters : requiredParameters;
                    parameterList.Add(connectorParameter);
                }

                if (Operation.RequestBody != null)
                {
                    // We don't support x-ms-dynamic-values in "body" parameters for now (is that possible?)
                    OpenApiRequestBody requestBody = Operation.RequestBody;
                    string bodyName = requestBody.GetBodyName();

                    if (requestBody.Content != null && requestBody.Content.Any())
                    {
                        (string cnt, OpenApiMediaType mediaType) = requestBody.Content.GetContentTypeAndSchema();

                        if (!string.IsNullOrEmpty(contentType) && mediaType != null)
                        {
                            OpenApiSchema bodySchema = mediaType.Schema;
                            contentType = cnt;
                            bodySchemaReferenceId = bodySchema?.Reference?.Id;

                            // Additional properties are ignored for now
                            if (bodySchema.AnyOf.Any() || bodySchema.Not != null || (bodySchema.Items != null && bodySchema.Type != "array"))
                            {
                                throw new NotImplementedException($"OpenApiSchema is not supported - AnyOf, Not, AdditionalProperties or Items not array");
                            }
                            else if (bodySchema.AllOf.Any() || bodySchema.Properties.Any())
                            {
                                // We allow AllOf to be present             
                                foreach (KeyValuePair<string, OpenApiSchema> bodyProperty in bodySchema.Properties)
                                {
                                    OpenApiSchema bodyPropertySchema = bodyProperty.Value;
                                    string bodyPropertyName = bodyProperty.Key;
                                    bool bodyPropertyRequired = bodySchema.Required.Contains(bodyPropertyName);
                                    bool bodyPropertyHiddenRequired = false;

                                    if (bodyPropertySchema.IsInternal())
                                    {
                                        if (bodyPropertyRequired && bodyPropertySchema.Default != null)
                                        {
                                            bodyPropertyHiddenRequired = true;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    OpenApiParameter bodyParameter = new OpenApiParameter() { Name = bodyPropertyName, Schema = bodyPropertySchema, Description = requestBody.Description, Required = bodyPropertyRequired, Extensions = bodyPropertySchema.Extensions };
                                    openApiBodyParameters.Add(bodyParameter);
                                    ConnectorParameter bodyConnectorParameter2 = new ConnectorParameter(bodyParameter, requestBody, NumberIsFloat);

                                    if (bodyConnectorParameter2.HiddenRecordType != null)
                                    {
                                        hiddenRequiredParameters.Add(new ConnectorParameter(bodyParameter, true, NumberIsFloat));
                                    }

                                    List<ConnectorParameter> parameterList = !bodyPropertyRequired ? optionalParameters : bodyPropertyHiddenRequired ? hiddenRequiredParameters : requiredParameters;
                                    parameterList.Add(bodyConnectorParameter2);
                                }
                            }
                            else
                            {
                                schemaLessBody = true;

                                OpenApiParameter bodyParameter2 = new OpenApiParameter() { Name = bodyName, Schema = bodySchema, Description = requestBody.Description, Required = requestBody.Required, Extensions = bodySchema.Extensions };
                                openApiBodyParameters.Add(bodyParameter2);

                                ConnectorParameter bodyConnectorParameter3 = new ConnectorParameter(bodyParameter2, requestBody, NumberIsFloat);

                                if (bodyConnectorParameter3.HiddenRecordType != null)
                                {
                                    throw new NotImplementedException("Unexpected value for schema-less body");
                                }

                                List<ConnectorParameter> parameterList = requestBody.Required ? requiredParameters : optionalParameters;
                                parameterList.Add(bodyConnectorParameter3);
                            }
                        }
                    }
                    else
                    {
                        // If the content isn't specified, we will expect Json in the body
                        contentType = OpenApiExtensions.ContentType_ApplicationJson;
                        OpenApiSchema bodyParameterSchema = new OpenApiSchema() { Type = "string" };

                        OpenApiParameter bodyParameter3 = new OpenApiParameter() { Name = bodyName, Schema = bodyParameterSchema, Description = "Body", Required = requestBody.Required };
                        openApiBodyParameters.Add(bodyParameter3);

                        ConnectorParameter bodyParameter = new ConnectorParameter(bodyParameter3, requestBody, NumberIsFloat);

                        List<ConnectorParameter> parameterList = requestBody.Required ? requiredParameters : optionalParameters;
                        parameterList.Add(bodyParameter);
                    }
                }

                // Validate we have no name conflict between required and optional parameters
                // In case of conflict, we rename the optional parameter and add _1, _2, etc. until we have no conflict
                // We could imagine an API with required param Foo, and optional body params Foo and Foo_1 but this is not considered for now
                // Implemented in PA Client in src\Cloud\DocumentServer.Core\Document\Importers\ServiceConfig\RestFunctionDefinitionBuilder.cs at line 1176 - CreateUniqueImpliedParameterName
                List<string> requiredParamNames = requiredParameters.Select(rpi => rpi.Name).ToList();
                foreach (ConnectorParameter opi in optionalParameters)
                {
                    string paramName = opi.Name;

                    if (requiredParamNames.Contains(paramName))
                    {
                        int i = 0;
                        string newName;

                        do
                        {
                            newName = $"{paramName}_{++i}";
                        }
                        while (requiredParamNames.Contains(newName));

                        opi.SetName(newName);
                    }
                }

                // Required params are first N params in the final list. 
                // Optional params are fields on a single record argument at the end.
                // Hidden required parameters do not count here            
                _requiredParameters = requiredParameters.ToArray();
                _optionalParameters = optionalParameters.ToArray();
                _hiddenRequiredParameters = hiddenRequiredParameters.ToArray();
                _arityMin = _requiredParameters.Length;
                _arityMax = _arityMin + (_optionalParameters.Length == 0 ? 0 : 1);

                (ConnectorType connectorType, string unsupportedReason) = Operation.GetConnectorReturnType(NumberIsFloat);
                _returnParameterType = connectorType;

                if (!string.IsNullOrEmpty(unsupportedReason))
                {
                    _isSupported = ConnectorSettings.AllowUnsupportedFunctions;
                    _notSupportedReason = unsupportedReason;
                }
            }
            catch (Exception ex)
            {
                _isSupported = ConnectorSettings.AllowUnsupportedFunctions;
                _notSupportedReason = ex.Message;

                _requiredParameters = new ConnectorParameter[0];
                _optionalParameters = new ConnectorParameter[0];
                _hiddenRequiredParameters = new ConnectorParameter[0];
                _arityMax = 0;
                _arityMin = 0;
                _returnParameterType = null;
            }

            return new ConnectorParameterInternals()
            {
                OpenApiBodyParameters = openApiBodyParameters,
                ContentType = contentType,
                BodySchemaReferenceId = bodySchemaReferenceId,
                ParameterDefaultValues = parameterDefaultValues,
                SchemaLessBody = schemaLessBody
            };
        }
    }
}
