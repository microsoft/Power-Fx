// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class SwaggerReference : ISwaggerReference
    {
        private readonly OpenApiReference _reference;

        public static ISwaggerReference New(OpenApiReference reference)
        {
            return reference == null ? null : new SwaggerReference(reference);
        }

        private SwaggerReference(OpenApiReference reference)
        {
            _reference = reference;
        }

        public string Id => _reference.Id;
    }
}
