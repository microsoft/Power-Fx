// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    /// <summary>
    /// Describes an option set - may be implemented by each back end over their existing enum-like symbols. 
    /// </summary>
    internal interface IExternalOptionSet : IExternalEntity
    {
        DisplayNameProvider DisplayNameProvider { get; }

        /// <summary>
        /// Logical names for the fields in this Option Set.
        /// </summary>
        IEnumerable<DName> OptionNames { get; }

        /// <summary>
        /// Backing kind for this option set. Must be Number, String, Boolean, or Color.
        /// </summary>
        DKind BackingKind { get; }

        // Option set flags: defines semantics for how the option set and backing kinds can be intermixed.
        //
        // Here are some of the most common usage patterns...
        //
        // Strongly typed, single option (default): All flags = false. The option set doesn't mix with the backing kind
        //          in any way. Useful for a strongly typed choice between options.
        //          Used by SortOder, TimeUnit, StartOfWeek, TraceSeverity, ...
        //
        // Strongly typed, multiple options at once: CanStronglyTypedConcatenation = true, string backed only.
        //          Used when multiple options are supported, members of the option set can be concatenated
        //          together to form new runtime members of the option set, thus retaining the strong typing.
        //          Concatenation with the backing kind is not allowed (see below for this case).
        //          Used by JSONFormat, MatchOptions, ...
        // 
        // Option set is shorthand for the backind kind: CanCoerceToBackingKind = true. The option set is a convenient
        //          shorthand for using the backind kind. Loosely typed, the only type safety is that a function defined
        //          with the option set can't be passed the backing type.
        //          Used by Color.
        //
        // Option set is shorthand for the backind kind, with Numerical Compare: CanCoreceToBackindKind and
        //          CanCompareNumerical = true, number backed only. Numerical order comparisons (<, >, <=, >=)
        //          are supported.
        //          Used by ErrorKind.
        //
        // Custom backing kind can be used in place of the option set:
        //          CanCoerceFromBackindKind = true, not supported for Color.
        //          Functions that have this option set as a parameter type can be passed the backing kind.
        //          Used by DateTimeFormat.
        //
        // Custom backing kind mixes with the option set:
        //          CanCoerceFromBackindKind and CanContenateStronglyTyped = true.
        //          Functions that have this option set as a parameter type can be passed the backing kind and
        //          concatenations between the option set and the backind kind result in the option set type.
        //          Used by Match.

        /// <summary>
        /// Backing kind typed values can be used in placed of this option set value.  
        /// This is also possible for concatenate and numeric compare if those flags are set.
        /// Examples: a completely custom regular expression string can passed for a Match enum
        /// and a number can be used in palce of an ErrorKind.
        /// </summary>
        bool CanCoerceFromBackingKind { get; }

        /// <summary>
        /// Option set can be used in place of the backing kind. 
        /// This is useful to provide a convenient enum value for the backing kind, for example ErrorKind and Color.
        /// There is no type safety, effectively the enum is just like using the backing type.
        /// </summary>
        bool CanCoerceToBackingKind { get; }

        /// <summary>
        /// Only applies to string backed option sets.
        /// All enums can be coerced to strings and concatenated as strings.  In some situations, we intend the maker
        /// to concatenate enums and not lose the strong typing of that enum, for example with JSONFormat or MatchOptions.
        /// A concatenate with like enum values results in the enum type being preserved if this flag is true.
        /// </summary>
        bool CanConcatenateStronglyTyped { get; }

        /// <summary>
        /// Only applies to number backed option sets.
        /// Members of this option set can be compared numerically with one another and with numbers.
        /// Examples: Members of ErrorKind, where ErrorKind.Custom is used to separate system ErrorKinds (those below) from
        /// custom ErrorKinds defined by makers (those above).
        /// </summary>
        bool CanCompareNumeric { get; }

        bool IsConvertingDisplayNameMapping { get; }

        bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue);
    }

    internal static class OptionSetExtensions
    {
        public static bool IsBooleanValued(this IExternalOptionSet optionSet)
        {
            return optionSet.BackingKind == DKind.Boolean;
        }

        public static bool IsStringValued(this IExternalOptionSet optionSet)
        {
            return optionSet.BackingKind == DKind.String;
        }

        public static bool IsNumberValued(this IExternalOptionSet optionSet)
        {
            return optionSet.BackingKind == DKind.Number;
        }

        public static bool IsColorValued(this IExternalOptionSet optionSet)
        {
            return optionSet.BackingKind == DKind.Color;
        }
    }
}
