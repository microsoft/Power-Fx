// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    [DebuggerDisplay("{Name}")]
    public class ConnectorFunction
    {
        public string Name { get; }

        public string Namespace { get; private set; }

        public string OriginalName => Operation.OperationId;

        public string Description => Operation.Description ?? $"Invoke {Name}";

        public string Summary => Operation.Summary;

        public string OperationPath { get; }

        public HttpMethod HttpMethod { get; }

        internal OpenApiOperation Operation { get; }

        public FormulaType ReturnType => Operation.GetReturnType();

        public bool IsBehavior => OpenApiParser.IsSafeHttpMethod(HttpMethod);

        public ConnectorParameter[] RequiredParameters => _requiredParameters ??= ArgumentMapper.RequiredParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.Description, sfpt.Summary, sfpt.DefaultValue)).ToArray();

        internal ConnectorParameter[] HiddenRequiredParameters => _hiddenRequiredParameters ??= ArgumentMapper.HiddenRequiredParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.Description, sfpt.Summary, sfpt.DefaultValue)).ToArray();

        public ConnectorParameter[] OptionalParameters => _optionalParameters ??= ArgumentMapper.OptionalParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.Description, sfpt.Summary, sfpt.DefaultValue)).ToArray();

        public int ArityMin => ArgumentMapper.ArityMin;

        public int ArityMax => ArgumentMapper.ArityMax;

        internal ArgumentMapper ArgumentMapper => _argumentMapper ??= new ArgumentMapper(Operation.Parameters, Operation);

        internal bool HasServiceFunction => _serviceFunction != null;

        private ArgumentMapper _argumentMapper;
        private ConnectorParameter[] _requiredParameters;
        private ConnectorParameter[] _hiddenRequiredParameters;
        private ConnectorParameter[] _optionalParameters;
        internal ServiceFunction _serviceFunction;

        public ConnectorFunction(OpenApiOperation openApiOperation, string name, string operationPath, HttpMethod httpMethod, string @namespace = null, HttpClient httpClient = null, bool throwOnError = false)
        {
            Operation = openApiOperation ?? throw new ArgumentNullException(nameof(openApiOperation));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OperationPath = operationPath ?? throw new ArgumentNullException(nameof(operationPath));
            HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));

            if (httpClient != null)
            {
                GetServiceFunction(@namespace, httpClient, throwOnError: throwOnError);
            }
        }

        public ConnectorParameters GetParameters(FormulaValue[] knownParameters)
        {
            ConnectorParameterWithSuggestions[] parametersWithSuggestions = RequiredParameters.Select((rp, i) => i < ArityMax - 1
                                                                                                            ? new ConnectorParameterWithSuggestions(rp, i < knownParameters.Length ? knownParameters[i] : null)
                                                                                                            : new ConnectorParameterWithSuggestions(rp, knownParameters.Skip(ArityMax - 1).ToArray())).ToArray();

            if (HasServiceFunction)
            {
                int index = Math.Min(knownParameters.Length, _serviceFunction.MaxArity - 1);
                List<RecordValue> suggestions = _serviceFunction.GetConnectorSuggestionsAsync(knownParameters, knownParameters.Length, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                
                if (knownParameters.Length >= ArityMax - 1)
                {
                    parametersWithSuggestions[index].ParameterNames = suggestions.Select(rv => rv.Fields.First().Name).ToArray();
                    suggestions = suggestions.Skip(knownParameters.Length - ArityMax + 1).ToList();
                }
                
                parametersWithSuggestions[index].Suggestions = suggestions;
            }

            return new ConnectorParameters()
            {
                IsCompleted = parametersWithSuggestions.All(p => !p.Suggestions.Any()),
                Parameters = parametersWithSuggestions
            };
        }

        public string GetExpression(string @namespace, ConnectorParameters parameters)
        {
            if (!parameters.IsCompleted)
            {
                return null;
            }

            string expr = $@"{@namespace}.{Name}({string.Join(", ", parameters.Parameters.Take(Math.Min(parameters.Parameters.Length, ArityMax - 1)).Select(p => p.Value.ToExpression()))}";

            if (parameters.Parameters.Length > ArityMax - 1)
            {
                ConnectorParameterWithSuggestions lastParam = parameters.Parameters.Last();
                expr += $@", {{ {string.Join(", ", lastParam.ParameterNames.Zip(lastParam.Values, (name, fv) => $"{name}: {fv.ToExpression()}"))} }}";
            }

            expr += ")";

            return expr;
        }

        internal ServiceFunction GetServiceFunction(string ns = null, HttpClient httpClient = null, ICachingHttpClient cache = null, bool throwOnError = false)
        {
            if (_serviceFunction == null)
            {
                IAsyncTexlFunction invoker = null;
                string func_ns = string.IsNullOrEmpty(ns) ? "Internal_Function" : ns;
                DPath functionNamespace = DPath.Root.Append(new DName(func_ns));
                Namespace = func_ns;

                if (httpClient != null)
                {
                    var httpInvoker = new HttpFunctionInvoker(httpClient, HttpMethod, OperationPath, ReturnType, ArgumentMapper, cache);
                    invoker = new ScopedHttpFunctionInvoker(DPath.Root.Append(DName.MakeValid(func_ns, out _)), Name, func_ns, httpInvoker, throwOnError);
                }

#pragma warning disable SA1117 // parameters should be on same line or all on different lines

                _serviceFunction = new ServiceFunction(null, functionNamespace, Name, Name, Description, ReturnType._type, BigInteger.Zero, ArityMin, ArityMax, IsBehavior, false, false, false, 10000, false, new Dictionary<TypedName, List<string>>(),
                    ArgumentMapper.OptionalParamInfo, ArgumentMapper.RequiredParamInfo, new Dictionary<string, Tuple<string, DType>>(StringComparer.Ordinal), "action", ArgumentMapper._parameterTypes)
                {
                    _invoker = invoker
                };

#pragma warning restore SA1117
            }

            return _serviceFunction;
        }

        public async Task<FormulaValue> InvokeAync(HttpClient httpClient, FormulaValue[] values, CancellationToken cancellationToken)
        {
            ServiceFunction svcFunction = GetServiceFunction(null, httpClient);
            FormulaValue[] v = values;

            return await svcFunction.InvokeAsync(v, cancellationToken).ConfigureAwait(false);
        }
    }

    public class ConnectorParameter
    {
        public string Name { get; }

        public FormulaType FormulaType { get; }

        public string Description { get; }

        public string Summary { get; }

        public FormulaValue DefaultValue { get; }

        public ConnectorParameter(string name, FormulaType type, string description, string summary, FormulaValue defaultValue)
        {
            Name = name;
            FormulaType = type;
            Description = description;
            Summary = summary;
            DefaultValue = defaultValue;
        }
    }

    public class ConnectorParameterWithSuggestions : ConnectorParameter
    {
        public List<RecordValue> Suggestions { get; internal set; }

        public FormulaValue Value { get; private set; }
        
        public FormulaValue[] Values { get; private set; }

        public string[] ParameterNames { get; internal set; }

        public ConnectorParameterWithSuggestions(string name, FormulaType type, string description, string summary, FormulaValue defaultValue)
            : base(name, type, description, summary, defaultValue)
        {
            Suggestions = new List<RecordValue>();
        }

        public ConnectorParameterWithSuggestions(ConnectorParameter connectorParameter, FormulaValue value)
            : base(connectorParameter.Name, connectorParameter.FormulaType, connectorParameter.Description, connectorParameter.Summary, connectorParameter.DefaultValue)
        {
            Suggestions = new List<RecordValue>();
            Value = value;
            Values = null;
        }

        public ConnectorParameterWithSuggestions(ConnectorParameter connectorParameter, FormulaValue[] values)
            : base(connectorParameter.Name, connectorParameter.FormulaType, connectorParameter.Description, connectorParameter.Summary, connectorParameter.DefaultValue)
        {
            Suggestions = new List<RecordValue>();
            Value = null;
            Values = values.ToArray();
        }
    }

    public class ConnectorParameters
    {
        public bool IsCompleted { get; internal set; }

        public ConnectorParameterWithSuggestions[] Parameters { get; internal set; }
    }
}
