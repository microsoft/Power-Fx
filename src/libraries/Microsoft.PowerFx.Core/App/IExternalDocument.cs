// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;

namespace Microsoft.PowerFx.Core.App
{
    internal interface IExternalDocument
    {
        IExternalDocumentProperties Properties { get; }

        IExternalEntityScope GlobalScope { get; }

        bool TryGetControlByUniqueId(string name, out IExternalControl control);
    }
}