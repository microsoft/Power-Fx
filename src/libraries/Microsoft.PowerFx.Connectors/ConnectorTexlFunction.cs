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

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorTexlFunction : TexlFunction, IAsyncTexlFunction3, IHasUnsupportedFunctions
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
            paramDescription = ConnectorFunction.RequiredParameters.FirstOrDefault(p => p.Name == paramName).Description ?? ConnectorFunction.OptionalParameters.FirstOrDefault(p => p.Name == paramName).Description;
            return !string.IsNullOrEmpty(paramDescription);
        }

        public override bool HasSuggestionsForParam(int argumentIndex) => argumentIndex <= MaxArity;

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await ConnectorFunction.InvokeAsync(args, serviceProvider, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<ConnectorSuggestions> GetConnectorSuggestionsAsync(FormulaValue[] knownParameters, int argPosition, IServiceProvider services, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await ConnectorFunction.GetConnectorSuggestionsAsync(knownParameters, argPosition, services, cancellationToken).ConfigureAwait(false);
        }
    }
}
