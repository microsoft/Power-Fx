// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet"/> class.
        /// </summary>
        /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="options">The members of the option set. Enumerable of pairs of logical name to display name.
        /// NameCollisionException is thrown if display and logical names for options are not unique.
        /// </param>
        public OptionSet(string name, ImmutableDictionary<DName, DName> options)
            : this(name, new SingleSourceDisplayNameProvider(options))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet"/> class.
        /// </summary>
        /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="displayNameProvider">The DisplayNameProvider for the members of the OptionSet.
        /// Consider using <see cref="DisplayNameUtility.MakeUnique(IEnumerable{KeyValuePair{string, string}})"/> to generate
        /// the DisplayNameProvider.
        /// </param>
        public OptionSet(string name, DisplayNameProvider displayNameProvider)
        {
            EntityName = new DName(name);
            Options = displayNameProvider.LogicalToDisplayPairs;

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

        DKind IExternalOptionSet.BackingKind => DKind.String;

        bool IExternalOptionSet.CanCoerceFromBackingKind => false;

        bool IExternalOptionSet.CanCoerceToBackingKind => false;

        bool IExternalOptionSet.CanCompareNumeric => false;

        bool IExternalOptionSet.CanConcatenateStronglyTyped => false;

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

    internal class NumberOptionSet : IExternalOptionSet
    {
        private readonly DisplayNameProvider _displayNameProvider;
        private readonly DType _type;

        private readonly bool _canCoerceFromBackingKind;
        private readonly bool _canCoerceToBackingKind;
        private readonly bool _canCompareNumeric;

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberOptionSet"/> class.
        /// </summary>
        /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="options">The members of the option set. Enumerable of pairs of logical name to display name.
        /// <param name="canCoerceFromBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="canCoerceToBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="canCompareNumeric">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// NameCollisionException is thrown if display and logical names for options are not unique.
        /// </param>
        public NumberOptionSet(string name, ImmutableDictionary<int, DName> options, bool canCoerceFromBackingKind = false, bool canCoerceToBackingKind = false, bool canCompareNumeric = false)
            : this(name, IntDisplayNameProvider(options), canCoerceFromBackingKind, canCoerceToBackingKind, canCompareNumeric)
        {
        }

        private static DisplayNameProvider IntDisplayNameProvider(ImmutableDictionary<int, DName> optionSetValues)
        { 
            return DisplayNameUtility.MakeUnique(optionSetValues.Select(kvp => new KeyValuePair<string, string>(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberOptionSet"/> class.
        /// </summary>
        /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="displayNameProvider">The DisplayNameProvider for the members of the OptionSet.
        /// <param name="canCoerceFromBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="canCoerceToBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="canCompareNumeric">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// Consider using <see cref="DisplayNameUtility.MakeUnique(IEnumerable{KeyValuePair{string, string}})"/> to generate
        /// the DisplayNameProvider.
        /// </param>
        public NumberOptionSet(string name, DisplayNameProvider displayNameProvider, bool canCoerceFromBackingKind = false, bool canCoerceToBackingKind = false, bool canCompareNumeric = faslse)
        {
            EntityName = new DName(name);
            Options = displayNameProvider.LogicalToDisplayPairs;

            _canCoerceFromBackingKind = canCoerceFromBackingKind;
            _canCoerceToBackingKind = canCoerceToBackingKind;
            _canCompareNumeric = canCompareNumeric;

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

            var osft = new OptionSetValueType(_type.OptionSetInfo);
            optionSetValue = new OptionSetValue(fieldName.Value, osft, double.Parse(fieldName.Value, CultureInfo.InvariantCulture));
            return true;
        }

        IEnumerable<DName> IExternalOptionSet.OptionNames => Options.Select(option => option.Key);

        DisplayNameProvider IExternalOptionSet.DisplayNameProvider => _displayNameProvider;

        bool IExternalOptionSet.IsConvertingDisplayNameMapping => false;

        DType IExternalEntity.Type => _type;

        DKind IExternalOptionSet.BackingKind => DKind.Number;

        bool IExternalOptionSet.CanCoerceFromBackingKind => _canCoerceFromBackingKind;

        bool IExternalOptionSet.CanCoerceToBackingKind => _canCoerceToBackingKind;

        bool IExternalOptionSet.CanCompareNumeric => _canCompareNumeric;

        bool IExternalOptionSet.CanConcatenateStronglyTyped => false;

        public override bool Equals(object obj)
        {
            return obj is NumberOptionSet other &&
                EntityName == other.EntityName &&
                this._type == other._type;
        }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(EntityName.GetHashCode(), this._type.GetHashCode());
        }
    }
}
