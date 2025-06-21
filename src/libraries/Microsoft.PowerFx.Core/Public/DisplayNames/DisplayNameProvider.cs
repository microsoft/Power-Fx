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
    [ThreadSafeImmutable]
    /// <summary>
    /// Provides an abstract base for mapping between logical and display names.
    /// </summary>
    public abstract class DisplayNameProvider
    {
        /// <summary>
        /// Attempts to get the logical name corresponding to the specified display name.
        /// </summary>
        /// <param name="displayName">The display name to look up.</param>
        /// <param name="logicalName">The logical name that corresponds to the display name, if found.</param>
        /// <returns>True if the logical name was found; otherwise, false.</returns>
        public abstract bool TryGetLogicalName(DName displayName, out DName logicalName);

        /// <summary>
        /// Attempts to get the display name corresponding to the specified logical name.
        /// </summary>
        /// <param name="logicalName">The logical name to look up.</param>
        /// <param name="displayName">The display name that corresponds to the logical name, if found.</param>
        /// <returns>True if the display name was found; otherwise, false.</returns>
        public abstract bool TryGetDisplayName(DName logicalName, out DName displayName);

        /// <summary>
        /// This function attempts to remap logical and display names given a display name.
        /// It's used for scenarios where display names are changed under the hood while the expression is in display name format already.
        /// This is a legacy Canvas app behavior, and should not be supported implemented by non-canvas hosts.
        /// If this isn't supported by a given display name provider, this should return the same as 
        /// <see cref="TryGetLogicalName(DName, out DName)"/>, with the newDisplayName output populated by the first arg. 
        /// </summary>
        internal virtual bool TryRemapLogicalAndDisplayNames(DName displayName, out DName logicalName, out DName newDisplayName)
        {
            newDisplayName = displayName;
            return TryGetLogicalName(displayName, out logicalName);
        }

        /// <summary>
        /// Gets the collection of logical to display name pairs.
        /// In KeyValue Pair, First is Logical Name, Second is Display Name.
        /// KeyValuePair&lt;DName, DName&gt; represents KeyValuePair&lt;LogicalName, DisplayName&gt;.
        /// </summary>
        public abstract IEnumerable<KeyValuePair<DName, DName>> LogicalToDisplayPairs { get; }

        /// <summary>
        /// Looks up a name that may be either a logical or display name and returns both forms if found.
        /// </summary>
        /// <param name="logicalOrDisplay">The name to look up, which may be either a logical or display name.</param>
        /// <param name="logicalName">The logical name if found; otherwise, the default value.</param>
        /// <param name="displayName">The display name if found; otherwise, the default value.</param>
        /// <returns>True if the name was found as either a logical or display name; otherwise, false.</returns>
        public bool TryGetLogicalOrDisplayName(DName logicalOrDisplay, out DName logicalName, out DName displayName)
        {
            if (this.TryGetDisplayName(logicalOrDisplay, out displayName))
            {
                logicalName = logicalOrDisplay;
                return true;
            }
            else if (this.TryGetLogicalName(logicalOrDisplay, out logicalName))
            {
                displayName = logicalOrDisplay;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new display name provider with the given logical to display name pairs.
        /// </summary>
        /// <param name="logicalToDisplayPairs">The collection of logical to display name pairs to use for the provider.</param>
        /// <returns>A new instance of <see cref="DisplayNameProvider"/> initialized with the specified pairs.</returns>
        public static DisplayNameProvider New(IEnumerable<KeyValuePair<DName, DName>> logicalToDisplayPairs)
        {
            if (logicalToDisplayPairs == null) 
            { 
                throw new ArgumentNullException(nameof(logicalToDisplayPairs)); 
            }

            return new SingleSourceDisplayNameProvider(logicalToDisplayPairs);
        }
    }
}
