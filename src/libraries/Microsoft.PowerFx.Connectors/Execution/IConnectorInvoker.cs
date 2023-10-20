// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.PowerFx.Connectors.Execution
{
    public class InvokerParameters
    {
        public QueryType QueryType { get; init; }

        public HttpMethod HttpMethod { get; init; }

        public string Url { get; init; }

        public IReadOnlyDictionary<string, string> Headers { get; init; }

        public string Body { get; init; }

        public string ContentType { get; init; }        
    }

    public enum QueryType
    {
        InitialRequest,
        NextPage
    }
}
