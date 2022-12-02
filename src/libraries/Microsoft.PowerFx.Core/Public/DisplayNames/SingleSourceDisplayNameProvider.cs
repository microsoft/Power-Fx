// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        public override IEnumerable<KeyValuePair<DName, DName>> LogicalToDisplayPairs => _logicalToDisplay;

        internal SingleSourceDisplayNameProvider()
        {
            _logicalToDisplay = ImmutableDictionary.Create<DName, DName>();
            _displayToLogical = ImmutableDictionary.Create<DName, DName>();
        }

        public SingleSourceDisplayNameProvider(IEnumerable<KeyValuePair<DName, DName>> logicalToDisplayPairs)
        {
            var lToDBuilder = ImmutableDictionary.CreateBuilder<DName, DName>();
            var dToLBuilder = ImmutableDictionary.CreateBuilder<DName, DName>();

            // Validate input while constructing the dictionaries
            foreach (var kvp in logicalToDisplayPairs)
            {
                if (dToLBuilder.ContainsKey(kvp.Value) || lToDBuilder.ContainsKey(kvp.Value) ||
                    lToDBuilder.ContainsKey(kvp.Key) || dToLBuilder.ContainsKey(kvp.Key))
                {
                    throw new NameCollisionException(kvp.Key);
                }

                lToDBuilder.Add(kvp.Key, kvp.Value);
                dToLBuilder.Add(kvp.Value, kvp.Key);
            }

            _logicalToDisplay = lToDBuilder.ToImmutable();
            _displayToLogical = dToLBuilder.ToImmutable();
        }

        internal SingleSourceDisplayNameProvider(ImmutableDictionary<DName, DName> logicalToDisplay, ImmutableDictionary<DName, DName> displayToLogical)
        {
            _logicalToDisplay = logicalToDisplay;
            _displayToLogical = displayToLogical;
        }

        internal SingleSourceDisplayNameProvider AddField(DName logicalName, DName displayName)
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

        public DisplayNameProvider RemoveField(DName lookupName)
        {
            if (_logicalToDisplay.TryGetValue(lookupName, out var displayName))
            {
                var logicalToDisplay = _logicalToDisplay.Remove(lookupName);
                var displayToLogic = _displayToLogical.Remove(displayName);
                return new SingleSourceDisplayNameProvider(logicalToDisplay, displayToLogic);

            }
            else if (_displayToLogical.TryGetValue(lookupName, out var logicalName))
            {
                var displayToLogic = _displayToLogical.Remove(lookupName);
                var logicalToDisplay = _logicalToDisplay.Remove(logicalName);
                return new SingleSourceDisplayNameProvider(logicalToDisplay, displayToLogic);
            }

            return this;
        }
    }
}
