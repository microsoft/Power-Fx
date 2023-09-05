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
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    [DebuggerDisplay("{Name}")]
    public class ConnectorFunction
    {
        /// <summary>
        /// Normlalized name of the function.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Namespace of the function (not contained in swagger file).
        /// </summary>
        public string Namespace => ConnectorSettings.Namespace;

        /// <summary>
        /// Numbers are defined as "double" type (otherwise "decimal").
        /// </summary>
        public bool NumberIsFloat => ConnectorSettings.NumberIsFloat;

        /// <summary>
        /// ConnectorSettings. Contains the Namespace and other parameters.
        /// </summary>
        public ConnectorSettings ConnectorSettings { get; }

        /// <summary>
        /// Defines if the function is supported or contains unsupported elements.
        /// </summary>
        public bool IsSupported => EnsureInitialized(() => _isSupported);

        /// <summary>
        /// Reason for which the function isn't supported.
        /// </summary>
        public string NotSupportedReason => EnsureInitialized(() => _notSupportedReason);

        /// <summary>
        /// Defines if the function is deprecated.
        /// </summary>
        public bool IsDeprecated => Operation.Deprecated;

        /// <summary>
        /// Page Link as defined in the x-ms-pageable extension.
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
        /// </summary>
        public bool IsBehavior => !IsSafeHttpMethod(HttpMethod);

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
        public ConnectorType ConnectorReturnType => Operation.GetConnectorReturnType(NumberIsFloat);

        /// <summary>
        /// Minimum number of arguments.
        /// </summary>
        public int ArityMin => EnsureInitialized(() => _arityMin);

        /// <summary>
        /// Maximum number of arguments.
        /// </summary>
        public int ArityMax => EnsureInitialized(() => _arityMax);

        /// <summary>
        /// Required parameters.
        /// </summary>
        public ConnectorParameter[] RequiredParameters => EnsureInitialized(() => _requiredParameters);

        /// <summary>
        /// Optional parameters.
        /// </summary>
        public ConnectorParameter[] OptionalParameters => EnsureInitialized(() => _optionalParameters);

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
        internal ConnectorParameter[] HiddenRequiredParameters => EnsureInitialized(() => _hiddenRequiredParameters);

        /// <summary>
        /// OpenApiServers defined in the document.
        /// </summary>
        internal IList<OpenApiServer> Servers { get; init; }

        /// <summary>
        /// Dynamic schema extension on return type (response).
        /// </summary>
        internal ConnectorDynamicSchema DynamicReturnSchema => EnsureConnectorFunction(ReturnParameterType?.DynamicReturnSchema, FunctionList);

        /// <summary>
        /// Dynamic schema extension on return type (response).
        /// </summary>
        internal ConnectorDynamicProperty DynamicReturnProperty => EnsureConnectorFunction(ReturnParameterType?.DynamicReturnProperty, FunctionList);

        /// <summary>
        /// Return type when determined at runtime/by dynamic intellisense.
        /// </summary>
        internal ConnectorParameterType ReturnParameterType => EnsureInitialized(() => _returnParameterType);

        /// <summary>
        /// Parameter types used for TexlFunction.
        /// </summary>
        internal DType[] ParameterTypes => _parameterTypes ?? GetParamTypes();

        private DType[] _parameterTypes;

        /// <summary>
        /// List of functions in the same swagger file. Used for resolving dynamic schema/property.
        /// </summary>
        internal List<ConnectorFunction> FunctionList { get; init; }

        // Those parameters are protected by EnsureInitialized
        private int _arityMin;
        private int _arityMax;
        private ConnectorParameter[] _requiredParameters;
        private ConnectorParameter[] _hiddenRequiredParameters;
        private ConnectorParameter[] _optionalParameters;
        private ConnectorParameterType _returnParameterType;
        private bool _isSupported;
        private string _notSupportedReason;

        // Those properties are only used by HttpFunctionInvoker
        internal ConnectorParameterInternals _internals = null;

        internal ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, ConnectorSettings connectorSettings, List<ConnectorFunction> functionList)
        {
            Operation = openApiOperation ?? throw new ArgumentNullException(nameof(openApiOperation));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OperationPath = operationPath ?? throw new ArgumentNullException(nameof(operationPath));
            HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
            ConnectorSettings = connectorSettings;
            FunctionList = functionList ?? throw new ArgumentNullException(nameof(functionList));

            _isSupported = isSupported || connectorSettings.AllowUnsupportedFunctions;
            _notSupportedReason = notSupportedReason ?? (isSupported ? string.Empty : throw new ArgumentNullException(nameof(notSupportedReason)));
        }

        public ConnectorParameters GetParameters(FormulaValue[] knownParameters, IServiceProvider services)
        {
            ConnectorParameterWithSuggestions[] parametersWithSuggestions = RequiredParameters.Select((rp, i) => i < ArityMax - 1
                                                                                                            ? new ConnectorParameterWithSuggestions(rp, i < knownParameters.Length ? knownParameters[i] : null)
                                                                                                            : new ConnectorParameterWithSuggestions(rp, knownParameters.Skip(ArityMax - 1).ToArray())).ToArray();

            int index = Math.Min(knownParameters.Length, ArityMax - 1);
            ConnectorSuggestions suggestions = GetConnectorSuggestionsAsync(knownParameters, knownParameters.Length, services, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

            bool error = suggestions == null || suggestions.Error != null;

            if (!error)
            {
                if (knownParameters.Length >= ArityMax - 1)
                {
                    parametersWithSuggestions[index].ParameterNames = suggestions.Suggestions.Select(s => s.DisplayName).ToArray();
                    suggestions.Suggestions = suggestions.Suggestions.Skip(knownParameters.Length - ArityMax + 1).ToList();
                }

                parametersWithSuggestions[index].Suggestions = suggestions.Suggestions;
            }

            return new ConnectorParameters()
            {
                IsCompleted = !error && parametersWithSuggestions.All(p => !p.Suggestions.Any()),
                Parameters = parametersWithSuggestions
            };
        }

        public string GetExpression(string @namespace, ConnectorParameters parameters)
        {
            if (!parameters.IsCompleted)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder($@"{@namespace}.{Name}({string.Join(", ", parameters.Parameters.Take(Math.Min(parameters.Parameters.Length, ArityMax - 1)).Select(p => p.Value.ToExpression()))}");

            if (parameters.Parameters.Length > ArityMax - 1)
            {
                ConnectorParameterWithSuggestions lastParam = parameters.Parameters.Last();
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

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await InvokeInternalAsync(args, serviceProvider, ConnectorSettings.ReturnRawResult, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<FormulaValue> InvokeInternalAsync(FormulaValue[] args, IServiceProvider serviceProvider, bool rawResult, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            EnsureInitialized();
            RuntimeConnectorContext context = serviceProvider.GetService<RuntimeConnectorContext>() ?? throw new InvalidOperationException("RuntimeConnectorContext is missing from service provider");
            FormattingInfo formattingInfo = serviceProvider.GetService<FormattingInfo>() ?? FormattingInfoHelper.CreateFormattingInfo();
            ScopedHttpFunctionInvoker invoker = new ScopedHttpFunctionInvoker(DPath.Root.Append(DName.MakeValid(Namespace, out _)), Name, Namespace, new HttpFunctionInvoker(context.GetInvoker(Namespace), this, rawResult), ConnectorSettings.ThrowOnError);

            FormulaValue result = await invoker.InvokeAsync(formattingInfo, args, context, cancellationToken).ConfigureAwait(false);
            return await PostProcessResultAsync(result, context, invoker, cancellationToken).ConfigureAwait(false);
        }

        private async Task<FormulaValue> PostProcessResultAsync(FormulaValue result, RuntimeConnectorContext context, ScopedHttpFunctionInvoker invoker, CancellationToken cancellationToken)
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
        private async Task<FormulaValue> GetNextPageAsync(string nextLink, RuntimeConnectorContext context, ScopedHttpFunctionInvoker invoker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FormulaValue result = await invoker.InvokeAsync(nextLink, context, cancellationToken).ConfigureAwait(false);
            result = await PostProcessResultAsync(result, context, invoker, cancellationToken).ConfigureAwait(false);

            return result;
        }

        // For future implementation, we want to support the capability to run intellisense on optional parameters and need to use the parameter name to get the suggestions.
        // An example is Office 365 Outlook connector, with GetEmails function, where folderPath is optional and defines x-ms-dynamic-values
        // Also on SharePoint action SearchForUser (Resolve User)
        //public async Task<ConnectorSuggestions> GetConnectorSuggestionsAsync(HttpClient httpClient, NamedFormulaType[] inputParams, string suggestedParamName, CancellationToken cancellationToken)

        // This API only works on required paramaters and assumes we interrogate param number N+1 if N parameters are provided.
        public async Task<ConnectorSuggestions> GetConnectorSuggestionsAsync(FormulaValue[] knownParameters, IServiceProvider services, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetConnectorSuggestionsAsync(knownParameters, knownParameters.Length, services, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<ConnectorSuggestions> GetConnectorSuggestionsAsync(FormulaValue[] knownParameters, int argPosition, IServiceProvider services, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (argPosition >= 0 && ArityMax > 0 && RequiredParameters.Length > ArityMax - 1)
            {
                ConnectorParameter currentParam = RequiredParameters[Math.Min(argPosition, ArityMax - 1)];

                ConnectorDynamicList cdl = currentParam.DynamicList;
                ConnectorDynamicValue cdv = currentParam.DynamicValue;
                ConnectorDynamicSchema cds = currentParam.DynamicSchema;
                ConnectorDynamicProperty cdp = currentParam.DynamicProperty;

                if (cdl != null)
                {
                    return await GetConnectorSuggestionsFromDynamicListAsync(knownParameters, services, cdl, cancellationToken).ConfigureAwait(false);
                }

                if (cdv != null && string.IsNullOrEmpty(cdv.Capability))
                {
                    return await GetConnectorSuggestionsFromDynamicValueAsync(knownParameters, services, cdv, cancellationToken).ConfigureAwait(false);
                }

                FormulaType ft = null;

                if (cdp != null && !string.IsNullOrEmpty(cdp.ItemValuePath))
                {
                    ft = await GetConnectorSuggestionsFromDynamicPropertyAsync(knownParameters, argPosition, services, cdp, cancellationToken).ConfigureAwait(false);
                }
                else
                if (cds != null && !string.IsNullOrEmpty(cds.ValuePath))
                {
                    ft = await GetConnectorSuggestionsFromDynamicSchemaAsync(knownParameters, argPosition, services, cds, cancellationToken).ConfigureAwait(false);
                }

                if (ft != null && ft is RecordType rt)
                {
                    return new ConnectorSuggestions(rt.FieldNames.Select(fn => new ConnectorSuggestion(FormulaValue.NewBlank(rt.GetFieldType(fn)), fn)).ToList());
                }

                return null;
            }

            return null;
        }

        public async Task<FormulaType> GetConnectorReturnSchemaAsync(FormulaValue[] knownParameters, IServiceProvider services, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ConnectorDynamicSchema cds = ReturnParameterType.DynamicReturnSchema;
            ConnectorDynamicProperty cdp = ReturnParameterType.DynamicReturnProperty;

            if (cdp != null && !string.IsNullOrEmpty(cdp.ItemValuePath))
            {
                return await GetConnectorSuggestionsFromDynamicPropertyAsync(knownParameters, knownParameters.Length, services, cdp, cancellationToken).ConfigureAwait(false);
            }
            else if (cds != null && !string.IsNullOrEmpty(cds.ValuePath))
            {
                return await GetConnectorSuggestionsFromDynamicSchemaAsync(knownParameters, knownParameters.Length, services, cds, cancellationToken).ConfigureAwait(false);
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

        private async Task<FormulaType> GetConnectorSuggestionsFromDynamicSchemaAsync(FormulaValue[] knownParameters, int argPosition, IServiceProvider serviceProvider, ConnectorDynamicSchema cds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cds, knownParameters.Take(Math.Min(argPosition, ArityMax - 1)).ToArray());
            FormulaValue result = await ConnectorDynamicCallAsync(cds, newParameters, serviceProvider, cancellationToken).ConfigureAwait(false);
            List<ConnectorSuggestion> suggestions = new List<ConnectorSuggestion>();

            if (result is not StringValue sv)
            {
                return null;
            }

            JsonElement je = ExtractFromJson(sv, cds.ValuePath);
            OpenApiSchema schema = new OpenApiStringReader().ReadFragment<OpenApiSchema>(je.ToString(), Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0, out OpenApiDiagnostic diag);
            ConnectorParameterType cpt = schema.ToFormulaType();

            return cpt.Type;
        }

        private async Task<FormulaType> GetConnectorSuggestionsFromDynamicPropertyAsync(FormulaValue[] knownParameters, int argPosition, IServiceProvider serviceProvider, ConnectorDynamicProperty cdp, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdp, knownParameters.Take(Math.Min(argPosition, ArityMax - 1)).ToArray());
            FormulaValue result = await ConnectorDynamicCallAsync(cdp, newParameters, serviceProvider, cancellationToken).ConfigureAwait(false);
            List<ConnectorSuggestion> suggestions = new List<ConnectorSuggestion>();

            if (result is not StringValue sv)
            {
                return null;
            }

            JsonElement je = ExtractFromJson(sv, cdp.ItemValuePath);
            OpenApiSchema schema = new OpenApiStringReader().ReadFragment<OpenApiSchema>(je.ToString(), Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0, out OpenApiDiagnostic diag);
            ConnectorParameterType cpt = schema.ToFormulaType();

            return cpt.Type;
        }

        private async Task<ConnectorSuggestions> GetConnectorSuggestionsFromDynamicValueAsync(FormulaValue[] knownParameters, IServiceProvider serviceProvider, ConnectorDynamicValue cdv, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdv, knownParameters);
            FormulaValue result = await ConnectorDynamicCallAsync(cdv, newParameters, serviceProvider, cancellationToken).ConfigureAwait(false);
            List<ConnectorSuggestion> suggestions = new List<ConnectorSuggestion>();

            if (result is not StringValue sv)
            {
                return null;
            }

            JsonElement je = ExtractFromJson(sv, cdv.ValueCollection ?? "value");

            foreach (JsonElement jElement in je.EnumerateArray())
            {
                JsonElement title = ExtractFromJson(jElement, cdv.ValueTitle);
                JsonElement value = ExtractFromJson(jElement, cdv.ValuePath);

                suggestions.Add(new ConnectorSuggestion(FormulaValueJSON.FromJson(value), title.ToString()));
            }

            return new ConnectorSuggestions(suggestions);
        }

        private async Task<ConnectorSuggestions> GetConnectorSuggestionsFromDynamicListAsync(FormulaValue[] knownParameters, IServiceProvider serviceProvider, ConnectorDynamicList cdl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdl, knownParameters);
            FormulaValue result = await ConnectorDynamicCallAsync(cdl, newParameters, serviceProvider, cancellationToken).ConfigureAwait(false);
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

                suggestions.Add(new ConnectorSuggestion(FormulaValueJSON.FromJson(value), title.ToString()));
            }

            return new ConnectorSuggestions(suggestions);
        }

        private async Task<FormulaValue> ConnectorDynamicCallAsync(ConnectionDynamicApi dynamicApi, FormulaValue[] arguments, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await EnsureConnectorFunction(dynamicApi, FunctionList).ConnectorFunction.InvokeInternalAsync(arguments, serviceProvider, true, cancellationToken).ConfigureAwait(false);
        }

        private T EnsureConnectorFunction<T>(T dynamicApi, List<ConnectorFunction> functionList)
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

        private FormulaValue[] GetArguments(ConnectionDynamicApi dynamicApi, FormulaValue[] knownParameters)
        {
            List<FormulaValue> arguments = new List<FormulaValue>();

            foreach (ConnectorParameter cpi in EnsureConnectorFunction(dynamicApi, FunctionList).ConnectorFunction.RequiredParameters)
            {
                string paramName = cpi.Name;
                DType paramType = cpi.FormulaType._type;

                if (dynamicApi.ParameterMap.FirstOrDefault(kvp => kvp.Value is StaticConnectorExtensionValue && kvp.Key == paramName).Value is StaticConnectorExtensionValue sValue)
                {
                    arguments.Add(sValue.Value);
                    continue;
                }

                KeyValuePair<string, IConnectorExtensionValue> dValue = dynamicApi.ParameterMap.FirstOrDefault(kvp => kvp.Value is DynamicConnectorExtensionValue dv && dv.Reference == paramName);
                if (!string.IsNullOrEmpty(dValue.Key))
                {
                    int currentFunctionParamIndex = RequiredParameters.FindIndex(st => st.Name == dValue.Key);

                    if (currentFunctionParamIndex >= 0 && currentFunctionParamIndex < knownParameters.Length)
                    {
                        arguments.Add(knownParameters[currentFunctionParamIndex]);
                    }
                }
            }

            return arguments.ToArray();
        }

        private static bool IsSafeHttpMethod(HttpMethod httpMethod)
        {
            // HTTP/1.1 spec states that only GET and HEAD requests are 'safe' by default.
            // https://www.w3.org/Protocols/rfc2616/rfc2616-sec9.html
            return httpMethod == HttpMethod.Get || httpMethod == HttpMethod.Head;
        }

        private T EnsureInitialized<T>(Func<T> func)
        {
            EnsureInitialized();
            return func();
        }

        private void EnsureInitialized()
        {
            _internals ??= Initialize();            
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

                    string parameterName = parameter.Name;
                    ConnectorParameterType parameterType = parameter.Schema.ToFormulaType(numberIsFloat: NumberIsFloat);
                    parameterType.SetProperties(parameter);

                    ConnectorDynamicValue connectorDynamicValue = parameter.GetDynamicValue(NumberIsFloat);
                    ConnectorDynamicList connectorDynamicList = parameter.GetDynamicList(NumberIsFloat);
                    string summary = parameter.GetSummary();
                    bool explicitInput = parameter.GetExplicitInput();

                    if (parameterType.HiddenRecordType != null)
                    {
                        throw new NotImplementedException("Unexpected value for a parameter");
                    }

                    HttpFunctionInvoker.VerifyCanHandle(parameter.In);

                    if (parameter.Schema.TryGetDefaultValue(parameterType.Type, out FormulaValue defaultValue, numberIsFloat: NumberIsFloat))
                    {
                        parameterDefaultValues[parameterName] = (parameterType.ConnectorType.IsRequired, defaultValue, parameterType.Type._type);
                    }

                    List<ConnectorParameter> parameterList = !parameter.Required ? optionalParameters : hiddenRequired ? hiddenRequiredParameters : requiredParameters;
                    parameterList.Add(new ConnectorParameter(parameterName, parameter.Description, parameter.Schema, parameterType.Type, parameterType.ConnectorType, summary, connectorDynamicValue, connectorDynamicList, null, null, NumberIsFloat));
                }

                if (Operation.RequestBody != null)
                {
                    // We don't support x-ms-dynamic-values in "body" parameters for now (is that possible?)
                    OpenApiRequestBody requestBody = Operation.RequestBody;
                    string bodyName = requestBody.GetBodyName();
                    string bodySummary = requestBody.GetSummary();

                    if (requestBody.Content != null && requestBody.Content.Any())
                    {
                        (string cnt, OpenApiMediaType mediaType) = requestBody.Content.GetContentTypeAndSchema();

                        if (!string.IsNullOrEmpty(contentType) && mediaType != null)
                        {
                            OpenApiSchema bodySchema = mediaType.Schema;
                            ConnectorDynamicSchema connectorDynamicSchema = bodySchema.GetDynamicSchema(NumberIsFloat);
                            ConnectorDynamicProperty connectorDynamicProperty = bodySchema.GetDynamicProperty(NumberIsFloat);

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

                                    openApiBodyParameters.Add(new OpenApiParameter() { Schema = bodyPropertySchema, Name = bodyPropertyName, Description = "Body", Required = bodyPropertyRequired });
                                    ConnectorParameterType bodyPropertyParameterType = bodyPropertySchema.ToFormulaType(numberIsFloat: NumberIsFloat);
                                    bodyPropertyParameterType.SetProperties(bodyPropertyName, bodyPropertyRequired, bodyPropertySchema.GetVisibility());

                                    if (bodyPropertyParameterType.HiddenRecordType != null)
                                    {
                                        hiddenRequiredParameters.Add(new ConnectorParameter(bodyPropertyName, "Body", bodyPropertySchema, bodyPropertyParameterType.HiddenRecordType, bodyPropertyParameterType.HiddenConnectorType, bodySummary, null, null, connectorDynamicSchema, connectorDynamicProperty, NumberIsFloat));
                                    }

                                    List<ConnectorParameter> parameterList = !bodyPropertyRequired ? optionalParameters : bodyPropertyHiddenRequired ? hiddenRequiredParameters : requiredParameters;
                                    parameterList.Add(new ConnectorParameter(bodyPropertyName, "Body", bodyPropertySchema, bodyPropertyParameterType.Type, bodyPropertyParameterType.ConnectorType, bodySummary, null, null, connectorDynamicSchema, connectorDynamicProperty, NumberIsFloat));
                                }
                            }
                            else
                            {
                                schemaLessBody = true;
                                openApiBodyParameters.Add(new OpenApiParameter() { Schema = bodySchema, Name = bodyName, Description = "Body", Required = requestBody.Required });
                                ConnectorParameterType bodyParameterType = bodySchema.ToFormulaType(numberIsFloat: NumberIsFloat);
                                bodyParameterType.SetProperties(bodyName, requestBody.Required, bodySchema.GetVisibility());
                                List<ConnectorParameter> parameterList = requestBody.Required ? requiredParameters : optionalParameters;
                                parameterList.Add(new ConnectorParameter(bodyName, "Body", bodySchema, bodyParameterType.Type, bodyParameterType.ConnectorType, bodySummary, null, null, connectorDynamicSchema, connectorDynamicProperty, NumberIsFloat));

                                if (bodyParameterType.HiddenRecordType != null)
                                {
                                    throw new NotImplementedException("Unexpected value for schema-less body");
                                }
                            }
                        }
                    }
                    else
                    {
                        // If the content isn't specified, we will expect Json in the body
                        contentType = OpenApiExtensions.ContentType_ApplicationJson;
                        OpenApiSchema bodyParameterSchema = new OpenApiSchema() { Type = "string" };
                        openApiBodyParameters.Add(new OpenApiParameter() { Schema = bodyParameterSchema, Name = bodyName, Description = "Body", Required = requestBody.Required });
                        List<ConnectorParameter> parameterList = requestBody.Required ? requiredParameters : optionalParameters;
                        parameterList.Add(new ConnectorParameter(bodyName, "Body", bodyParameterSchema, FormulaType.String, new ConnectorType(bodyParameterSchema, FormulaType.String), bodySummary, null, null, null, null, NumberIsFloat));
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

                        opi.Name = newName;
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

                (ConnectorParameterType cpt, string unsupportedReason) = Operation.GetConnectorParameterReturnType(NumberIsFloat);
                _returnParameterType = cpt;

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
