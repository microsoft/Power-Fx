// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;

namespace Microsoft.PowerFx.Connectors
{
    internal static class WrapperExtensions
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

                JsonValueKind.Object => new JsonObjectExtension(je),
                JsonValueKind.Array => new JsonArrayExtension(je),

                JsonValueKind.Undefined => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }
    }
}
