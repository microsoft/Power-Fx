// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    public interface IConnectorInvoker
    {
        // runtime context
        BaseRuntimeConnectorContext Context { get; }

        // request: classic call
        Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken);

        // resquest: support for paging
        Task<FormulaValue> InvokeAsync(string nextlink, CancellationToken cancellationToken);        
        
        // processing/inner call
        Task<FormulaValue> SendAsync(InvokerParameters invokerElements, CancellationToken cancellationToken);
    }

    public class InvokerParameters
    {
        public QueryType QueryType { get; init; }

        public HttpMethod HttpMethod { get; init; }

        public string Url { get; init; }

        public Dictionary<string, string> Headers { get; init; }

        public string Body { get; init; }

        public string ContentType { get; init; }        
    }

    public enum QueryType
    {
        InitialRequest,
        NextPage
    }
}
