// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal interface IColumnsCapabilities
    {
        void AddColumnCapability(string name, ColumnCapabilitiesBase capability);
    }
}
