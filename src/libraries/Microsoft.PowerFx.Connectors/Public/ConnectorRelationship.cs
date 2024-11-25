// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Connectors
{
    [Obsolete("We shouldn't be exposing relationships and will remove this class in next iterations.")]
    public class ConnectorRelationship
    {
        public string ForeignTable { get; internal init; }

        public string ForeignKey { get; internal init; }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(ForeignTable?.GetHashCode() ?? 0, ForeignKey?.GetHashCode() ?? 0);
        }

        public override string ToString() => $"{ForeignTable ?? "<null>"}:{ForeignKey ?? "<null>"}";
    }
}
