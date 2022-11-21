// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core
{
    // If there are multiple DisplayNameProviders associated with a type, we may have name conflicts
    // In that case, we block the use of display names using this provider
    [ThreadSafeImmutable]
    internal class DisabledDisplayNameProvider : DisplayNameProvider
    {
        internal static DisabledDisplayNameProvider Instance { get; } = new DisabledDisplayNameProvider();

        public override IEnumerable<KeyValuePair<DName, DName>> LogicalToDisplayPairs => ImmutableDictionary<DName, DName>.Empty;

        internal DisabledDisplayNameProvider()
        {
        }

        public override bool TryGetDisplayName(DName logicalName, out DName displayName)
        {
            displayName = default;
            return false;
        }

        public override bool TryGetLogicalName(DName displayName, out DName logicalName)
        {
            logicalName = default;
            return false;
        }

        internal override bool TryRemapLogicalAndDisplayNames(DName displayName, out DName logicalName, out DName newDisplayName)
        {
            logicalName = default;
            newDisplayName = default;
            return false;
        }
    }
}
