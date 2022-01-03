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

        public DisplayNameProvider AddField(DName logicalName, DName displayName)
        {
            // Check for collisions between display and logical names
            if (_displayNames.ContainsSecondKey(logicalName) || _displayNames.ContainsFirstKey(logicalName) ||
                _displayNames.ContainsFirstKey(displayName) || _displayNames.ContainsSecondKey(displayName))
            {
                throw new NameCollisionException(displayName);
            }

            var result = Clone();
            result._displayNames.Add(logicalName, displayName);
            return result;
        }


        public bool TryGetLogicalName(DName displayName, out DName logicalName)
        {
            return _displayNames.TryGetFromSecond(displayName, out logicalName);
        }
        
        public bool TryGetDisplayName(DName logicalName, out DName displayName)
        {
            return _displayNames.TryGetFromFirst(logicalName, out displayName);
        }

        public DisplayNameProvider Clone()
        {
            return new DisplayNameProvider(_displayNames);
        }
    }
}
