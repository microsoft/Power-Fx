// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.UtilityDataStructures;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IDisplayMapped<T>
    {
        bool IsConvertingDisplayNameMapping { get; }

        /// <summary>
        /// Maps logical names to display names.
        /// </summary>
        public BidirectionalDictionary<T, string> DisplayNameMapping { get; }

        /// <summary>
        /// Display Mapped objects occasionally change their display names, in which case we need
        /// both the new and old display names to correctly rewrite them in rules.
        /// </summary>
        public BidirectionalDictionary<T, string> PreviousDisplayNameMapping { get; }
    }
}
