// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal sealed class PagingCapabilities
    {
        public readonly bool IsOnlyServerPagable;

        public readonly string[] ServerPagingOptions;

        public PagingCapabilities(bool isOnlyServerPagable, string[] serverPagingOptions)
        {
            IsOnlyServerPagable = isOnlyServerPagable;
            ServerPagingOptions = serverPagingOptions;
        }
    }
}
