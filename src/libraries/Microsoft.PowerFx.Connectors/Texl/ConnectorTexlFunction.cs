// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Publish;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.ConnectorHelperFunctions;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorTexlFunction : TexlFunction, IHasUnsupportedFunctions
    {
        public ConnectorFunction ConnectorFunction { get; }

        internal ConnectorTexlFunction(ConnectorFunction function)
            : base(DPath.Root.Append(new DName(function.Namespace)), function.Name, function.Name, (locale) => function.Description, FunctionCategories.REST, function.ReturnType._type, BigInteger.Zero, function.ArityMin, function.ArityMax, function.ParameterTypes)
        {
            ConnectorFunction = function;
        }

        public bool IsDeprecated => ConnectorFunction.IsDeprecated;

        public bool IsInternal => ConnectorFunction.IsInternal;

        public bool IsNotSupported => !ConnectorFunction.IsSupported;

        public string NotSupportedReason => ConnectorFunction.NotSupportedReason;

        public override bool IsBehaviorOnly => ConnectorFunction.IsBehavior;

        public override bool IsSelfContained => !ConnectorFunction.IsBehavior;

        public override bool IsAsync => true;

        public override bool IsStateless => false;

        public override bool IsAutoRefreshable => false;

        public override bool RequireAllParamColumns => true;

        public override string HelpLink => string.Empty;

        public override Capabilities Capabilities => Capabilities.OutboundInternetAccess | Capabilities.EnterpriseAuthentication | Capabilities.PrivateNetworkAccess;

        // Used by Intellisense, otherwise unused for InvokeAsync.
        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return ConnectorFunction.RequiredParameters.Select<ConnectorParameter, TexlStrings.StringGetter>(p => (locale) => p.Name).ToArray();
            yield return ConnectorFunction.OptionalParameters.Select<ConnectorParameter, TexlStrings.StringGetter>(p => (locale) => p.Name).ToArray();
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            paramDescription = ConnectorFunction.RequiredParameters.FirstOrDefault(p => p.Name == paramName)?.Description ?? ConnectorFunction.OptionalParameters.FirstOrDefault(p => p.Name == paramName)?.Description;
            return !string.IsNullOrEmpty(paramDescription);
        }

        public override bool HasSuggestionsForParam(int argumentIndex) => argumentIndex <= MaxArity;

        public override async Task<ConnectorSuggestions> GetConnectorSuggestionsAsync(FormulaValue[] arguments, int argPosition, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (serviceProvider?.GetService(typeof(BaseRuntimeConnectorContext)) is not BaseRuntimeConnectorContext runtimeContext)
            {
                return null;
            }

            try
            {
                if (argPosition >= ConnectorFunction.RequiredParameters.Length)
                {
                    runtimeContext.ExecutionLogger?.LogInformation($"Exiting [Texl] {ConnectorFunction.LogFunction(nameof(GetConnectorSuggestionsAsync))} with null, with {LogArguments(arguments)} and position {argPosition} >= {ConnectorFunction.RequiredParameters.Length}");
                    return null;
                }

                NamedValue[] namedValues = arguments.Select((kp, i) => new NamedValue(ConnectorFunction.RequiredParameters[i].Name, kp)).ToArray();
                runtimeContext.ExecutionLogger?.LogInformation($"Entering [Texl] {ConnectorFunction.LogFunction(nameof(GetConnectorSuggestionsAsync))} with {LogKnownParameters(namedValues)} and position {argPosition}");

                ConnectorSuggestions suggestions = (await ConnectorFunction.GetConnectorSuggestionsInternalAsync(namedValues.ToArray(), ConnectorFunction.RequiredParameters[argPosition].ConnectorType, runtimeContext, cancellationToken).ConfigureAwait(false))?.ConnectorSuggestions;
                runtimeContext.ExecutionLogger?.LogInformation($"Exiting [Texl] {ConnectorFunction.LogFunction(nameof(GetConnectorSuggestionsAsync))} returning from {nameof(ConnectorFunction.GetConnectorSuggestionsInternalAsync)} with {LogConnectorSuggestions(suggestions)}");
                return suggestions;
            }
            catch (Exception ex)
            {
                runtimeContext.ExecutionLogger?.LogException(ex, $"Exception in [Texl] {ConnectorFunction.LogFunction(nameof(GetConnectorSuggestionsAsync))} with {LogArguments(arguments)} and position {argPosition} {LogException(ex)}");
                throw;
            }
        }
    }
}
