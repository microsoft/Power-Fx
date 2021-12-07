// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    /// <summary>
    /// Entity info that respresents an enum, such as "Align" or "Font".
    /// </summary>
    internal sealed class EnumSymbol
    {
        private readonly Dictionary<string, string> _valuesInvariantToLoc;
        private readonly Dictionary<string, string> _valuesLocToInvariant;
        private readonly Dictionary<string, string> _valuesInvariantToDisplayName;

        public DType EnumType { get; }
        public IEnumerable<string> LocalizedEnumValues => _valuesLocToInvariant.Keys;

        /// <summary>
        /// The variant name for the enum
        /// </summary>
        public string Name { get; set; }
        
        public string InvariantName { get; set; }

        public EnumSymbol(EnumStore store, DName name, DName invariantName, DType invariantType)
        {
            Contracts.AssertValid(invariantName);
            Contracts.Assert(invariantType.IsEnum);

            Name = name;
            InvariantName = invariantName;
            EnumType = invariantType;

            // Initialize the locale-specific enum values, and the loc<->invariant maps.
            _valuesInvariantToLoc = new Dictionary<string, string>();
            _valuesLocToInvariant = new Dictionary<string, string>();
            _valuesInvariantToDisplayName = new Dictionary<string, string>();

            foreach (var typedName in EnumType.GetNames(DPath.Root))
            {
                string invName = typedName.Name.Value;

                string locName;
                if (!StringResources.TryGet($"{InvariantName}_{typedName.Name.Value}_Name", out locName))
                    locName = invName;

                Contracts.Assert(DName.IsValidDName(invName));
                _valuesInvariantToLoc[invName] = locName;
                _valuesLocToInvariant[locName] = invName;

                string displayName;
                if (!StringResources.TryGet($"{InvariantName}_{typedName.Name.Value}_DisplayName", out displayName))
                    displayName = locName;

                string custDisplayName;
                string entityNameValue = name.Value;
                if (!store.TryGetLocalizedEnumValue(entityNameValue, invName, out custDisplayName))
                    custDisplayName = displayName;

                _valuesInvariantToDisplayName[invName] = custDisplayName;
            }
        }

        /// <summary>
        /// Look up an enum value by its locale-specific name.
        /// For example, locName="Droit" --> invName="Right", value="right"
        /// </summary>
        public bool TryLookupValueByLocName(string locName, out string invName, out object value)
        {
            Contracts.AssertValue(locName);
            Contracts.Assert(DName.IsValidDName(locName));

            if (!_valuesLocToInvariant.TryGetValue(locName, out invName))
            {
                value = null;
                return false;
            }

            return EnumType.TryGetEnumValue(new DName(invName), out value);
        }
        
        /// <summary>
        /// Get the invariant enum value name corresponding to the given locale-specific name.
        /// For example, locName="Droit" --> invName="Right"For example, locName="Droit" --> invName="Right"
        /// </summary>
        public bool TryGetInvariantValueName(string locName, out string invName)
        {
            Contracts.AssertValue(locName);
            Contracts.Assert(DName.IsValidDName(locName));

            return _valuesLocToInvariant.TryGetValue(locName, out invName);
        }

        /// <summary>
        /// Get the locale-specific enum value name corresponding to the given invariant name.
        /// Note: This value is not localized currently, as we do not localized the language.
        /// </summary>
        public bool TryGetLocValueName(string invName, out string locName)
        {
            Contracts.AssertValue(invName);
            Contracts.Assert(DName.IsValidDName(invName));

            return _valuesInvariantToLoc.TryGetValue(invName, out locName);
        }

        /// <summary>
        /// Gets the locale-specific display value for the enum value corresponding to the given invariant name.
        /// </summary>
        public bool TryGetDisplayLocValueName(string invName, out string displayLocName)
        {
            Contracts.AssertValue(invName);
            Contracts.Assert(DName.IsValidDName(invName));

            return _valuesInvariantToDisplayName.TryGetValue(invName, out displayLocName);
        }
    }
}
