// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal sealed class DataTableMetadata
    {
        public string DisplayName { get; }

        public string Name { get; }

        public DataTableMetadata(string name, string displayName)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertNonEmpty(displayName);

            Name = name;
            DisplayName = displayName;
        }
    }
}
