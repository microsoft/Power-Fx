// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpTableDescriptor
    {
        public FormulaType FormulaType { get; init; }

        public string Name { get; init; }

        public string DisplayName { get; init; }

        public ServiceCapabilities2 TableCapabilities2 { get; init; }

        public IReadOnlyDictionary<string, Relationship> Relationships { get; init; }
    }
}
