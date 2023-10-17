// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalCdsDataSource : IExternalTabularDataSource
    {
        string DatasetName { get; }

        IExternalDocument Document { get; }

        IExternalTableDefinition TableDefinition { get; }

        bool TryGetRelatedColumn(string selectColumnName, out string additionalColumnName, IExternalTableDefinition expandsTableDefinition = null);

        bool IsArgTypeValidForMutation(DType type, out IEnumerable<string> invalidFieldName);
    }
}
