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
    public abstract class DisplayNameProvider
    {
        public abstract bool TryGetLogicalName(DName displayName, out DName logicalName);

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
        /// In KeyValue Pair, First is Logical Name, Second is Display Name
        /// KeyValuePair&lt;Dname, Dname&gt;&gt; represents  KeyValuePair&lt;LogicalName, DisplayName&gt;&gt;.
        /// </summary>
        public abstract IEnumerable<KeyValuePair<DName, DName>> LogicalToDisplayPairs { get; }

        /// <summary>
        /// Lookup when it is either logical or display name. 
        /// </summary>
        /// <param name="logicalOrDisplay"></param>
        /// <param name="logicalName"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
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
    }
}
