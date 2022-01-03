// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core
{
    internal class DisplayNameProvider
    {
        // First is Logical Name, Second is Display Name
        private readonly BidirectionalDictionary<DName, DName> _displayNames;

        public DisplayNameProvider()
        {
            _displayNames = new();
        }

        private DisplayNameProvider(BidirectionalDictionary<DName, DName> displayNames)
        {
            _displayNames = displayNames.Clone();
        }

        public bool TryAddField(DName logicalName, DName displayName)
        {
            // Check for collisions between display and logical names
            if (_displayNames.ContainsSecondKey(logicalName) || _displayNames.ContainsFirstKey(logicalName) ||
                _displayNames.ContainsFirstKey(displayName) || _displayNames.ContainsSecondKey(displayName))
            {
                return false; 
            }
            return _displayNames.Add(logicalName, displayName);
        }


        public bool TryGetLogicalName(DName displayName, out DName logicalName)
        {
            return _displayNames.TryGetFromSecond(displayName, out logicalName);
        }
        
        public bool TryGetDisplayName(DName logicalName, out DName displayName)
        {
            return _displayNames.TryGetFromFirst(logicalName, out displayName);
        }

        public bool Matches(DisplayNameProvider other)
        {
            return other != null &&
                _displayNames.Count() == other._displayNames.Count() &&
                _displayNames.Keys.All(other._displayNames.ContainsFirstKey) &&
                _displayNames.Values.All(other._displayNames.ContainsSecondKey);
        }

        public DisplayNameProvider Clone()
        {
            return new DisplayNameProvider(_displayNames);
        }
    }
}
