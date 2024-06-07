// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorApiDiscriminator : IConnectorDiscriminator
    {
        private readonly OpenApiDiscriminator _discriminator;

        public static ConnectorApiDiscriminator New(OpenApiDiscriminator discriminator)
        {
            return discriminator == null ? null : new ConnectorApiDiscriminator(discriminator);
        }

        private ConnectorApiDiscriminator(OpenApiDiscriminator discriminator)
        {
            _discriminator = discriminator;
        }

        public string PropertyName => _discriminator?.PropertyName;
    }
}
