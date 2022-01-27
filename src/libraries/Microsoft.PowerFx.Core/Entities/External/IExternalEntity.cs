// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalEntity
    {
        DName EntityName { get; }

        bool IsControl { get; }

        IExternalEntityScope EntityScope { get; }

        IEnumerable<IDocumentError> Errors { get; }
    }
}
