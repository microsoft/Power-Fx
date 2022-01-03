// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.PowerFx.Core.UtilityDataStructures;

namespace Microsoft.PowerFx.Core
{
    internal class DisplayNameProvider
    {
        // First is Logical Name, Second is Display Name
        private BidirectionalDictionary<string, string> _displayNames;

        public DisplayNameProvider()
        {
            _displayNames = new(StringComparer.Ordinal, StringComparer.Ordinal);
        }

        public bool TryAddField(string logicalName, string displayName)
        {
            // Check for collisions between display and logical names
            if (_displayNames.ContainsSecondKey(logicalName) || _displayNames.ContainsFirstKey(logicalName) ||
                _displayNames.ContainsFirstKey(displayName) || _displayNames.ContainsSecondKey(displayName))
            {
                return false; 
            }
            return _displayNames.Add(logicalName, displayName);
        }


        public bool TryGetLogicalName(string displayName, out string logicalName)
        {
            return _displayNames.TryGetFromSecond(displayName, out logicalName);
        }
        
        public bool TryGetDisplayName(string logicalName, out string displayName)
        {
            return _displayNames.TryGetFromFirst(logicalName, out displayName);
        }

        public bool Matches(DisplayNameProvider other)
        {
            return other != null &&
                _displayNames.Count() == other._displayNames.Count() &&
                _displayNames.Keys.All(other._displayNames.ContainsFirstKey) &&
                _displayNames.Values.Any(other._displayNames.ContainsSecondKey);
        }
    }
}
