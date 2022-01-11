// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalEntity
    {
        DName EntityName { get; }

        string InvariantName { get; }

        bool IsControl { get; }

        IExternalEntityScope EntityScope { get; }

        IEnumerable<IDocumentError> Errors { get; }

        bool TryGetRule(DName propertyName, out IExternalRule rule);
    }
}