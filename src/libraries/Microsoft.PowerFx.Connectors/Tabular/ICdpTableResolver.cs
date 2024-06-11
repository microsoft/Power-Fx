// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Connectors
{
    internal interface ICdpTableResolver
    {
        ConnectorLogger Logger { get; }

        Task<CdpTableDescriptor> ResolveTableAsync(string tableName, CancellationToken cancellationToken);
    }
}
