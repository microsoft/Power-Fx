// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Connectors
{
    internal interface ITabularTableResolver
    {
        ConnectorLogger Logger { get; }

        Task<TabularTableDescriptor> ResolveTableAsync(string tableName, CancellationToken cancellationToken);
    }
}
