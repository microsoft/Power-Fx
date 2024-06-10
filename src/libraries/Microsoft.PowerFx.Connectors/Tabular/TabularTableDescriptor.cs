// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    internal struct TabularTableDescriptor
    {
        public ConnectorType ConnectorType { get; init; }

        public string Name { get; init; }

        public string DisplayName { get; init; }

        public ServiceCapabilities TableCapabilities { get; init; }
    }
}
