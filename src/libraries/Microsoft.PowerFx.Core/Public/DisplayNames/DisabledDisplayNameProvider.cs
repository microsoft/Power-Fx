using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core
{
    // If there are multiple DisplayNameProviders associated with a type, we may have name conflicts
    // In that case, we block the use of display names using this provider
    internal class DisabledDisplayNameProvider : DisplayNameProvider
    {
        public static DisabledDisplayNameProvider Instance { get; } = new DisabledDisplayNameProvider();

        private DisabledDisplayNameProvider()
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
    }
}
