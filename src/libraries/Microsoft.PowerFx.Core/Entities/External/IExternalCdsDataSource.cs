// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Entities.Delegation;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalCdsDataSource : IExternalTabularDataSource
    {
        string DatasetName { get; }

        IExternalDocument Document { get; }

        IExternalTableDefinition TableDefinition { get; }

        bool TryGetRelatedColumn(string selectColumnName, out string additionalColumnName, IExternalTableDefinition expandsTableDefinition = null);

        bool CanRemoveNavigationColumn(string selectColumnName);
    }
}
