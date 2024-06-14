﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    internal readonly struct CdpTableDescriptor
    {
        public ConnectorType ConnectorType { get; init; }

        public string Name { get; init; }

        public string DisplayName { get; init; }

        public ServiceCapabilities TableCapabilities { get; init; }
    }
}
