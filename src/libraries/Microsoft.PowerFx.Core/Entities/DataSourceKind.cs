// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.Entities
{
    [TransportType(TransportKind.Enum)]
    internal enum DataSourceKind
    {
        Static = 0,
        Dynamic = 1,

        // Note: 3 was 'Service'. Which has been removed from the product.
        Collection = 2,
        Resource = 4,
        AppReference = 5,
        Connected = 6,
        CdsNative = 7,
        WebResource = 8
    }
}