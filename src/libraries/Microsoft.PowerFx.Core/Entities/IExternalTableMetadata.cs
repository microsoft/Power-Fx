// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalTableMetadata
    {
        bool TryGetColumn(string nameRhs, out ColumnMetadata columnMetadata);

        string DisplayName { get; }

        string Name { get; }
    }
}