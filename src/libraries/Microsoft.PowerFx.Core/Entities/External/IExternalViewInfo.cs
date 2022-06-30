// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalViewInfo : IExternalEntity
    {
        DisplayNameProvider DisplayNameProvider { get; }

        /// <summary>
        /// Logical names for the members in this View.
        /// </summary>
        IEnumerable<DName> ViewNames { get; }

        string Name { get; }

        string RelatedEntityName { get; }

        bool IsConvertingDisplayNameMapping { get; }
    }
}
