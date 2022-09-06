// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.App
{
    internal interface IExternalDocumentProperties
    {
        IExternalEnabledFeatures EnabledFeatures { get; }
    }
}
