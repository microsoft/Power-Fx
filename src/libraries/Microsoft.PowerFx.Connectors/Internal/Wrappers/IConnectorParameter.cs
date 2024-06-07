// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    internal interface IConnectorParameter : IConnectorExtensions
    {
        public IConnectorSchema Schema { get; }

        public string Name { get; }

        public bool Required { get; }
    }
}
