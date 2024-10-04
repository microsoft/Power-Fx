// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpTableDescriptor
    {
        public TabularRecordType RecordType { get; init; }

        public string Name { get; init; }

        public string DisplayName { get; init; }

        public TableParameters TableParameters { get; init; }

        public IReadOnlyDictionary<string, Relationship> Relationships { get; init; }
    }
}
