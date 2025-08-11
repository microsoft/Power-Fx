// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.OpenApi.Interfaces;

namespace Microsoft.PowerFx.Connectors
{
    internal class SwaggerExtensions : ISwaggerExtensions
    {
        protected IDictionary<string, IOpenApiExtension> _extensions;

        protected SwaggerExtensions()
        {
        }

        public static ISwaggerExtensions New(IOpenApiExtensible schema)
        {
            return schema == null ? null : new SwaggerExtensions(schema);
        }

        protected SwaggerExtensions(IOpenApiExtensible schema)
        {
            _extensions = schema?.Extensions;
        }

        public SwaggerExtensions(IDictionary<string, IOpenApiExtension> extensions)
        {
            _extensions = extensions;
        }

        public IDictionary<string, IOpenApiExtension> Extensions => _extensions ?? new Dictionary<string, IOpenApiExtension>();
    }   
}
