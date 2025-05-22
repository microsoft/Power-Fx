// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.PowerFx.Core.Functions.Delegation;

namespace Microsoft.PowerFx.Connectors
{
    internal static class Extensions
    {
        public static IOpenApiExtension ToIOpenApiExtension(this JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => new OpenApiString(je.GetString()),
                JsonValueKind.Number => new OpenApiDouble(je.GetDouble()),
                JsonValueKind.True => new OpenApiBoolean(true),
                JsonValueKind.False => new OpenApiBoolean(false),
                JsonValueKind.Null => new OpenApiNull(),

                JsonValueKind.Object => new SwaggerJsonObject(je),
                JsonValueKind.Array => new SwaggerJsonArray(je),

                JsonValueKind.Undefined => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }

        public static IEnumerable<string> ToStr(this IEnumerable<DelegationOperator> ops)
        {
            if (ops == null)
            {
                return null;
            }

            return ops.Select(op => op.ToString().ToLowerInvariant());
        }
    }
}
