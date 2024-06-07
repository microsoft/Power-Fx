// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorApiReference : IConnectorReference
    {
        private readonly OpenApiReference _reference;

        public static IConnectorReference New(OpenApiReference reference)
        {
            return reference == null ? null : new ConnectorApiReference(reference);
        }

        private ConnectorApiReference(OpenApiReference reference)
        {
            _reference = reference;
        }

        public string Id => _reference.Id;
    }
}
