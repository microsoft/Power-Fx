// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    /// <summary>
    /// Describe an option set - maybe implemented by each back end over their existing enum-like symbols. 
    /// </summary>
    internal interface IExternalOptionSet : IExternalEntity
    {
        DisplayNameProvider DisplayNameProvider { get; }

        /// <summary>
        /// Logical names for the fields in this Option Set.
        /// </summary>
        IEnumerable<DName> OptionNames { get; }
                
        DKind BackingKind { get; }

        bool IsConvertingDisplayNameMapping { get; }

        bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue);
    }

    internal static class OptionSetExtensions
    {
        public static bool IsBooleanValued(this IExternalOptionSet optionSet)
        {
            return optionSet.BackingKind == DKind.Boolean;
        }
    }
}
