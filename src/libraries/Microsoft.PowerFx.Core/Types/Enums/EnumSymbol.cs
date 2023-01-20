// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    /// <summary>
    /// Entity info that respresents an enum, such as "Align" or "Font".
    /// </summary>
    internal sealed class EnumSymbol
    {
        public DType EnumType { get; }

        /// <summary>
        /// The name for the enum.
        /// </summary>
        public string Name { get; set; }

        public EnumSymbol(DName name, DType enumSpec)
        {
            Contracts.AssertValid(name);
            Contracts.Assert(enumSpec.IsEnum);

            Name = name;
            EnumType = enumSpec;
        }

        /// <summary>
        /// Look up an enum value by its unqualified name.
        /// For example, locName="Droit" --> invName="Right", value="right".
        /// </summary>
        public bool TryLookupValueByName(string unqualifiedName, out object value)
        {
            Contracts.AssertValue(unqualifiedName);
            Contracts.Assert(DName.IsValidDName(unqualifiedName));

            return EnumType.TryGetEnumValue(new DName(unqualifiedName), out value);
        }
    }
}
