// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalEntity
    {
        /// <summary>
        /// This is the Host's symbolic name - not the logical name or entity display name. 
        /// Eg, "Accounts_2" , not "account". 
        /// This information may also be baked into the type. 
        /// </summary>
        DName EntityName { get; }

        DType Type { get; }
    }
}
