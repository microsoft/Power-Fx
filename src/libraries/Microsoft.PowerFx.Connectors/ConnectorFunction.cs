// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;
using Microsoft.PowerFx.Connectors.Localization;
using Microsoft.PowerFx.Connectors.Tabular;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.ConnectorHelperFunctions;

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

        /// <summary>
        /// Warnings to be reported to end user.
        /// </summary>
        public IReadOnlyCollection<ErrorResourceKey> Warnings
        {
            get
            {
                EnsureInitialized();
                return _warnings;
            }
        }

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
        public FormulaType ReturnType => Operation.GetReturnType(ConnectorSettings.Compatibility);

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
        /// Filtered function.
        /// </summary>
        internal bool Filtered { get; }

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
                return _returnType;
            }
        }

        /// <summary>
        /// Parameter types used for TexlFunction.
        /// </summary>
        internal DType[] ParameterTypes => _parameterTypes ?? GetParamTypes();

        private DType[] _parameterTypes;

        /// <summary>
        /// Contains the list of functions in the same swagger file, used for resolving dynamic schema/property.
        /// Also contains all global values.
        /// </summary>
        internal ConnectorGlobalContext GlobalContext { get; }

        // Those parameters are protected by EnsureInitialized
        private int _arityMin;

        private int _arityMax;

        private ConnectorParameter[] _requiredParameters;

        private ConnectorParameter[] _hiddenRequiredParameters;

        private ConnectorParameter[] _optionalParameters;

        private ConnectorType _returnType;

        private bool _isSupported;

        private string _notSupportedReason;

        private List<ErrorResourceKey> _warnings;

        // Those properties are only used by HttpFunctionInvoker
        internal ConnectorParameterInternals _internals = null;

        private readonly ConnectorLogger _configurationLogger = null;

        internal ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, ConnectorSettings connectorSettings, List<ConnectorFunction> functionList, ConnectorLogger configurationLogger, IReadOnlyDictionary<string, FormulaValue> globalValues)
        {
            Operation = openApiOperation;
            Name = name;
            OperationPath = operationPath;
            HttpMethod = httpMethod;
            ConnectorSettings = connectorSettings;
            GlobalContext = new ConnectorGlobalContext(functionList ?? throw new ArgumentNullException(nameof(functionList)), globalValues);

            _configurationLogger = configurationLogger;
            _isSupported = isSupported;
            _notSupportedReason = notSupportedReason ?? (isSupported ? string.Empty : throw new PowerFxConnectorException("Internal error on not supported reason"));

            int nvCount = globalValues?.Count(nv => nv.Key != "connectionId") ?? 0;
            if (nvCount > 0)
            {
                EnsureInitialized();
                Filtered = _requiredParameters.Length < nvCount || !_requiredParameters.Take(nvCount).All(rp => globalValues.Keys.Contains(rp.Name));

                if (!Filtered)
                {
                    _requiredParameters = _requiredParameters.Skip(nvCount).ToArray();
                    _arityMin -= nvCount;
                    _arityMax -= nvCount;
                }
            }
        }

        internal void SetUnsupported(string notSupportedReason)
        {
            _isSupported = false;
            _notSupportedReason = string.IsNullOrEmpty(_notSupportedReason) ? notSupportedReason : $"{_notSupportedReason}, {notSupportedReason}";
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
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetParameterSuggestionsAsync))}, with {LogKnownParameters(knownParameters)}, {LogConnectorParameter(connectorParameter)}");
                ConnectorParameters parameters = await GetParameterSuggestionsInternalAsync(knownParameters, connectorParameter, runtimeContext, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetParameterSuggestionsAsync))}, returning {LogConnectorParameters(parameters)}");
                return parameters;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetParameterSuggestionsAsync))}, Context {LogKnownParameters(knownParameters)} {LogConnectorParameter(connectorParameter)}, {LogException(ex)}");
                throw;
            }
        }

        internal async Task<ConnectorParameters> GetParameterSuggestionsInternalAsync(NamedValue[] knownParameters, ConnectorParameter connectorParameter, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runtimeContext.ExecutionLogger?.LogDebug($"Entering in {this.LogFunction(nameof(GetParameterSuggestionsInternalAsync))}, with {LogKnownParameters(knownParameters)}, {LogConnectorParameter(connectorParameter)}");

            List<ConnectorParameterWithSuggestions> parametersWithSuggestions = new List<ConnectorParameterWithSuggestions>();
            ConnectorEnhancedSuggestions suggestions = GetConnectorSuggestionsInternalAsync(knownParameters, connectorParameter.ConnectorType, runtimeContext, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

            runtimeContext.ExecutionLogger?.LogDebug($"In {this.LogFunction(nameof(GetParameterSuggestionsInternalAsync))}, returning from {nameof(GetConnectorSuggestionsInternalAsync)} with {LogConnectorEnhancedSuggestions(suggestions)}");
            foreach (ConnectorParameter parameter in RequiredParameters.Union(OptionalParameters))
            {
                NamedValue namedValue = knownParameters.FirstOrDefault(p => p.Name == parameter.Name);
                ConnectorParameterWithSuggestions cpws = new ConnectorParameterWithSuggestions(parameter, namedValue?.Value, connectorParameter.Name, suggestions, knownParameters);
                parametersWithSuggestions.Add(cpws);
            }

            ConnectorParameters connectorParameters = new ConnectorParameters()
            {
                IsCompleted = suggestions != null && parametersWithSuggestions.All(p => !p.Suggestions.Any()),
                ParametersWithSuggestions = parametersWithSuggestions.ToArray()
            };

            runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetParameterSuggestionsInternalAsync))}, with {LogConnectorParameters(connectorParameters)}");
            return connectorParameters;
        }

        /// <summary>
        /// Get connector function parameter suggestions.
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="connectorType">Connector type for which we need a set of suggestions.</param>
        /// <param name="runtimeContext">Runtime connector context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>ConnectorParameters class with suggestions.</returns>
        public async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsAsync(NamedValue[] knownParameters, ConnectorType connectorType, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetConnectorSuggestionsAsync))}, with {LogKnownParameters(knownParameters)}, {LogConnectorType(connectorType)}");
                ConnectorEnhancedSuggestions suggestions = await GetConnectorSuggestionsInternalAsync(knownParameters, connectorType, runtimeContext, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsAsync))}, returning from {nameof(GetConnectorSuggestionsInternalAsync)} with {LogConnectorEnhancedSuggestions(suggestions)}");
                return suggestions;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorSuggestionsAsync))}, Context {LogKnownParameters(knownParameters)} {LogConnectorType(connectorType)}, {LogException(ex)}");
                throw;
            }
        }

        internal async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsInternalAsync(NamedValue[] knownParameters, ConnectorType connectorType, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runtimeContext.ExecutionLogger?.LogDebug($"Entering in {this.LogFunction(nameof(GetConnectorSuggestionsInternalAsync))}, with {LogKnownParameters(knownParameters)}, {LogConnectorType(connectorType)}");

            if (connectorType != null)
            {
                if (connectorType.DynamicList != null)
                {
                    ConnectorEnhancedSuggestions suggestions = await GetConnectorSuggestionsFromDynamicListAsync(knownParameters, runtimeContext, connectorType.DynamicList, cancellationToken).ConfigureAwait(false);
                    runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicListAsync)} with {LogConnectorEnhancedSuggestions(suggestions)}");
                    return suggestions;
                }

                // BuiltInOperation and capability are currently not supported
                if (connectorType.DynamicValues != null)
                {
                    ConnectorEnhancedSuggestions suggestions = await GetConnectorSuggestionsFromDynamicValueAsync(knownParameters, runtimeContext, connectorType.DynamicValues, cancellationToken).ConfigureAwait(false);
                    runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicValueAsync)} with {LogConnectorEnhancedSuggestions(suggestions)}");
                    return suggestions;
                }

                ConnectorType outputConnectorType = null;
                SuggestionMethod suggestionMethod = SuggestionMethod.None;

                if (connectorType.DynamicProperty != null && !string.IsNullOrEmpty(connectorType.DynamicProperty.ItemValuePath))
                {
                    outputConnectorType = await GetConnectorSuggestionsFromDynamicPropertyAsync(knownParameters, runtimeContext, connectorType.DynamicProperty, cancellationToken).ConfigureAwait(false);
                    suggestionMethod = SuggestionMethod.DynamicProperty;
                }
                else if (connectorType.DynamicSchema != null && !string.IsNullOrEmpty(connectorType.DynamicSchema.ValuePath))
                {
                    outputConnectorType = await GetConnectorSuggestionsFromDynamicSchemaAsync(knownParameters, runtimeContext, connectorType.DynamicSchema, cancellationToken).ConfigureAwait(false);
                    suggestionMethod = SuggestionMethod.DynamicSchema;
                }

                if (outputConnectorType != null && outputConnectorType.FormulaType is RecordType rt)
                {
                    ConnectorEnhancedSuggestions suggestions = new ConnectorEnhancedSuggestions(suggestionMethod, rt.FieldNames.Select(fn => new ConnectorSuggestion(FormulaValue.NewBlank(rt.GetFieldType(fn)), fn)).ToList(), outputConnectorType);
                    runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsInternalAsync))}, returning from {(suggestionMethod == SuggestionMethod.DynamicProperty ? nameof(GetConnectorSuggestionsFromDynamicPropertyAsync) : nameof(GetConnectorSuggestionsFromDynamicSchemaAsync))} with {LogConnectorEnhancedSuggestions(suggestions)}");
                    return suggestions;
                }

                if (connectorType.DynamicList == null & connectorType.DynamicValues == null && connectorType.DynamicProperty == null && connectorType.DynamicSchema == null)
                {
                    runtimeContext.ExecutionLogger?.LogWarning($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsInternalAsync))}, returning null as no dynamic extension for {LogConnectorType(connectorType)}");
                }
                else
                {
                    runtimeContext.ExecutionLogger?.LogWarning($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsInternalAsync))}, returning null for {LogConnectorType(connectorType)}");
                }

                return null;
            }

            runtimeContext.ExecutionLogger?.LogWarning($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsInternalAsync))}, returning null as connectorType is null");
            return null;
        }

        /// <summary>
        /// Dynamic intellisense (dynamic property/schema) on a given parameter.
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="connectorParameter">Parameter for which we need the (dynamic) type.</param>
        /// <param name="runtimeContext">Connector context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorParameterTypeAsync(NamedValue[] knownParameters, ConnectorParameter connectorParameter, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetConnectorParameterTypeAsync))}, with {LogKnownParameters(knownParameters)}, {LogConnectorParameter(connectorParameter)}");

                ConnectorType result = await GetConnectorParameterTypeAsync(knownParameters, connectorParameter, runtimeContext, new CallCounter() { CallsLeft = 1 }, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorParameterTypeAsync))}, returning from {nameof(GetConnectorTypeInternalAsync)} with {LogConnectorType(result)}");
                return result;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorParameterTypeAsync))}, Context {LogKnownParameters(knownParameters)} {LogConnectorParameter(connectorParameter)}, {LogException(ex)}");
                throw;
            }
        }

        /// <summary>
        /// Dynamic intellisense (dynamic property/schema) on a given parameter.
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="connectorParameter">Parameter for which we need the (dynamic) type.</param>
        /// <param name="runtimeContext">Connector context.</param>
        /// <param name="maxCalls">Max number of recursive network calls.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorParameterTypeAsync(NamedValue[] knownParameters, ConnectorParameter connectorParameter, BaseRuntimeConnectorContext runtimeContext, int maxCalls, CancellationToken cancellationToken)
        {
            return await GetConnectorParameterTypeAsync(knownParameters, connectorParameter, runtimeContext, new CallCounter() { CallsLeft = maxCalls }, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<ConnectorType> GetConnectorParameterTypeAsync(NamedValue[] knownParameters, ConnectorParameter connectorParameter, BaseRuntimeConnectorContext runtimeContext, CallCounter maxCalls, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetConnectorParameterTypeAsync))}, with {LogKnownParameters(knownParameters)}, callsLeft {maxCalls.CallsLeft}, {LogConnectorParameter(connectorParameter)}");

                ConnectorType result = await GetConnectorTypeInternalAsync(knownParameters, connectorParameter.ConnectorType ?? ReturnParameterType, runtimeContext, maxCalls, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorParameterTypeAsync))}, returning from {nameof(GetConnectorTypeInternalAsync)} with {LogConnectorType(result)}, callsLeft {maxCalls.CallsLeft}");
                return result;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorParameterTypeAsync))}, Context {LogKnownParameters(knownParameters)} {LogConnectorParameter(connectorParameter)}, {LogException(ex)}");
                throw;
            }
        }

        /// <summary>
        /// Dynamic intellisense (dynamic property/schema) on a given connector type (coming from a parameter or returned from GetConnectorTypeAsync).
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="connectorType">Connector type for which we need the (dynamic) type.</param>
        /// <param name="runtimeContext">Connector context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorTypeAsync(NamedValue[] knownParameters, ConnectorType connectorType, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetConnectorTypeAsync))}, with {LogKnownParameters(knownParameters)} for {LogConnectorType(connectorType)}");
                ConnectorType connectorType2 = await GetConnectorTypeAsync(knownParameters, connectorType, runtimeContext, new CallCounter() { CallsLeft = 1 }, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorTypeAsync))}, returning from {nameof(GetConnectorTypeInternalAsync)} with {LogConnectorType(connectorType2)}");
                return connectorType2;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorTypeAsync))}, Context {LogKnownParameters(knownParameters)} {LogConnectorType(connectorType)}, {LogException(ex)}");
                throw;
            }
        }

        /// <summary>
        /// Dynamic intellisense (dynamic property/schema) on a given connector type (coming from a parameter or returned from GetConnectorTypeAsync).
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="connectorType">Connector type for which we need the (dynamic) type.</param>
        /// <param name="runtimeContext">Connector context.</param>
        /// <param name="maxCalls">Max number of recursive network calls.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorTypeAsync(NamedValue[] knownParameters, ConnectorType connectorType, BaseRuntimeConnectorContext runtimeContext, int maxCalls, CancellationToken cancellationToken)
        {
            return await GetConnectorTypeAsync(knownParameters, connectorType, runtimeContext, new CallCounter() { CallsLeft = maxCalls }, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<ConnectorType> GetConnectorTypeAsync(NamedValue[] knownParameters, ConnectorType connectorType, BaseRuntimeConnectorContext runtimeContext, CallCounter maxCalls, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetConnectorTypeAsync))}, with {LogKnownParameters(knownParameters)} and callsLeft {maxCalls.CallsLeft} for {LogConnectorType(connectorType)}");
                ConnectorType connectorType2 = await GetConnectorTypeInternalAsync(knownParameters, connectorType, runtimeContext, maxCalls, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorTypeAsync))}, returning from {nameof(GetConnectorTypeInternalAsync)} with {LogConnectorType(connectorType2)}, callsLeft {maxCalls.CallsLeft}");
                return connectorType2;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorTypeAsync))}, Context {LogKnownParameters(knownParameters)}, callsLeft {maxCalls.CallsLeft} {LogConnectorType(connectorType)}, {LogException(ex)}");
                throw;
            }
        }

        internal async Task<ConnectorType> GetConnectorTypeInternalAsync(NamedValue[] knownParameters, ConnectorType connectorType, BaseRuntimeConnectorContext runtimeContext, CallCounter maxCalls, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runtimeContext.ExecutionLogger?.LogDebug($"Entering in {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, with {LogKnownParameters(knownParameters)}, callsLeft {maxCalls.CallsLeft} for {LogConnectorType(connectorType)}");

            if (connectorType.DynamicProperty != null && !string.IsNullOrEmpty(connectorType.DynamicProperty.ItemValuePath))
            {
                maxCalls.CallsLeft--;
                ConnectorType result = await GetConnectorSuggestionsFromDynamicPropertyAsync(knownParameters, runtimeContext, connectorType.DynamicProperty, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicPropertyAsync)} with {LogConnectorType(result)}");
                return result;
            }
            else if (connectorType.DynamicSchema != null && !string.IsNullOrEmpty(connectorType.DynamicSchema.ValuePath))
            {
                maxCalls.CallsLeft--;
                ConnectorType result = await GetConnectorSuggestionsFromDynamicSchemaAsync(knownParameters, runtimeContext, connectorType.DynamicSchema, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicSchemaAsync)} with {LogConnectorType(result)}");
                return result;
            }
            else if (connectorType.DynamicList != null && !string.IsNullOrEmpty(connectorType.DynamicList.ItemPath))
            {
                maxCalls.CallsLeft--;
                ConnectorEnhancedSuggestions result = await GetConnectorSuggestionsFromDynamicListAsync(knownParameters, runtimeContext, connectorType.DynamicList, cancellationToken).ConfigureAwait(false);

                // This is an OptionSet
                if (result != null && (connectorType.FormulaType is DecimalType || connectorType.FormulaType is NumberType))
                {
                    ConnectorType connectorType2 = GetOptionSetFromSuggestions(connectorType, result);
                    runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicListAsync)} with {LogConnectorType(connectorType2)}");
                    return connectorType2;
                }
            }
            else if (connectorType.DynamicValues != null && !string.IsNullOrEmpty(connectorType.DynamicValues.ValuePath))
            {
                maxCalls.CallsLeft--;
                ConnectorEnhancedSuggestions result = await GetConnectorSuggestionsFromDynamicValueAsync(knownParameters, runtimeContext, connectorType.DynamicValues, cancellationToken).ConfigureAwait(false);

                // This is an OptionSet
                if (result != null && (connectorType.FormulaType is DecimalType || connectorType.FormulaType is NumberType))
                {
                    ConnectorType connectorType2 = GetOptionSetFromSuggestions(connectorType, result);
                    runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicValueAsync)} with {LogConnectorType(connectorType2)}");
                    return connectorType2;
                }
            }
            else if (connectorType.ContainsDynamicIntellisense && connectorType.Fields.Any() && connectorType.FormulaType is AggregateType aggregateType)
            {
                List<ConnectorType> fieldTypes = new List<ConnectorType>();
                RecordType recordType = RecordType.Empty();

                foreach (ConnectorType field in connectorType.Fields)
                {
                    ConnectorType newFieldType = field;

                    while (newFieldType.ContainsDynamicIntellisense && maxCalls.CallsLeft > 0)
                    {
                        ConnectorType newFieldType2 = await GetConnectorTypeInternalAsync(knownParameters, newFieldType, runtimeContext, maxCalls, cancellationToken).ConfigureAwait(false);

                        if (newFieldType2 == null)
                        {
                            break;
                        }

                        newFieldType = newFieldType2;
                    }

                    if (maxCalls.CallsLeft <= 0)
                    {
                        runtimeContext.ExecutionLogger?.LogDebug($"In {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, callsLeft {maxCalls.CallsLeft}.");
                    }

                    // field's name correspond to the key of the fhe field in the record, we need to set it to wire it up correctly to the updated record type
                    newFieldType.Name = field.Name;
                    fieldTypes.Add(newFieldType);
                    recordType = recordType.Add(field.Name, newFieldType.FormulaType, field.DisplayName);
                }

                FormulaType formulaType = connectorType.FormulaType is RecordType ? recordType : recordType.ToTable();
                ConnectorType newConnectorType = new ConnectorType(connectorType, fieldTypes.ToArray(), formulaType);
                runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicSchemaAsync)} with {LogConnectorType(newConnectorType)}");
                return newConnectorType;
            }

            runtimeContext.ExecutionLogger?.LogWarning($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning null as no dynamic extension defined for {LogConnectorType(connectorType)}");
            return null;
        }

        private static ConnectorType GetOptionSetFromSuggestions(ConnectorType connectorType, ConnectorEnhancedSuggestions result)
        {
            IEnumerable<KeyValuePair<string, object>> values = result.ConnectorSuggestions.Suggestions.Select(cs => new KeyValuePair<string, object>(cs.DisplayName, cs.Suggestion.AsDouble()));
            EnumSymbol optionSet = new EnumSymbol(new DName(connectorType.Name), DType.Number, values);
            OpenApiParameter openApiParameter = new OpenApiParameter()
            {
                Name = connectorType.Name,
                Required = connectorType.IsRequired,
                Extensions = new Dictionary<string, IOpenApiExtension>()
                {
                    { Constants.XMsVisibility, new OpenApiString(connectorType.Visibility.ToString()) },
                    { Constants.XMsMediaKind, new OpenApiString(connectorType.MediaKind.ToString()) }
                }
            };

            IConnectorSchema schema = new ConnectorApiSchema(connectorType.Schema)
            {
                Enum = optionSet.EnumType.ValueTree.GetPairs().Select(kvp => new OpenApiDouble((double)kvp.Value.Object) as IOpenApiAny).ToList()
            };

            OpenApiArray array = new OpenApiArray();
            array.AddRange(optionSet.EnumType.ValueTree.GetPairs().Select(kvp => new OpenApiString(kvp.Key)));

            schema.Extensions.Add(new KeyValuePair<string, IOpenApiExtension>(Constants.XMsEnumDisplayName, array));

            // For now, we keep the original formula type (number/string/bool...)
            return new ConnectorType(schema, ConnectorApiParameter.New(openApiParameter), connectorType.FormulaType /* optionSet.FormulaType */);
        }

        /// <summary>
        /// Dynamic intellisense on return value.
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="runtimeContext">Connector context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorReturnTypeAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetConnectorReturnTypeAsync))}, with {LogKnownParameters(knownParameters)}");
                ConnectorType connectorType = await GetConnectorReturnTypeAsync(knownParameters, runtimeContext, new CallCounter() { CallsLeft = 1 }, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorReturnTypeAsync))}, returning {nameof(GetConnectorTypeAsync)}, with {LogConnectorType(connectorType)}");
                return connectorType;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorReturnTypeAsync))}, Context {LogKnownParameters(knownParameters)}, {LogException(ex)}");
                throw;
            }
        }

        /// <summary>
        /// Dynamic intellisense on return value.
        /// </summary>
        /// <param name="knownParameters">Known parameters.</param>
        /// <param name="runtimeContext">Connector context.</param>
        /// <param name="maxCalls">Max number of recursive network calls.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Formula Type determined by dynamic Intellisense.</returns>
        public async Task<ConnectorType> GetConnectorReturnTypeAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, int maxCalls, CancellationToken cancellationToken)
        {
            return await GetConnectorReturnTypeAsync(knownParameters, runtimeContext, new CallCounter() { CallsLeft = maxCalls }, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<ConnectorType> GetConnectorReturnTypeAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, CallCounter maxCalls, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetConnectorReturnTypeAsync))}, with {LogKnownParameters(knownParameters)} and callsLeft {maxCalls.CallsLeft}");
                ConnectorType connectorType = await GetConnectorTypeAsync(knownParameters, ReturnParameterType, runtimeContext, maxCalls, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorReturnTypeAsync))}, returning {nameof(GetConnectorTypeAsync)}, with {LogConnectorType(connectorType)}");
                return connectorType;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorReturnTypeAsync))}, Context {LogKnownParameters(knownParameters)}, {LogException(ex)}");
                throw;
            }
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
        /// <param name="arguments">Arguments.</param>
        /// <param name="runtimeContext">RuntimeConnectorContext.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Function result.</returns>
        public Task<FormulaValue> InvokeAsync(FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            return InvokeAsync(arguments, runtimeContext, null, cancellationToken);
        }

        /// <summary>
        /// Call connector function.
        /// </summary>
        /// <param name="arguments">Arguments.</param>
        /// <param name="runtimeContext">RuntimeConnectorContext.</param>
        /// <param name="outputTypeOverride">The output type that should be used during output parsing.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Function result.</returns>
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, FormulaType outputTypeOverride, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(InvokeAsync))}, with {LogArguments(arguments)}");
                FormulaValue formulaValue = await InvokeInternalAsync(arguments, runtimeContext, outputTypeOverride, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(InvokeAsync))}, returning from {nameof(InvokeInternalAsync)}, with {LogFormulaValue(formulaValue)}");
                return formulaValue;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(InvokeAsync))}, Context {LogArguments(arguments)}, {LogException(ex)}");
                throw;
            }
        }

        internal Task<FormulaValue> InvokeInternalAsync(FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            return InvokeInternalAsync(arguments, runtimeContext, null, cancellationToken);
        }

        internal async Task<FormulaValue> InvokeInternalAsync(FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, FormulaType outputTypeOverride, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureInitialized();
            runtimeContext.ExecutionLogger?.LogDebug($"Entering in {this.LogFunction(nameof(InvokeInternalAsync))}, with {LogArguments(arguments)}");

            if (!IsSupported)
            {
                throw new InvalidOperationException($"In namespace {Namespace}, function {Name} is not supported.");
            }

            FormulaValue ev = arguments.Where(arg => arg is ErrorValue).FirstOrDefault();
            if (ev != null)
            {
                return ev;
            }

            BaseRuntimeConnectorContext context = ReturnParameterType.Binary ? runtimeContext.WithRawResults() : runtimeContext;
            ScopedHttpFunctionInvoker invoker = new ScopedHttpFunctionInvoker(DPath.Root.Append(DName.MakeValid(Namespace, out _)), Name, Namespace, new HttpFunctionInvoker(this, context), context.ThrowOnError);
            FormulaValue result = await invoker.InvokeAsync(arguments, context, outputTypeOverride, cancellationToken).ConfigureAwait(false);
            FormulaValue formulaValue = await PostProcessResultAsync(result, runtimeContext, invoker, cancellationToken).ConfigureAwait(false);

            runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(InvokeInternalAsync))}, returning {LogFormulaValue(formulaValue)}");
            return formulaValue;
        }

        private async Task<FormulaValue> PostProcessResultAsync(FormulaValue result, BaseRuntimeConnectorContext runtimeContext, ScopedHttpFunctionInvoker invoker, CancellationToken cancellationToken)
        {
            ExpressionError er = null;

            if (result is ErrorValue ev && (er = ev.Errors.FirstOrDefault(e => e.Kind == ErrorKind.Network)) != null)
            {
                runtimeContext.ExecutionLogger?.LogError($"{this.LogFunction(nameof(PostProcessResultAsync))}, ErrorValue is returned with {er.Message}");
                result = FormulaValue.NewError(new ExpressionError() { Kind = er.Kind, Severity = er.Severity, Message = $"{DPath.Root.Append(new DName(Namespace)).ToDottedSyntax()}.{Name} failed: {er.Message}" }, ev.Type);
            }

            if (IsPageable && result is RecordValue rv)
            {
                FormulaValue pageLink = rv.GetField(PageLink);
                string nextLink = (pageLink as StringValue)?.Value;

                // If there is no next link, we'll return a "normal" RecordValue as no paging is needed
                if (!string.IsNullOrEmpty(nextLink))
                {
                    result = new PagedRecordValue(rv, async () => await GetNextPageAsync(nextLink, runtimeContext, invoker, cancellationToken).ConfigureAwait(false), ConnectorSettings.MaxRows, cancellationToken);
                }
            }

            if (ReturnParameterType.FormulaType is BlobType bt && result is StringValue str)
            {
                result = FormulaValue.NewBlob(str.Value, ReturnParameterType.Schema.Format == "byte");
            }

            return result;
        }

        // Can return 3 possible FormulaValues
        // - PagesRecordValue if the next page has a next link
        // - RecordValue if there is no next link
        // - ErrorValue
        private async Task<FormulaValue> GetNextPageAsync(string nextLink, BaseRuntimeConnectorContext runtimeContext, ScopedHttpFunctionInvoker invoker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetNextPageAsync))}, getting next page");

            if (!IsSupported)
            {
                throw new InvalidOperationException($"In namespace {Namespace}, function {Name} is not supported.");
            }

            FormulaValue result = await invoker.InvokeAsync(nextLink, runtimeContext, cancellationToken).ConfigureAwait(false);
            result = await PostProcessResultAsync(result, runtimeContext, invoker, cancellationToken).ConfigureAwait(false);
            runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetNextPageAsync))} with {LogFormulaValue(result)}");
            return result;
        }

        private static JsonElement ExtractFromJson(StringValue sv, string location)
        {
            if (sv == null || string.IsNullOrEmpty(sv.Value))
            {
                return default;
            }

            return ExtractFromJson(JsonDocument.Parse(sv.Value).RootElement, location);
        }

        private static JsonElement ExtractFromJson(StringValue sv, string location, out string name, out string displayName)
        {
            if (sv == null || string.IsNullOrEmpty(sv.Value))
            {
                name = displayName = null;
                return default;
            }

            JsonElement jsonRoot = JsonDocument.Parse(sv.Value).RootElement;
            JsonElement je = ExtractFromJson(jsonRoot, location);

            name = displayName = null;

            if (jsonRoot.ValueKind == JsonValueKind.Object)
            {
                JsonElement.ObjectEnumerator objectEnumerator = jsonRoot.EnumerateObject();
                name = SafeGetProperty(objectEnumerator, "name");
                displayName = SafeGetProperty(objectEnumerator, "title");
            }

            return je;
        }

        private static readonly char[] _slash = new char[] { '/' };

        private static JsonElement ExtractFromJson(JsonElement je, string location)
        {
            foreach (string vpPart in (location ?? string.Empty).Split(_slash, StringSplitOptions.RemoveEmptyEntries))
            {
                if (je.ValueKind != JsonValueKind.Object)
                {
                    return default;
                }

                je = je.EnumerateObject().FirstOrDefault(jp => vpPart.Equals(jp.Name, StringComparison.OrdinalIgnoreCase)).Value;
            }

            return je;
        }

        private static string SafeGetProperty(JsonElement.ObjectEnumerator jsonObjectEnumerator, string propName)
        {
            JsonElement je = jsonObjectEnumerator.FirstOrDefault(jp => jp.Name.Equals(propName, StringComparison.OrdinalIgnoreCase)).Value;
            return je.ValueKind == JsonValueKind.String ? je.GetString() : null;
        }

        private async Task<ConnectorType> GetConnectorSuggestionsFromDynamicSchemaAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, ConnectorDynamicSchema cds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cds, knownParameters, runtimeContext);

            if (newParameters == null)
            {
                return null;
            }

            FormulaValue result = await ConnectorDynamicCallAsync(cds, newParameters, runtimeContext, cancellationToken).ConfigureAwait(false);

            if (result is not StringValue sv)
            {
                runtimeContext.ExecutionLogger?.LogError($"{this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicSchemaAsync))}, result isn't a StringValue but {LogFormulaValue(result)}");
                return null;
            }

            ConnectorType connectorType = GetConnectorType(cds.ValuePath, sv, ConnectorSettings.Compatibility);

            if (connectorType.HasErrors)
            {
                runtimeContext.ExecutionLogger?.LogError($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicSchemaAsync))}, with {LogConnectorType(connectorType)}");
            }
            else
            {
                runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicSchemaAsync))}, with {LogConnectorType(connectorType)}");
            }

            return connectorType;
        }

        internal static ConnectorType GetConnectorType(string valuePath, StringValue sv, ConnectorCompatibility compatibility)
        {
            JsonElement je = ExtractFromJson(sv, valuePath);
            return GetConnectorTypeInternal(compatibility, je);
        }

        // Only called by ConnectorTable.GetSchema
        internal static ConnectorType GetConnectorTypeAndTableCapabilities(string connectorName, string valuePath, StringValue sv, ConnectorCompatibility compatibility, string datasetName, out string name, out string displayName, out ServiceCapabilities tableCapabilities)
        {
            // There are some errors when parsing this Json payload but that's not a problem here as we only need x-ms-capabilities parsing to work
            OpenApiReaderSettings oars = new OpenApiReaderSettings() { RuleSet = DefaultValidationRuleSet };
            IConnectorSchema tableSchema = ConnectorApiSchema.New(new OpenApiStringReader(oars).ReadFragment<OpenApiSchema>(sv.Value, OpenApi.OpenApiSpecVersion.OpenApi2_0, out OpenApiDiagnostic _));
            tableCapabilities = tableSchema.GetTableCapabilities();

            JsonElement je = ExtractFromJson(sv, valuePath, out name, out displayName);

            // Json version to be able to read SalesForce unique properties
            ConnectorType connectorType = GetJsonConnectorTypeInternal(compatibility, je);

            if (tableSchema != null)
            {
                ConnectorPermission tablePermission = tableSchema.GetPermission();
                bool isTableReadOnly = tablePermission == ConnectorPermission.PermissionReadOnly;

                List<ConnectorType> primaryKeyParts = connectorType.Fields.Where(f => f.KeyType == ConnectorKeyType.Primary).OrderBy(f => f.KeyOrder).ToList();

                if (primaryKeyParts.Count == 0)
                {
                    // $$$ need to check what triggers RO for SQL 
                    //isTableReadOnly = true;
                }

                connectorType.AddTabularDataSource(new DName(name), datasetName, connectorType, tableCapabilities, isTableReadOnly);
            }

            // referencedEntities
            if (connectorName == "salesforce")
            {
                // OneToMany relationships that have this table in relation
                JsonElement je2 = ExtractFromJson(sv, "referencedEntities", out name, out displayName);
                List<ReferencedEntity> refEntities = je2.Deserialize<Dictionary<string, SalesForceReferencedEntity>>()
                                                         .Where(kvp => !string.IsNullOrEmpty(kvp.Value.RelationshipName))
                                                         .Select(kvp => new ReferencedEntity()
                                                         {
                                                             FieldName = kvp.Value.Field,
                                                             RelationshipName = kvp.Value.RelationshipName,
                                                             TableName = kvp.Value.ChildSObject
                                                         })
                                                         .ToList();

                connectorType.AddReferencedEntities(refEntities);
            }

            return connectorType;
        }       

        // https://developer.salesforce.com/docs/atlas.en-us.apexref.meta/apexref/apex_class_Schema_ChildRelationship.htm
        internal class SalesForceReferencedEntity
        {
            [JsonPropertyName("cascadeDelete")]
            public bool CascadeDelete { get; set; }

            [JsonPropertyName("childSObject")]
            public string ChildSObject { get; set; }

            [JsonPropertyName("deprecatedAndHidden")]
            public bool DeprecatedAndHidden { get; set; }

            [JsonPropertyName("field")]
            public string Field { get; set; }

            // ManyToMany
            [JsonPropertyName("junctionIdListNames")]
            public IList<string> JunctionIdListNames { get; set; }

            [JsonPropertyName("junctionReferenceTo")]
            public IList<string> JunctionReferenceTo { get; set; }

            [JsonPropertyName("relationshipName")]
            public string RelationshipName { get; set; }

            [JsonPropertyName("restrictedDelete")]
            public bool RestrictedDelete { get; set; }
        }

        private static ConnectorType GetConnectorTypeInternal(ConnectorCompatibility compatibility, JsonElement je)
        {
            OpenApiReaderSettings oars = new OpenApiReaderSettings() { RuleSet = DefaultValidationRuleSet };
            OpenApiSchema schema = new OpenApiStringReader(oars).ReadFragment<OpenApiSchema>(je.ToString(), OpenApi.OpenApiSpecVersion.OpenApi2_0, out OpenApiDiagnostic diag);

            return new ConnectorType(ConnectorApiSchema.New(schema), compatibility);
        }

        private static ConnectorType GetJsonConnectorTypeInternal(ConnectorCompatibility compatibility, JsonElement je)
        {
            return new ConnectorType(je, compatibility);
        }

        private async Task<ConnectorType> GetConnectorSuggestionsFromDynamicPropertyAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, ConnectorDynamicProperty cdp, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdp, knownParameters, runtimeContext);

            if (newParameters == null)
            {
                return null;
            }

            FormulaValue result = await ConnectorDynamicCallAsync(cdp, newParameters, runtimeContext, cancellationToken).ConfigureAwait(false);

            if (result is not StringValue sv)
            {
                runtimeContext.ExecutionLogger?.LogError($"{this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicPropertyAsync))}, result isn't a StringValue but {LogFormulaValue(result)}");
                return null;
            }

            ConnectorType connectorType = GetConnectorType(cdp.ItemValuePath, sv, ConnectorSettings.Compatibility);

            if (connectorType.HasErrors)
            {
                runtimeContext.ExecutionLogger?.LogError($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicPropertyAsync))}, with {LogConnectorType(connectorType)}");
            }
            else
            {
                runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicPropertyAsync))}, with {LogConnectorType(connectorType)}");
            }

            return connectorType;
        }

        internal static ValidationRuleSet DefaultValidationRuleSet
        {
            get
            {
                IList<ValidationRule> rules = ValidationRuleSet.GetDefaultRuleSet().Rules;

                // OpenApiComponentsRules.KeyMustBeRegularExpression is the only rule with this type
                var keyMustBeRegularExpression = rules.First(r => r.GetType() == typeof(ValidationRule<OpenApiComponents>));
                rules.Remove(keyMustBeRegularExpression);

                return new ValidationRuleSet(rules);
            }
        }

        private async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsFromDynamicValueAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, ConnectorDynamicValue cdv, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdv, knownParameters, runtimeContext);

            if (newParameters == null)
            {
                return null;
            }

            FormulaValue result = await ConnectorDynamicCallAsync(cdv, newParameters, runtimeContext, cancellationToken).ConfigureAwait(false);
            List<ConnectorSuggestion> suggestions = new List<ConnectorSuggestion>();

            if (result is not StringValue sv)
            {
                runtimeContext.ExecutionLogger?.LogError($"{this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicValueAsync))}, result isn't a StringValue but {LogFormulaValue(result)}");
                return null;
            }

            JsonElement je = ExtractFromJson(sv, cdv.ValueCollection);

            if (je.ValueKind != JsonValueKind.Array && string.IsNullOrEmpty(cdv.ValueCollection))
            {
                je = ExtractFromJson(sv, "value");
            }

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

            runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicValueAsync))}, with {suggestions.Count} suggestions");
            return new ConnectorEnhancedSuggestions(SuggestionMethod.DynamicValue, suggestions);
        }

        private async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsFromDynamicListAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, ConnectorDynamicList cdl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdl, knownParameters, runtimeContext);

            if (newParameters == null)
            {
                return null;
            }

            FormulaValue result = await ConnectorDynamicCallAsync(cdl, newParameters, runtimeContext, cancellationToken).ConfigureAwait(false);
            List<ConnectorSuggestion> suggestions = new List<ConnectorSuggestion>();

            if (result is not StringValue sv)
            {
                runtimeContext.ExecutionLogger?.LogError($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicListAsync))} with null, result isn't a StringValue but {LogFormulaValue(result)}");
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

            runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicListAsync))}, returning {suggestions.Count} suggestions");
            return new ConnectorEnhancedSuggestions(SuggestionMethod.DynamicList, suggestions);
        }

        private async Task<FormulaValue> ConnectorDynamicCallAsync(ConnectorDynamicApi dynamicApi, FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureConnectorFunction(dynamicApi, GlobalContext.FunctionList);
            if (dynamicApi.ConnectorFunction == null)
            {
                runtimeContext.ExecutionLogger?.LogError($"Exiting {this.LogFunction(nameof(ConnectorDynamicCallAsync))}, {nameof(dynamicApi.ConnectorFunction)} is null");
                return null;
            }

            return await dynamicApi.ConnectorFunction.InvokeInternalAsync(arguments, runtimeContext.WithRawResults(), cancellationToken).ConfigureAwait(false);
        }

        private void EnsureInitialized()
        {
            _internals ??= Initialize();
        }

        private T EnsureConnectorFunction<T>(T dynamicApi, IReadOnlyList<ConnectorFunction> functionList)
            where T : ConnectorDynamicApi
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
            if (RequiredParameters == null)
            {
                return Array.Empty<DType>();
            }

            IEnumerable<DType> parameterTypes = RequiredParameters.Select(parameter => parameter.FormulaType._type);
            if (OptionalParameters.Length != 0)
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
        private FormulaValue[] GetArguments(ConnectorDynamicApi dynamicApi, NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext)
        {
            List<FormulaValue> arguments = new List<FormulaValue>();

            ConnectorFunction functionToBeCalled = EnsureConnectorFunction(dynamicApi, GlobalContext.FunctionList).ConnectorFunction;

            foreach (ConnectorParameter connectorParameter in functionToBeCalled.RequiredParameters)
            {
                string requiredParameterName = connectorParameter.Name;

                // TODO: properly implement the ability to reference child properties instead of simplistic check "GetLastPart(kvp.Key) == requiredParameterName"
                // doc: https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions
                // e.g. make this example working:
                // "destinationInputParam1/property1": {
                //    "parameterReference": "sourceInputParam1/property1"
                // }
                if (dynamicApi.ParameterMap.FirstOrDefault(kvp => kvp.Value is StaticConnectorExtensionValue && GetLastPart(kvp.Key) == requiredParameterName).Value is StaticConnectorExtensionValue sValue)
                {
                    arguments.Add(sValue.Value);
                    continue;
                }

                KeyValuePair<string, IConnectorExtensionValue> dValue = dynamicApi.ParameterMap.FirstOrDefault(kvp => kvp.Value is DynamicConnectorExtensionValue dv && GetLastPart(kvp.Key) == requiredParameterName);
                string[] referenceList = ((DynamicConnectorExtensionValue)dValue.Value).Reference.Split('/');

                var parameterToUse = knownParameters.FirstOrDefault(nv => nv.Name == referenceList.First())?.Value;
                for (int i = 1; i < referenceList.Length && parameterToUse != null; i++)
                {
                    if (parameterToUse is RecordValue recordValue)
                    {
                        parameterToUse = recordValue.GetField(referenceList[i]);
                    }
                    else
                    {
                        runtimeContext.ExecutionLogger?.LogWarning($"Provided parameter is expected to be a record but is {parameterToUse.Type._type}");
                        return null;
                    }
                }

                if (parameterToUse == null || parameterToUse.IsBlank())
                {
                    runtimeContext.ExecutionLogger?.LogWarning($"Missing required property to run suggestions");
                    return null;
                }

                arguments.Add(parameterToUse);
            }

            return arguments.ToArray();
        }

        private string GetLastPart(string str) => str.Split('/').Last();

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
            Dictionary<ConnectorParameter, FormulaValue> openApiBodyParameters = new ();
            string bodySchemaReferenceId = null;
            bool schemaLessBody = false;
            bool fatalError = false;
            string contentType = OpenApiExtensions.ContentType_ApplicationJson;
            ConnectorErrors errorsAndWarnings = new ConnectorErrors();

            foreach (OpenApiParameter parameter in Operation.Parameters)
            {
                bool hiddenRequired = false;

                if (parameter == null)
                {
                    errorsAndWarnings.AddError($"OpenApiParameter is null, this swagger file is probably containing errors");
                    fatalError = true;
                    break;
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
                    else if (ConnectorSettings.Compatibility == ConnectorCompatibility.SwaggerCompatibility)
                    {
                        continue;
                    }
                }

                if (!VerifyCanHandle(parameter.In))
                {
                    return null;
                }

                ConnectorParameter connectorParameter = errorsAndWarnings.AggregateErrorsAndWarnings(new ConnectorParameter(parameter, ConnectorSettings.Compatibility));

                if (connectorParameter.HiddenRecordType != null)
                {
                    errorsAndWarnings.AddError("[Internal error] Unexpected HiddenRecordType non-null value");
                    fatalError = true;
                }

                if (ConnectorApiSchema.New(parameter.Schema).TryGetDefaultValue(connectorParameter.FormulaType, out FormulaValue defaultValue, errorsAndWarnings))
                {
                    parameterDefaultValues[parameter.Name] = (connectorParameter.ConnectorType.IsRequired, defaultValue, connectorParameter.FormulaType._type);
                }

                List<ConnectorParameter> parameterList = !parameter.Required ? optionalParameters : hiddenRequired ? hiddenRequiredParameters : requiredParameters;
                parameterList.Add(connectorParameter);
            }

            if (!fatalError)
            {
                if (Operation.RequestBody != null)
                {
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
                                errorsAndWarnings.AddError("[Body] OpenApiSchema is not supported - AnyOf, Not, AdditionalProperties or Items not array");
                            }
                            else if (bodySchema.AllOf.Any() || bodySchema.Properties.Any())
                            {
                                foreach (KeyValuePair<string, OpenApiSchema> bodyProperty in bodySchema.Properties)
                                {
                                    OpenApiSchema bodyPropertySchema = bodyProperty.Value;
                                    string bodyPropertyName = bodyProperty.Key;
                                    bool bodyPropertyRequired = bodySchema.Required.Contains(bodyPropertyName);
                                    bool bodyPropertyHiddenRequired = false;

                                    if (bodyPropertySchema.IsInternal())
                                    {
                                        if (bodyPropertyRequired)
                                        {
                                            if (bodyPropertySchema.Default == null)
                                            {
                                                continue;
                                            }

                                            bodyPropertyHiddenRequired = ConnectorSettings.Compatibility != ConnectorCompatibility.PowerAppsCompatibility || !requestBody.Required;
                                        }
                                        else if (ConnectorSettings.Compatibility == ConnectorCompatibility.SwaggerCompatibility)
                                        {
                                            continue;
                                        }
                                    }

                                    OpenApiParameter bodyParameter = new OpenApiParameter() { Name = bodyPropertyName, Schema = bodyPropertySchema, Description = requestBody.Description, Required = bodyPropertyRequired, Extensions = bodyPropertySchema.Extensions };
                                    ConnectorParameter bodyConnectorParameter2 = errorsAndWarnings.AggregateErrorsAndWarnings(new ConnectorParameter(bodyParameter, requestBody, ConnectorSettings.Compatibility));
                                    openApiBodyParameters.Add(bodyConnectorParameter2, OpenApiExtensions.TryGetOpenApiValue(bodyConnectorParameter2.Schema.Default, null, out FormulaValue defaultValue, errorsAndWarnings) ? defaultValue : null);

                                    if (bodyConnectorParameter2.HiddenRecordType != null)
                                    {
                                        hiddenRequiredParameters.Add(errorsAndWarnings.AggregateErrorsAndWarnings(new ConnectorParameter(bodyParameter, true, ConnectorSettings.Compatibility)));
                                    }

                                    List<ConnectorParameter> parameterList = !bodyPropertyRequired ? optionalParameters : bodyPropertyHiddenRequired ? hiddenRequiredParameters : requiredParameters;
                                    parameterList.Add(bodyConnectorParameter2);
                                }
                            }
                            else
                            {
                                schemaLessBody = true;

                                if (bodySchema.Type == "string" && bodySchema.Format == "binary")
                                {
                                    // Blob - In Power Apps, when the body parameter is of type "binary", the name of the parameter becomes "file"
                                    // ServiceConfigParser.cs, see DefaultBinaryRequestBodyParameterName reference
                                    bodyName = "file";
                                }

                                OpenApiParameter bodyParameter2 = new OpenApiParameter() { Name = bodyName, Schema = bodySchema, Description = requestBody.Description, Required = requestBody.Required, Extensions = bodySchema.Extensions };
                                ConnectorParameter bodyConnectorParameter3 = errorsAndWarnings.AggregateErrorsAndWarnings(new ConnectorParameter(bodyParameter2, requestBody, ConnectorSettings.Compatibility));
                                openApiBodyParameters.Add(bodyConnectorParameter3, OpenApiExtensions.TryGetOpenApiValue(bodyConnectorParameter3.Schema.Default, null, out FormulaValue defaultValue, errorsAndWarnings) ? defaultValue : null);

                                if (bodyConnectorParameter3.HiddenRecordType != null)
                                {
                                    errorsAndWarnings.AddError("[Internal error] Unexpected HiddenRecordType not-null value for schema-less body");
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
                        ConnectorParameter bodyParameter = errorsAndWarnings.AggregateErrorsAndWarnings(new ConnectorParameter(bodyParameter3, requestBody, ConnectorSettings.Compatibility));
                        openApiBodyParameters.Add(bodyParameter, OpenApiExtensions.TryGetOpenApiValue(bodyParameter.Schema.Default, null, out FormulaValue defaultValue, errorsAndWarnings) ? defaultValue : null);

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
            }

            // Required params are first N params in the final list, "in" parameters first.
            // Optional params are fields on a single record argument at the end.
            // Hidden required parameters do not count here
            _requiredParameters = ConnectorSettings.Compatibility == ConnectorCompatibility.PowerAppsCompatibility ? GetPowerAppsParameterOrder(requiredParameters) : requiredParameters.ToArray();
            _optionalParameters = optionalParameters.ToArray();
            _hiddenRequiredParameters = hiddenRequiredParameters.ToArray();
            _arityMin = _requiredParameters.Length;
            _arityMax = _arityMin + (_optionalParameters.Length == 0 ? 0 : 1);
            _warnings = new List<ErrorResourceKey>();

            _returnType = errorsAndWarnings.AggregateErrorsAndWarnings(Operation.GetConnectorReturnType(ConnectorSettings.Compatibility));

            if (IsDeprecated)
            {
                _warnings.Add(ConnectorStringResources.WarnDeprecatedFunction);
                string msg = ErrorUtils.FormatMessage(StringResources.Get(ConnectorStringResources.WarnDeprecatedFunction), null, Name, Namespace);
                _configurationLogger?.LogWarning($"{msg}");
            }

            if (openApiBodyParameters.Count > 1 && openApiBodyParameters.Any(p => p.Key.ConnectorType.Binary))
            {
                errorsAndWarnings.AddError("Body with multiple parameters is not supported when one of the parameters is of type 'blob'");
            }

            if (errorsAndWarnings.HasErrors)
            {
                foreach (string error in errorsAndWarnings.Errors)
                {
                    _configurationLogger?.LogError($"Function {Name}: {error}");
                }

                SetUnsupported(string.Join(", ", errorsAndWarnings.Errors));
            }

            if (errorsAndWarnings.HasWarnings)
            {
                foreach (ErrorResourceKey warning in errorsAndWarnings.Warnings)
                {
                    string msg = ErrorUtils.FormatMessage(StringResources.Get(warning), null, Name, Namespace);
                    _configurationLogger?.LogWarning($"Function {Name}: {msg}");
                }

                _warnings.AddRange(errorsAndWarnings.Warnings.ToArray());
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

        private ConnectorParameter[] GetPowerAppsParameterOrder(List<ConnectorParameter> parameters)
        {
            List<ConnectorParameter> newList = new List<ConnectorParameter>();

            foreach (ConnectorParameter parameter in parameters)
            {
                if (parameter.Location == ParameterLocation.Path)
                {
                    newList.Add(parameter);
                }
            }

            foreach (ConnectorParameter parameter in parameters)
            {
                if (parameter.Location == ParameterLocation.Query)
                {
                    newList.Add(parameter);
                }
            }

            foreach (ConnectorParameter parameter in parameters)
            {
                if (parameter.Location == ParameterLocation.Header)
                {
                    newList.Add(parameter);
                }
            }

            foreach (ConnectorParameter parameter in parameters)
            {
                if (parameter.Location == null || parameter.Location == ParameterLocation.Cookie)
                {
                    newList.Add(parameter);
                }
            }

            return newList.ToArray();
        }

        private bool VerifyCanHandle(ParameterLocation? location)
        {
            switch (location.Value)
            {
                case ParameterLocation.Path:
                case ParameterLocation.Query:
                case ParameterLocation.Header:
                    return true;

                case ParameterLocation.Cookie:
                default:
                    _configurationLogger?.LogError($"{this.LogFunction(nameof(VerifyCanHandle))}, unsupported {location.Value}");
                    return false;
            }
        }

        internal class CallCounter
        {
            internal int CallsLeft;
        }
    }
}
