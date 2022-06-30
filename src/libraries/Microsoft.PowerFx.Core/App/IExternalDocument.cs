// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Entities;

namespace Microsoft.PowerFx.Core.App
{
    internal interface IExternalDocument
    {
        IExternalDocumentProperties Properties { get; }

        IExternalEntityScope GlobalScope { get; }

        bool TryGetControlByUniqueId(string name, out IExternalControl control);
    }
}