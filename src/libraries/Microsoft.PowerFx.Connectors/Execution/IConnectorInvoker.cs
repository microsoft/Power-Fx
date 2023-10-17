// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    public interface IConnectorInvoker
    {
        // runtime context
        public BaseRuntimeConnectorContext Context { get; }

        // classic call
        public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken);

        // support for paging
        public Task<FormulaValue> InvokeAsync(string nextlink, CancellationToken cancellationToken);        
    }

#pragma warning disable CA1005 // Avoid excessive parameters on generic types
    public interface IConnectorSender<TInvoker, TRequest, TResponse>
    {
        Task<TResponse> SendAsync(TInvoker invoker, TRequest request, CancellationToken cancellationToken);
    }
#pragma warning restore CA1005 // Avoid excessive parameters on generic types
}
