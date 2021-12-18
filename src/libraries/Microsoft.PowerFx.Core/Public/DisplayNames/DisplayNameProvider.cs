// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.UtilityDataStructures;

namespace Microsoft.PowerFx.Core
{
    internal class DisplayNameProvider
    {
        // String comparison is case-sensitive by default, and that's the behavior we want
        private BidirectionalDictionary<string, string> _displayNames;

        public DisplayNameProvider()
        {
            _displayNames = new();
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

        public override bool Equals(object obj)
        {
            return obj is DisplayNameProvider other && _displayNames.Equals(other._displayNames);
        }

        public override int GetHashCode()
        {
            return _displayNames.GetHashCode();
        }
    }
}
