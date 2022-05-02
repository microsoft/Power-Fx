// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Utilities for working with Display Names in Power Fx.
    /// </summary>
    public static class DisplayNameUtility
    {
        /// <summary>
        /// PowerFx Display Names are required to be unique with respect to other display names in the same type,
        /// as well as the logical names of that type. This helper ensures that display names are unique, by rewriting to 
        /// `Display (logical)` for all colliding names. If uniqueness cannot be found, we fall back on using
        /// logical names as display names for all fields.
        /// </summary>
        /// <param name="logicalToDisplayPairs">Enumerable of (logical, display) pairs.</param>
        /// <returns>Enumerable of unique (logical, display) pairs.</returns>
        public static IEnumerable<KeyValuePair<DName, DName>> MakeUnique(IEnumerable<KeyValuePair<string, string>> logicalToDisplayPairs)
        {
            var displayNameMapping = new BidirectionalDictionary<DName, DName>();
            var usedNames = new HashSet<DName>();

            foreach (var pair in logicalToDisplayPairs)
            {
                // Logical names must always be valid names
                if (!DName.IsValidDName(pair.Key))
                {
                    throw new ArgumentException($"Invalid logical name {pair.Key}");
                }

                var logicalName = new DName(pair.Key);

                // Display names must be valid names, if not, logical name should map to itself.
                if (!DName.IsValidDName(pair.Value))
                {
                    usedNames.Add(logicalName);
                    displayNameMapping.Add(logicalName, logicalName).Verify();
                    continue;
                }

                var displayName = new DName(pair.Value);

                usedNames.Add(logicalName);

                // Logical -> already existing display name conflict, construct new name for earlier one
                if (displayNameMapping.ContainsSecondKey(logicalName))
                {
                    displayNameMapping.TryGetFromSecond(logicalName, out var previousLogical).Verify();

                    // Replace earlier mapping with "Display Name (logicalname)"
                    displayNameMapping.TryRemoveFromSecond(logicalName);
                    var updatedDisplayName = ConstructFallbackName(logicalName, previousLogical);

                    usedNames.Add(updatedDisplayName);
                    displayNameMapping.Add(previousLogical, updatedDisplayName).Verify();
                }

                // Display -> logical | primary conflict, fall back to  "Display Name (logicalname)"
                if (displayNameMapping.ContainsFirstKey(displayName))
                {
                    displayName = ConstructFallbackName(displayName, logicalName);
                    usedNames.Add(displayName);
                    displayNameMapping.Add(logicalName, displayName).Verify();
                    continue;
                }

                // Try the provided mapping if usedNames doesn't contain the display name or if the logical name is the same as the display name.
                // We need to handle the logical name equal to display name here since we have already added logical name to the usedNames list
                if ((!usedNames.Contains(displayName) || logicalName.Equals(displayName)) && displayNameMapping.Add(logicalName, displayName))
                {
                    usedNames.Add(displayName);
                    continue;
                }

                var disambiguatedDisplayName = ConstructFallbackName(displayName, logicalName);

                // Fallback mapping if there were duplicate display names
                if (displayNameMapping.Add(logicalName, disambiguatedDisplayName))
                {
                    usedNames.Add(disambiguatedDisplayName);

                    // Clean up existing un-disambiguated display name
                    if (displayNameMapping.TryGetFromSecond(displayName, out var previousLogical))
                    {
                        displayNameMapping.TryRemoveFromSecond(displayName);

                        // replace earlier mapping with "Display Name (logicalname)"
                        var updatedDisplayName = ConstructFallbackName(displayName, previousLogical);
                        usedNames.Add(updatedDisplayName);
                        displayNameMapping.Add(previousLogical, updatedDisplayName).Verify();
                    }

                    continue;
                }

                // Only remaining case is multiple of the same logical name were passed, throw
                throw new ArgumentException($"Duplicate logical names {logicalName}");
            }

            return displayNameMapping;
        }

        private static DName ConstructFallbackName(DName display, DName logical)
        {
            return new DName(display.Value + " " + TexlLexer.PunctuatorParenOpen + logical.Value + TexlLexer.PunctuatorParenClose);
        }
    }
}
