// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    public class InvokerParameters
    {
        public QueryType QueryType { get; init; }

        public HttpMethod HttpMethod { get; init; }

        public string ContentType { get; init; }

        public Uri Address { get; init; }              

        // in Swagger order
        public IReadOnlyList<InvokerParameter> HeaderParameters { get; set; }

        public IReadOnlyList<InvokerParameter> PathParameters { get; set; }

        public IReadOnlyList<InvokerParameter> QueryParameters { get; set; }

        public IReadOnlyList<InvokerParameter> BodyParameters { get; set; }
    }

    public class InvokerParameter
    {
        public string Name { get; init; }

        public OpenApiSchema Schema { get; init; }

        public FormulaValue Value { get; init; }

        public bool DoubleEncoded { get; init; }
    }

    public enum QueryType
    {
        InitialRequest,
        NextPage
    }
}
