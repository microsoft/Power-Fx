// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    /// <summary>
    /// Entity info that respresents an enum, such as "Align" or "Font".
    /// </summary>
    internal sealed class EnumSymbol : IExternalOptionSet
    {
        public DName EntityName { get; }

        public DType EnumType { get; }
                
        public DType OptionSetType { get; }

        /// <summary>
        /// Formula Type corresponding to this Enum.
        /// Use in function declarations, etc to refer to this type.
        /// </summary>
        public OptionSetValueType FormulaType { get; }

        public DKind BackingKind => EnumType.EnumSuperkind;

        public DisplayNameProvider DisplayNameProvider => DisabledDisplayNameProvider.Instance;

        public IEnumerable<DName> OptionNames => EnumType.GetAllNames(DPath.Root).Select(typedName => typedName.Name);

        public EnumSymbol(DName name, DType enumSpec)
        {
            Contracts.AssertValid(name);
            Contracts.Assert(enumSpec.IsEnum);

            EntityName = name;
            EnumType = enumSpec;
            
            FormulaType = new OptionSetValueType(this);
            OptionSetType = DType.CreateOptionSetType(this);
        }

        public EnumSymbol(DName name, DType backingType, IEnumerable<KeyValuePair<string, object>> members)
        {
            Contracts.AssertValid(name);

            EntityName = name;
            EnumType = DType.CreateEnum(
                backingType,
                members.Select(kvp => new KeyValuePair<DName, object>(new DName(kvp.Key), kvp.Value)));

            FormulaType = new OptionSetValueType(this);
            OptionSetType = DType.CreateOptionSetType(this);
        }

        /// <summary>
        /// Look up an enum value by its unqualified name.
        /// For example, unqualifiedName="Right" -> value="right".
        /// </summary>
        public bool TryLookupValueByName(string unqualifiedName, out object value)
        {
            Contracts.AssertValue(unqualifiedName);
            Contracts.Assert(DName.IsValidDName(unqualifiedName));

            return EnumType.TryGetEnumValue(new DName(unqualifiedName), out value);
        }

        public bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
        {
            if (!TryLookupValueByName(fieldName, out var value))
            {
                optionSetValue = null;
                return false;
            }

            optionSetValue = new OptionSetValue(fieldName, FormulaType, value);
            return true;
        }

        public bool IsConvertingDisplayNameMapping => false;

        /// <summary>
        /// Don't access the type of an EnumSymbol. They should always be accesed via either
        /// <see cref="EnumType"/> or <see cref="OptionSetType"/> depending on the value of 
        /// <see cref="Features.StronglyTypedBuiltinEnums"/>.
        /// </summary>
        public DType Type => throw new System.NotSupportedException("Don't access the type of an EnumSymbol directly, it depends on the value of a feature flag");
    }
}
