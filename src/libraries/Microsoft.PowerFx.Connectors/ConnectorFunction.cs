// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppMagic.Authoring;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorFunction
    {
        public string Name { get; }

        public string Description => Operation.Description ?? $"Invoke {Name}";

        public string Summary => Operation.Summary;

        public string OperationPath { get; }

        public HttpMethod HttpMethod { get; }

        internal OpenApiOperation Operation { get; }

        public FormulaType ReturnType => Operation.GetReturnType();

        public bool IsBehavior => OpenApiParser.IsSafeHttpMethod(HttpMethod);

        public ConnectorParameter[] RequiredParameters => _requiredParameters ??= ArgumentMapper.RequiredParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.Description, sfpt.DefaultValue)).ToArray();

        public ConnectorParameter[] HiddenRequiredParameters => _hiddenRequiredParameters ??= ArgumentMapper.HiddenRequiredParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.Description, sfpt.DefaultValue)).ToArray();

        public ConnectorParameter[] OptionalParameters => _optionalParameters ??= ArgumentMapper.OptionalParamInfo.Select(sfpt => new ConnectorParameter(sfpt.TypedName.Name, sfpt.FormulaType, sfpt.Description, sfpt.DefaultValue)).ToArray();

        public int ArityMin => ArgumentMapper.ArityMin;

        public int ArityMax => ArgumentMapper.ArityMax;

        internal ArgumentMapper ArgumentMapper => _argumentMapper ??= new ArgumentMapper(Operation.Parameters, Operation);

        private ArgumentMapper _argumentMapper;
        private ConnectorParameter[] _requiredParameters;
        private ConnectorParameter[] _hiddenRequiredParameters;
        private ConnectorParameter[] _optionalParameters;

        public ConnectorFunction(OpenApiOperation openApiOperation, string name, string operationPath, HttpMethod httpMethod)
        {
            Operation = openApiOperation ?? throw new ArgumentNullException(nameof(openApiOperation));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OperationPath = operationPath ?? throw new ArgumentNullException(nameof(operationPath));
            HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        }

        internal ServiceFunction GetServiceFunction(HttpClient httpClient = null, ICachingHttpClient cache = null)
        {
            IAsyncTexlFunction invoker = null;
            string functionName = "Internal_Function";
            DPath functionNamespace = DPath.Root.Append(new DName(functionName));

            if (httpClient != null)
            {
                var httpInvoker = new HttpFunctionInvoker(httpClient, HttpMethod, OperationPath, ReturnType, ArgumentMapper, cache);
                invoker = new ScopedHttpFunctionInvoker(DPath.Root.Append(DName.MakeValid(functionName, out _)), Name, functionName, httpInvoker);
            }

#pragma warning disable SA1117 // parameters should be on same line or all on different lines

            return new ServiceFunction(null, functionNamespace, Name, Name, Description, ReturnType._type, BigInteger.Zero, ArityMin, ArityMax, IsBehavior, false, false, false, 10000, false, new Dictionary<TypedName, List<string>>(),
                ArgumentMapper.OptionalParamInfo, ArgumentMapper.RequiredParamInfo, new Dictionary<string, Tuple<string, DType>>(StringComparer.Ordinal), "action", ArgumentMapper._parameterTypes)
            {
                _invoker = invoker
            };

#pragma warning restore SA1117
        }

        public async Task<FormulaValue> InvokeAync(HttpClient httpClient, FormulaValue[] values, CancellationToken cancellationToken)
        {
            ServiceFunction svcFunction = GetServiceFunction(httpClient);
            FormulaValue[] v = values;            

            return await svcFunction.InvokeAsync(v, cancellationToken).ConfigureAwait(false);
        }
    }

    public class ConnectorParameter
    {
        public string Name { get; }

        public FormulaType FormulaType { get; }

        public string Description { get; }

        public FormulaValue DefaultValue { get; }

        public ConnectorParameter(string name, FormulaType type, string description, FormulaValue defaultValue)
        {
            Name = name;
            FormulaType = type;
            Description = description;
            DefaultValue = defaultValue;
        }
    }
}
