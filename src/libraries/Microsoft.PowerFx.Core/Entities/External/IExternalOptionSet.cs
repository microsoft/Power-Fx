// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalOptionSet : IExternalEntity
    {
        DisplayNameProvider DisplayNameProvider { get; }

        /// <summary>
        /// Logical names for the fields in this Option Set.
        /// </summary>
        IEnumerable<DName> OptionNames { get; }
                
        bool IsBooleanValued { get; }

        bool IsConvertingDisplayNameMapping { get; } 

        DType Type { get; }
    }
}
