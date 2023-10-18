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
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
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
        public FormulaType ReturnType => Operation.GetReturnType();

        /// <summary>
        /// Connector return type of the function (contains inner "x-ms-summary" and descriptions).
        /// </summary>
        internal ConnectorType ConnectorReturnType => Operation.GetConnectorReturnType().ConnectorType;

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
        internal ConnectorDynamicSchema DynamicReturnSchema => EnsureConnectorFunction(ReturnParameterType?.DynamicSchema, FunctionList);

        /// <summary>
        /// Dynamic schema extension on return type (response).
        /// </summary>
        internal ConnectorDynamicProperty DynamicReturnProperty => EnsureConnectorFunction(ReturnParameterType?.DynamicProperty, FunctionList);

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

        /// <summary>
        /// List of functions in the same swagger file. Used for resolving dynamic schema/property.
        /// </summary>
        internal IReadOnlyList<ConnectorFunction> FunctionList { get; }

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

        private readonly ConnectorLogger _configurationLogger = null;

        internal ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, ConnectorSettings connectorSettings, List<ConnectorFunction> functionList, ConnectorLogger configurationLogger)
        {
            Operation = openApiOperation;
            Name = name;
            OperationPath = operationPath;
            HttpMethod = httpMethod;
            ConnectorSettings = connectorSettings;
            FunctionList = functionList;

            _configurationLogger = configurationLogger;
            _isSupported = isSupported || connectorSettings.AllowUnsupportedFunctions;
            _notSupportedReason = notSupportedReason ?? (isSupported ? string.Empty : "Internal error on not supported reason");
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

                if (connectorType.DynamicValues != null && string.IsNullOrEmpty(connectorType.DynamicValues.Capability))
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

                ConnectorType result = await GetConnectorTypeInternalAsync(knownParameters, connectorParameter.ConnectorType ?? ReturnParameterType, runtimeContext, cancellationToken).ConfigureAwait(false);
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
                ConnectorType connectorType2 = await GetConnectorTypeInternalAsync(knownParameters, connectorType, runtimeContext, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorTypeAsync))}, returning from {nameof(GetConnectorTypeInternalAsync)} with {LogConnectorType(connectorType2)}");
                return connectorType2;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorTypeAsync))}, Context {LogKnownParameters(knownParameters)} {LogConnectorType(connectorType)}, {LogException(ex)}");
                throw;
            }
        }

        internal async Task<ConnectorType> GetConnectorTypeInternalAsync(NamedValue[] knownParameters, ConnectorType connectorType, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runtimeContext.ExecutionLogger?.LogDebug($"Entering in {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, with {LogKnownParameters(knownParameters)} for {LogConnectorType(connectorType)}");

            if (connectorType.DynamicProperty != null && !string.IsNullOrEmpty(connectorType.DynamicProperty.ItemValuePath))
            {
                ConnectorType result = await GetConnectorSuggestionsFromDynamicPropertyAsync(knownParameters, runtimeContext, connectorType.DynamicProperty, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicPropertyAsync)} with {LogConnectorType(result)}");
                return result;
            }
            else if (connectorType.DynamicSchema != null && !string.IsNullOrEmpty(connectorType.DynamicSchema.ValuePath))
            {
                ConnectorType result = await GetConnectorSuggestionsFromDynamicSchemaAsync(knownParameters, runtimeContext, connectorType.DynamicSchema, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning from {nameof(GetConnectorSuggestionsFromDynamicSchemaAsync)} with {LogConnectorType(result)}");
                return result;
            }

            runtimeContext.ExecutionLogger?.LogWarning($"Exiting {this.LogFunction(nameof(GetConnectorTypeInternalAsync))}, returning null as no dynamic extension defined for {LogConnectorType(connectorType)}");
            return null;
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
                ConnectorType connectorType = await GetConnectorTypeAsync(knownParameters, ReturnParameterType, runtimeContext, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetConnectorReturnTypeAsync))}, returning {nameof(GetConnectorTypeAsync)}, with {LogConnectorType(connectorType)}");
                return connectorType;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(GetConnectorReturnTypeAsync))}, Context {LogKnownParameters(knownParameters)}, {LogException(ex)}");
                throw;
            }
        }

        internal async Task<ConnectorType> GetConnectorReturnTypeInternalAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runtimeContext.ExecutionLogger?.LogDebug($"Entering in {this.LogFunction(nameof(GetConnectorReturnTypeInternalAsync))}, with {LogKnownParameters(knownParameters)}");

            ConnectorType connectorType = await GetConnectorTypeInternalAsync(knownParameters, ReturnParameterType, runtimeContext, cancellationToken).ConfigureAwait(false);
            runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorReturnTypeInternalAsync))}, returning {nameof(GetConnectorTypeInternalAsync)}, with {LogConnectorType(connectorType)}");
            return connectorType;
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
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(InvokeAsync))}, with {LogArguments(arguments)}");
                FormulaValue formulaValue = await InvokeInternalAsync(arguments, runtimeContext, cancellationToken).ConfigureAwait(false);
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(InvokeAsync))}, returning from {nameof(InvokeInternalAsync)}, with {LogFormulaValue(formulaValue)}");
                return formulaValue;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in {this.LogFunction(nameof(InvokeAsync))}, Context {LogArguments(arguments)}, {LogException(ex)}");
                throw;
            }
        }

        internal async Task<FormulaValue> InvokeInternalAsync(FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureInitialized();
            runtimeContext.ExecutionLogger?.LogDebug($"Entering in {this.LogFunction(nameof(InvokeInternalAsync))}, with {LogArguments(arguments)}");
            BaseRuntimeConnectorContext context = ConnectorReturnType.Binary ? runtimeContext.WithRawResults() : runtimeContext;

            IConnectorInvoker invoker = context.GetInvoker(this);
            FormulaValue result = await invoker.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
            FormulaValue formulaValue = await PostProcessResultAsync(result, invoker, cancellationToken).ConfigureAwait(false);

            runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(InvokeInternalAsync))}, returning {LogFormulaValue(formulaValue)}");
            return formulaValue;
        }

        private async Task<FormulaValue> PostProcessResultAsync(FormulaValue result, IConnectorInvoker invoker, CancellationToken cancellationToken)
        {
            ExpressionError er = null;

            if (result is ErrorValue ev && (er = ev.Errors.FirstOrDefault(e => e.Kind == ErrorKind.Network)) != null)
            {
                invoker.Context.ExecutionLogger?.LogError($"{this.LogFunction(nameof(PostProcessResultAsync))}, ErrorValue is returned with {er.Message}");
                result = FormulaValue.NewError(new ExpressionError() { Kind = er.Kind, Severity = er.Severity, Message = $"{DPath.Root.Append(new DName(Namespace)).ToDottedSyntax()}.{Name} failed: {er.Message}" }, ev.Type);
            }

            if (IsPageable && result is RecordValue rv)
            {
                FormulaValue pageLink = rv.GetField(PageLink);
                string nextLink = (pageLink as StringValue)?.Value;

                // If there is no next link, we'll return a "normal" RecordValue as no paging is needed
                if (!string.IsNullOrEmpty(nextLink))
                {
                    result = new PagedRecordValue(rv, async () => await GetNextPageAsync(nextLink, invoker, cancellationToken).ConfigureAwait(false), ConnectorSettings.MaxRows, cancellationToken);
                }
            }

            return result;
        }

        // Can return 3 possible FormulaValues
        // - PagesRecordValue if the next page has a next link
        // - RecordValue if there is no next link
        // - ErrorValue
        private async Task<FormulaValue> GetNextPageAsync(string nextLink, IConnectorInvoker invoker, CancellationToken cancellationToken)            
        {
            cancellationToken.ThrowIfCancellationRequested();
            invoker.Context.ExecutionLogger?.LogInformation($"Entering in {this.LogFunction(nameof(GetNextPageAsync))}, getting next page");
            FormulaValue result = await invoker.InvokeAsync(nextLink, cancellationToken).ConfigureAwait(false);
            result = await PostProcessResultAsync(result, invoker, cancellationToken).ConfigureAwait(false);
            invoker.Context.ExecutionLogger?.LogInformation($"Exiting {this.LogFunction(nameof(GetNextPageAsync))} with {LogFormulaValue(result)}");
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

        private async Task<ConnectorType> GetConnectorSuggestionsFromDynamicSchemaAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, ConnectorDynamicSchema cds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cds, knownParameters);

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

            JsonElement je = ExtractFromJson(sv, cds.ValuePath);
            OpenApiSchema schema = new OpenApiStringReader().ReadFragment<OpenApiSchema>(je.ToString(), Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0, out OpenApiDiagnostic diag);
            ConnectorType connectorType = new ConnectorType(schema);

            runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicSchemaAsync))}, with {LogConnectorType(connectorType)}");
            return connectorType;
        }

        private async Task<ConnectorType> GetConnectorSuggestionsFromDynamicPropertyAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, ConnectorDynamicProperty cdp, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdp, knownParameters);

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

            JsonElement je = ExtractFromJson(sv, cdp.ItemValuePath);
            OpenApiSchema schema = new OpenApiStringReader().ReadFragment<OpenApiSchema>(je.ToString(), Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0, out OpenApiDiagnostic diag);
            ConnectorType connectorType = new ConnectorType(schema);

            runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicPropertyAsync))}, with {LogConnectorType(connectorType)}");
            return connectorType;
        }

        private async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsFromDynamicValueAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, ConnectorDynamicValue cdv, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdv, knownParameters);

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

            runtimeContext.ExecutionLogger?.LogDebug($"Exiting {this.LogFunction(nameof(GetConnectorSuggestionsFromDynamicValueAsync))}, with {suggestions.Count} suggestions");
            return new ConnectorEnhancedSuggestions(SuggestionMethod.DynamicValue, suggestions);
        }

        private async Task<ConnectorEnhancedSuggestions> GetConnectorSuggestionsFromDynamicListAsync(NamedValue[] knownParameters, BaseRuntimeConnectorContext runtimeContext, ConnectorDynamicList cdl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] newParameters = GetArguments(cdl, knownParameters);

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

        private async Task<FormulaValue> ConnectorDynamicCallAsync(ConnectionDynamicApi dynamicApi, FormulaValue[] arguments, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureConnectorFunction(dynamicApi, FunctionList);
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
            if (RequiredParameters == null)
            {
                return Array.Empty<DType>();
            }

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

            ConnectorFunction functionToBeCalled = EnsureConnectorFunction(dynamicApi, FunctionList).ConnectorFunction;

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
                        _configurationLogger?.LogError("OpenApiParameters cannot be null, this swagger file is probably containing errors");
                        return null;
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

                    ConnectorParameter connectorParameter = new ConnectorParameter(parameter);

                    if (connectorParameter.HiddenRecordType != null)
                    {
                        throw new NotImplementedException("Unexpected value for a parameter");
                    }

                    if (parameter.Schema.TryGetDefaultValue(connectorParameter.FormulaType, out FormulaValue defaultValue))
                    {
                        parameterDefaultValues[parameter.Name] = (connectorParameter.ConnectorType.IsRequired, defaultValue, connectorParameter.FormulaType._type);
                    }

                    List<ConnectorParameter> parameterList = !parameter.Required ? optionalParameters : hiddenRequired ? hiddenRequiredParameters : requiredParameters;
                    parameterList.Add(connectorParameter);
                }

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
                                        if (bodyPropertyRequired)
                                        {
                                            if (bodyPropertySchema.Default == null)
                                            {
                                                continue;
                                            }

                                            bodyPropertyHiddenRequired = ConnectorSettings.Compatibility == ConnectorCompatibility.PowerAppsCompatibility ? !requestBody.Required : true;
                                        }
                                        else if (ConnectorSettings.Compatibility == ConnectorCompatibility.SwaggerCompatibility)
                                        {
                                            continue;
                                        }
                                    }

                                    OpenApiParameter bodyParameter = new OpenApiParameter() { Name = bodyPropertyName, Schema = bodyPropertySchema, Description = requestBody.Description, Required = bodyPropertyRequired, Extensions = bodyPropertySchema.Extensions };
                                    openApiBodyParameters.Add(bodyParameter);
                                    ConnectorParameter bodyConnectorParameter2 = new ConnectorParameter(bodyParameter, requestBody);

                                    if (bodyConnectorParameter2.HiddenRecordType != null)
                                    {
                                        hiddenRequiredParameters.Add(new ConnectorParameter(bodyParameter, true));
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

                                ConnectorParameter bodyConnectorParameter3 = new ConnectorParameter(bodyParameter2, requestBody);

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

                        ConnectorParameter bodyParameter = new ConnectorParameter(bodyParameter3, requestBody);

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

                // Required params are first N params in the final list, "in" parameters first.
                // Optional params are fields on a single record argument at the end.
                // Hidden required parameters do not count here            
                _requiredParameters = ConnectorSettings.Compatibility == ConnectorCompatibility.PowerAppsCompatibility ? GetPowerAppsParameterOrder(requiredParameters) : requiredParameters.ToArray();
                _optionalParameters = optionalParameters.ToArray();
                _hiddenRequiredParameters = hiddenRequiredParameters.ToArray();
                _arityMin = _requiredParameters.Length;
                _arityMax = _arityMin + (_optionalParameters.Length == 0 ? 0 : 1);

                (ConnectorType connectorType, string unsupportedReason) = Operation.GetConnectorReturnType();
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
    }
}
