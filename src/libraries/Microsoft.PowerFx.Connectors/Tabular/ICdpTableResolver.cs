// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Connectors
{
    internal interface ICdpTableResolver
    {
        ConnectorLogger Logger { get; }

        [Obsolete("This property is a temporary hack to generate ADS")]
        bool GenerateADS { get; init; }

        Task<CdpTableDescriptor> ResolveTableAsync(string tableName, CancellationToken cancellationToken);
    }
}
