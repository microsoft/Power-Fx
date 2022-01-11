// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core
{
    internal class SingleSourceDisplayNameProvider : DisplayNameProvider
    {
        // First is Logical Name, Second is Display Name
        private readonly ImmutableDictionary<DName, DName> _logicalToDisplay;
        private readonly ImmutableDictionary<DName, DName> _displayToLogical;

        public SingleSourceDisplayNameProvider()
        {
            _logicalToDisplay = ImmutableDictionary.Create<DName, DName>();
            _displayToLogical = ImmutableDictionary.Create<DName, DName>();
        }

        private SingleSourceDisplayNameProvider(ImmutableDictionary<DName, DName> logicalToDisplay, ImmutableDictionary<DName, DName> displayToLogical)
        {
            _logicalToDisplay = logicalToDisplay;
            _displayToLogical = displayToLogical;
        }

        public SingleSourceDisplayNameProvider AddField(DName logicalName, DName displayName)
        {
            // Check for collisions between display and logical names
            if (_displayToLogical.ContainsKey(logicalName) || _logicalToDisplay.ContainsKey(logicalName) ||
                _logicalToDisplay.ContainsKey(displayName) || _displayToLogical.ContainsKey(displayName))
            {
                throw new NameCollisionException(displayName);
            }

            var newDisplayToLogical = _displayToLogical.Add(displayName, logicalName);
            var newLogicalToDisplay = _logicalToDisplay.Add(logicalName, displayName);

            return new SingleSourceDisplayNameProvider(newLogicalToDisplay, newDisplayToLogical);
        }

        public override bool TryGetLogicalName(DName displayName, out DName logicalName)
        {
            return _displayToLogical.TryGetValue(displayName, out logicalName);
        }

        public override bool TryGetDisplayName(DName logicalName, out DName displayName)
        {
            return _logicalToDisplay.TryGetValue(logicalName, out displayName);
        }
    }
}
