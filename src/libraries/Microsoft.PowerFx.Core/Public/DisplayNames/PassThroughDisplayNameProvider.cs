// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal class PassThroughDisplayNameProvider : SingleSourceDisplayNameProvider
    {
        internal readonly AggregateType BackingType;

        internal PassThroughDisplayNameProvider(AggregateType backingType)
        {
            BackingType = backingType;
        }
        
        internal override bool TryGetLogicalName(DName displayName, out DName logicalName)
        {
            if (BackingType.TryGetLogicalName(displayName.Value, out var logical))
            {
                logicalName = new DName(logical);
                return logicalName.IsValid;
            }

            logicalName = default;
            return false;
        }

        internal override bool TryGetDisplayName(DName logicalName, out DName displayName)
        {
            if (BackingType.TryGetDisplayName(logicalName.Value, out var display))
            {
                displayName = new DName(display);
                return displayName.IsValid;
            }

            displayName = default;
            return false;
        }
    }
}
