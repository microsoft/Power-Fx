// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal readonly struct CdpTableDescriptor
    {
        public FormulaType FormulaType { get; init; }

        public string Name { get; init; }

        public string DisplayName { get; init; }

        public ServiceCapabilities TableCapabilities { get; init; }

        public IReadOnlyDictionary<string, Relationship> Relationships { get; init; }
    }
}
