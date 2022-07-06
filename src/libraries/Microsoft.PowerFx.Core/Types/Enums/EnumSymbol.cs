// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.ContractsUtils;
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
        /// The variant name for the enum.
        /// </summary>
        public string Name { get; set; }

        public string InvariantName { get; set; }

        public EnumSymbol(IReadOnlyDictionary<string, Dictionary<string, string>> customEnumLocDict, DName name, DName invariantName, DType invariantType)
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
                var invName = typedName.Name.Value;

                if (!StringResources.TryGet($"{InvariantName}_{typedName.Name.Value}_Name", out var locName))
                {
                    locName = invName;
                }

                Contracts.Assert(DName.IsValidDName(invName));
                _valuesInvariantToLoc[invName] = locName;
                _valuesLocToInvariant[locName] = invName;

                if (!StringResources.TryGet($"{InvariantName}_{typedName.Name.Value}_DisplayName", out var displayName))
                {
                    displayName = locName;
                }

                var entityNameValue = name.Value;
                if (!TryGetLocalizedEnumValue(customEnumLocDict, entityNameValue, invName, out var custDisplayName))
                {
                    custDisplayName = displayName;
                }

                _valuesInvariantToDisplayName[invName] = custDisplayName;
            }
        }

        private bool TryGetLocalizedEnumValue(IReadOnlyDictionary<string, Dictionary<string, string>> customEnumLocDict, string enumName, string enumValue, out string locValue)
        {
            Contracts.AssertValue(enumName);
            Contracts.AssertValue(enumValue);

            locValue = enumValue;
            if (customEnumLocDict.ContainsKey(enumName))
            {
                var thisEnum = customEnumLocDict[enumName];
                if (thisEnum.ContainsKey(enumValue))
                {
                    locValue = thisEnum[enumValue];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Look up an enum value by its locale-specific name.
        /// For example, locName="Droit" --> invName="Right", value="right".
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
        /// For example, locName="Droit" --> invName="Right"For example, locName="Droit" --> invName="Right".
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
