// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    public class OptionSet : IExternalOptionSet
    {
        private readonly DisplayNameProvider _displayNameProvider;
        private readonly DType _type;
        private readonly DKind _dkind;

        // At this time, only these data types are supported.
        // Don't be tempted to add Decimal until we have a specific need,
        // not that it should be a problem to add, but it would add more comlication that is needed.
        // Dataverse option sets easily fit within the integer range of Number (floating point).
        public enum OptionSetBackingType
        {
            String = 1,
            Number,
            Boolean,
            Color,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet"/> class.
        /// </summary>
        /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="options">The members of the option set. Enumerable of pairs of logical name to display name.
        /// <param name="backingType">The option set uses a specific backing type.</param>
        /// <param name="canCoerceFromBackingKind">Can the backing kind be used to replace this option set.</param>
        /// <param name="canConcatenateStronglyTyped">Can members of this option set be concatenated together, and with text if canCoerceBackingKind is true. Only applies to String.</param>
        /// <param name="canCompareNumeric">Can members of this option set be compared numerically? Only applies to Number.</param>
        /// NameCollisionException is thrown if display and logical names for options are not unique.
        /// </param>
        public OptionSet(
            string name, 
            ImmutableDictionary<DName, DName> options,
            OptionSetBackingType backingType = OptionSetBackingType.String,
            bool canCoerceFromBackingKind = false,
            bool canConcatenateStronglyTyped = false,
            bool canCompareNumeric = false)
            : this(name, new SingleSourceDisplayNameProvider(options), canCoerceFromBackingKind: canCoerceFromBackingKind, canConcatenateStronglyTyped: canConcatenateStronglyTyped, canCompareNumeric: canCompareNumeric)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet"/> class.
        /// </summary>
        /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="displayNameProvider">The DisplayNameProvider for the members of the OptionSet.</param>
        /// <param name="backingType">The option set uses a specific backing type.</param>
        /// <param name="canCoerceFromBackingKind">Can the backing kind be used to replace this option set.</param>
        /// <param name="canConcatenateStronglyTyped">Can members of this option set be concatenated together, and with text if canCoerceBackingKind is true. Only applies to String.</param>
        /// <param name="canCompareNumeric">Can members of this option set be compared numerically? Only applies to Number.</param>
        /// <param name="canCoerceToBackingKind">Can member of this option set coerce to the backing kind.</param>
        /// Consider using <see cref="DisplayNameUtility.MakeUnique(IEnumerable{KeyValuePair{string, string}})"/> to generate
        /// the DisplayNameProvider.
        public OptionSet(
            string name, 
            DisplayNameProvider displayNameProvider, 
            OptionSetBackingType backingType = OptionSetBackingType.String, 
            bool canCoerceFromBackingKind = false, 
            bool canConcatenateStronglyTyped = false,
            bool canCompareNumeric = false,
            bool canCoerceToBackingKind = false)
        {
            EntityName = new DName(name);
            Options = displayNameProvider.LogicalToDisplayPairs;

            Contracts.Assert(backingType == OptionSetBackingType.Number || !canCompareNumeric);
            Contracts.Assert(backingType == OptionSetBackingType.String || !canConcatenateStronglyTyped);

            switch (backingType)
            {
                case OptionSetBackingType.String:
                    _dkind = DKind.String;
                    break;
                case OptionSetBackingType.Number:
                    _dkind = DKind.Number;
                    break;
                case OptionSetBackingType.Boolean:
                    _dkind = DKind.Boolean;
                    break;
                case OptionSetBackingType.Color:
                    _dkind = DKind.Color;
                    break;
                default:
                    throw new InvalidEnumException(nameof(backingType));
            }

            CanCoerceFromBackingKind = canCoerceFromBackingKind;
            CanConcatenateStronglyTyped = canConcatenateStronglyTyped;
            CanCompareNumeric = canCompareNumeric;
            CanCoerceToBackingKind = canCoerceToBackingKind;

            _displayNameProvider = displayNameProvider;
            FormulaType = new OptionSetValueType(this);
            _type = DType.CreateOptionSetType(this);
        }

        /// <summary>
        /// Name of the option set, referenceable from expressions.
        /// </summary>
        public DName EntityName { get; }

        /// <summary>
        /// Contains the members of the option set.
        /// Key is logical/invariant name, value is display name.
        /// </summary>
        public IEnumerable<KeyValuePair<DName, DName>> Options { get; }

        /// <summary>
        /// Formula Type corresponding to this option set.
        /// Use in record/table contexts to define the type of fields using this option set.
        /// </summary>
        public OptionSetValueType FormulaType { get; }

        public bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
        {
            if (!Options.Any(option => option.Key == fieldName))
            {
                optionSetValue = null;
                return false;
            }

            optionSetValue = new OptionSetValue(fieldName, FormulaType);
            return true;
        }

        IEnumerable<DName> IExternalOptionSet.OptionNames => Options.Select(option => option.Key);

        DisplayNameProvider IExternalOptionSet.DisplayNameProvider => _displayNameProvider;
        
        bool IExternalOptionSet.IsConvertingDisplayNameMapping => false;

        DType IExternalEntity.Type => _type;

        DKind IExternalOptionSet.BackingKind => _dkind;

        public bool CanCoerceFromBackingKind { get; }

        public bool CanConcatenateStronglyTyped { get; }

        public bool CanCompareNumeric { get; }

        public bool CanCoerceToBackingKind { get; }

        public override bool Equals(object obj)
        {
            return obj is OptionSet other &&
                EntityName == other.EntityName &&
                this._type == other._type;
        }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(EntityName.GetHashCode(), this._type.GetHashCode());
        }
    }
}
