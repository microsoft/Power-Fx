// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.OpenApi.Interfaces;

namespace Microsoft.PowerFx.Connectors
{
    internal class SwaggerJsonExtensions : ISwaggerExtensions
    {
        private readonly JsonElement _schema;

        public SwaggerJsonExtensions(JsonElement je)
        {
            _schema = je;
        }

        public IDictionary<string, IOpenApiExtension> Extensions
        {
            get
            {
                Dictionary<string, IOpenApiExtension> exts = new Dictionary<string, IOpenApiExtension>();

                if (_schema.ValueKind != JsonValueKind.Object)
                {
                    return exts;
                }

                foreach (JsonProperty jp in _schema.EnumerateObject())
                {
                    exts.Add(jp.Name, jp.Value.ToIOpenApiExtension());
                }

                return exts;
            }
        }
    }
}
