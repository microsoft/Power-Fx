// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
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
        public string Namespace { get; private set; }

        /// <summary>
        /// Defines if the function is supported or contains unsupported elements.
        /// </summary>
        public bool IsSupported { get; private set; }

        /// <summary>
        /// Defines if the function is deprecated.
        /// </summary>
        public bool IsDeprecated => Operation.Deprecated;

        /// <summary>
        /// Defines if the function is pageable (using x-ms-pageable extension).
        /// </summary>
        public bool IsPageable => !string.IsNullOrEmpty(PageLink);

        /// <summary>
        /// Page Link as defined in the x-ms-pageable extension.
        /// </summary>
        public string PageLink => Operation.PageLink();

        /// <summary>
        /// Reason for which the function isn't supported.
        /// </summary>
        public string NotSupportedReason { get; private set; }

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
        /// Swagger's operation.
        /// </summary>
        internal OpenApiOperation Operation { get; }

        /// <summary>
        /// Visibility defined as "x-ms-visibility" string content.
        /// </summary>
        public string Visibility => Operation.GetVisibility();

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
        /// Defines behavioral functions (GET and HEAD HTTP methods).
        /// </summary>
        public bool IsBehavior => OpenApiParser.IsSafeHttpMethod(HttpMethod);

        /// <summary>
        /// Required parameters.
        /// </summary>
        public ConnectorParameter[] RequiredParameters => _requiredParameters ??= ArgumentMapper.RequiredParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.ConnectorType, sfpt.Description, sfpt.Summary, sfpt.DefaultValue)).ToArray();

        /// <summary>
        /// Hidden required parameters.
        /// Defined as 
        /// - part "required" swagger array
        /// - "x-ms-visibility" string set to "internal" 
        /// - has a default value.
        /// </summary>
        internal ConnectorParameter[] HiddenRequiredParameters => _hiddenRequiredParameters ??= ArgumentMapper.HiddenRequiredParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.ConnectorType, sfpt.Description, sfpt.Summary, sfpt.DefaultValue)).ToArray();

        /// <summary>
        /// Optional parameters.
        /// </summary>
        public ConnectorParameter[] OptionalParameters => _optionalParameters ??= ArgumentMapper.OptionalParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.ConnectorType, sfpt.Description, sfpt.Summary, sfpt.DefaultValue)).ToArray();

        /// <summary>
        /// Minimum number of arguments.
        /// </summary>
        public int ArityMin => ArgumentMapper.ArityMin;

        /// <summary>
        /// Maximum number of arguments.
        /// </summary>
        public int ArityMax => ArgumentMapper.ArityMax;

        /// <summary>
        /// Numbers are defined as "double" type (otherwise "decimal").
        /// </summary>
        public bool NumberIsFloat => ConnectorSettings.NumberIsFloat;

        public ConnectorSettings ConnectorSettings;

        /// <summary>
        /// ArgumentMapper class.
        /// </summary>
        internal ArgumentMapper ArgumentMapper => _argumentMapper ??= new ArgumentMapper(Operation.Parameters, Operation, NumberIsFloat);

        /// <summary>
        /// True if the function has a service function.
        /// </summary>
        internal bool HasServiceFunction => _defaultServiceFunction != null;

        private ArgumentMapper _argumentMapper;
        private ConnectorParameter[] _requiredParameters;
        private ConnectorParameter[] _hiddenRequiredParameters;
        private ConnectorParameter[] _optionalParameters;
        internal readonly ServiceFunction _defaultServiceFunction;

        public ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod)
            : this(openApiOperation, isSupported, notSupportedReason, name, operationPath, httpMethod, null, null, false, new ConnectorSettings())
        {
        }

        public ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, string @namespace)
            : this(openApiOperation, isSupported, notSupportedReason, name, operationPath, httpMethod, @namespace, null, false, new ConnectorSettings())
        {
        }

        public ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, string @namespace, HttpClient httpClient)
            : this(openApiOperation, isSupported, notSupportedReason, name, operationPath, httpMethod, @namespace, httpClient, false, new ConnectorSettings())
        {
        }

        public ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, string @namespace, HttpClient httpClient, bool throwOnError)
            : this(openApiOperation, isSupported, notSupportedReason, name, operationPath, httpMethod, @namespace, httpClient, throwOnError, new ConnectorSettings())
        {
        }

        public ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, string @namespace, HttpClient httpClient, bool throwOnError, bool numberIsFloat)
            : this(openApiOperation, isSupported, notSupportedReason, name, operationPath, httpMethod, @namespace, httpClient, throwOnError, new ConnectorSettings() { NumberIsFloat = numberIsFloat })
        {
        }

        public ConnectorFunction(OpenApiOperation openApiOperation, bool isSupported, string notSupportedReason, string name, string operationPath, HttpMethod httpMethod, string @namespace, HttpClient httpClient, bool throwOnError, ConnectorSettings connectorSettings)
        {
            Operation = openApiOperation ?? throw new ArgumentNullException(nameof(openApiOperation));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OperationPath = operationPath ?? throw new ArgumentNullException(nameof(operationPath));
            HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
            ConnectorSettings = connectorSettings;
            IsSupported = isSupported;
            NotSupportedReason = notSupportedReason ?? (isSupported ? string.Empty : throw new ArgumentNullException(nameof(notSupportedReason)));

            if (httpClient != null)
            {
                _defaultServiceFunction = GetServiceFunction(@namespace, httpClient, throwOnError: throwOnError, connectorSettings);
            }

            if (isSupported)
            {
                // validate return type
                (ConnectorParameterType ct, string unsupportedReason) = openApiOperation.GetConnectorParameterReturnType(connectorSettings.NumberIsFloat);

                if (!string.IsNullOrEmpty(unsupportedReason))
                {
                    IsSupported = false;
                    NotSupportedReason = unsupportedReason;
                }
            }
        }

        public ConnectorParameters GetParameters(FormulaValue[] knownParameters)
        {
            ConnectorParameterWithSuggestions[] parametersWithSuggestions = RequiredParameters.Select((rp, i) => i < ArityMax - 1
                                                                                                            ? new ConnectorParameterWithSuggestions(rp, i < knownParameters.Length ? knownParameters[i] : null)
                                                                                                            : new ConnectorParameterWithSuggestions(rp, knownParameters.Skip(ArityMax - 1).ToArray())).ToArray();

            bool error = true;

            if (HasServiceFunction)
            {
                int index = Math.Min(knownParameters.Length, _defaultServiceFunction.MaxArity - 1);
                ConnectorSuggestions suggestions = _defaultServiceFunction.GetConnectorSuggestionsAsync(knownParameters, knownParameters.Length, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                error = suggestions == null || suggestions.Error != null;

                if (!error)
                {
                    if (knownParameters.Length >= ArityMax - 1)
                    {
                        parametersWithSuggestions[index].ParameterNames = suggestions.Suggestions.Select(s => s.DisplayName).ToArray();
                        suggestions.Suggestions = suggestions.Suggestions.Skip(knownParameters.Length - ArityMax + 1).ToList();
                    }

                    parametersWithSuggestions[index].Suggestions = suggestions.Suggestions;
                }
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

        internal ServiceFunction GetServiceFunction(string ns = null, HttpMessageInvoker httpClient = null, bool throwOnError = false, ConnectorSettings connectorSettings = null)
        {
            ScopedHttpFunctionInvoker invoker = null;
            string func_ns = string.IsNullOrEmpty(ns) ? "Internal_Function" : ns;
            DPath functionNamespace = DPath.Root.Append(new DName(func_ns));
            Namespace = func_ns;
            connectorSettings ??= new ConnectorSettings();

            if (httpClient != null)
            {
                var httpInvoker = new HttpFunctionInvoker(httpClient, HttpMethod, OperationPath, ReturnType, ArgumentMapper, connectorSettings.Cache);
                invoker = new ScopedHttpFunctionInvoker(DPath.Root.Append(DName.MakeValid(func_ns, out _)), Name, func_ns, httpInvoker, throwOnError);
            }

            ServiceFunction serviceFunction = new ServiceFunction(
                parentService: null,
                theNamespace: functionNamespace,
                name: Name,
                localeSpecificName: Name,
                description: Description,
                returnType: ReturnType._type,
                maskLambdas: BigInteger.Zero,
                arityMin: ArityMin,
                arityMax: ArityMax,
                isBehaviorOnly: IsBehavior,
                isAutoRefreshable: false,
                isDynamic: false,
                isCacheEnabled: false,
                cacheTimeoutMs: 10000,
                isHidden: false,
                parameterOptions: new Dictionary<TypedName, List<string>>(),
                optionalParamInfo: ArgumentMapper.OptionalParamInfo,
                requiredParamInfo: ArgumentMapper.RequiredParamInfo,
                parameterDefaultValues: new Dictionary<string, Tuple<string, DType>>(StringComparer.Ordinal),
                pageLink: PageLink,
                isSupported: IsSupported,
                notSupportedReason: NotSupportedReason,
                isDeprecated: IsDeprecated,
                actionName: "action",
                connectorSettings: connectorSettings,
                paramTypes: ArgumentMapper._parameterTypes)
            {
                _invoker = invoker
            };

            return serviceFunction;
        }

        internal async Task<FormulaValue> InvokeAync(FormattingInfo context, HttpClient httpClient, FormulaValue[] values, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetServiceFunction(null, httpClient).InvokeAsync(context, values, cancellationToken).ConfigureAwait(false);
        }

        public Task<FormulaValue> InvokeAync(IRuntimeConfig config, HttpClient httpClient, FormulaValue[] values, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return InvokeAync(config.ServiceProvider.GetFormattingInfo(), httpClient, values, cancellationToken);
        }

        public Task<FormulaValue> InvokeAync(HttpClient httpClient, FormulaValue[] values, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return InvokeAync(FormattingInfoHelper.CreateFormattingInfo(), httpClient, values, cancellationToken);
        }
    }

    [DebuggerDisplay("{Name} {FormulaType._type}")]
    public class ConnectorParameter
    {
        /// <summary>
        /// Parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Parameter type.
        /// </summary>
        public FormulaType FormulaType { get; }

        /// <summary>
        /// Parameter ConnectorType.
        /// </summary>
        public ConnectorType ConnectorType { get; }

        /// <summary>
        /// Parameter description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Parameter summary (defined in "x-ms-summary").
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// Parameter default value.
        /// </summary>
        public FormulaValue DefaultValue { get; }

        public ConnectorParameter(string name, FormulaType type, ConnectorType connectorType, string description, string summary, FormulaValue defaultValue)
        {
            Name = name;
            FormulaType = type;
            ConnectorType = connectorType;
            Description = description;
            Summary = summary;
            DefaultValue = defaultValue;
        }
    }

    public class ConnectorParameterWithSuggestions : ConnectorParameter
    {
        public IReadOnlyList<ConnectorSuggestion> Suggestions { get; internal set; }

        public FormulaValue Value { get; private set; }

        public FormulaValue[] Values { get; private set; }

        public string[] ParameterNames { get; internal set; }

        public ConnectorParameterWithSuggestions(string name, FormulaType type, ConnectorType connectorType, string description, string summary, FormulaValue defaultValue)
            : base(name, type, connectorType, description, summary, defaultValue)
        {
            Suggestions = new List<ConnectorSuggestion>();
        }

        public ConnectorParameterWithSuggestions(ConnectorParameter connectorParameter, FormulaValue value)
            : base(connectorParameter.Name, connectorParameter.FormulaType, connectorParameter.ConnectorType, connectorParameter.Description, connectorParameter.Summary, connectorParameter.DefaultValue)
        {
            Suggestions = new List<ConnectorSuggestion>();
            Value = value;
            Values = null;
        }

        public ConnectorParameterWithSuggestions(ConnectorParameter connectorParameter, FormulaValue[] values)
            : base(connectorParameter.Name, connectorParameter.FormulaType, connectorParameter.ConnectorType, connectorParameter.Description, connectorParameter.Summary, connectorParameter.DefaultValue)
        {
            Suggestions = new List<ConnectorSuggestion>();
            Value = null;
            Values = values.ToArray();
        }
    }

    public class ConnectorParameters
    {
        public bool IsCompleted { get; internal set; }

        public ConnectorParameterWithSuggestions[] Parameters { get; internal set; }
    }

    internal static class Extensions
    {
        internal static string PageLink(this OpenApiOperation op)
            => op.Extensions.TryGetValue("x-ms-pageable", out IOpenApiExtension ext) &&
               ext is OpenApiObject oao &&
               oao.Any() &&
               oao.First().Key == "nextLinkName" &&
               oao.First().Value is OpenApiString oas
            ? oas.Value
            : null;
    }
}
