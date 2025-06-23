// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        // Exposed to PowerFx to allow it to skip operations from while Binding that
        // are not needed for non-canvas hosts, or canvas hosts running new analysis.
        bool IsRunningDataflowAnalysis();

        bool TryGetControlByUniqueId(string name, out IExternalControl control);
    }
}