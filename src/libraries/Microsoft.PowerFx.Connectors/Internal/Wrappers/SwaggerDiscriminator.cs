// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class SwaggerDiscriminator : ISwaggerDiscriminator
    {
        private readonly OpenApiDiscriminator _discriminator;

        public static SwaggerDiscriminator New(OpenApiDiscriminator discriminator)
        {
            return discriminator == null ? null : new SwaggerDiscriminator(discriminator);
        }

        private SwaggerDiscriminator(OpenApiDiscriminator discriminator)
        {
            _discriminator = discriminator;
        }

        public string PropertyName => _discriminator?.PropertyName;
    }
}
