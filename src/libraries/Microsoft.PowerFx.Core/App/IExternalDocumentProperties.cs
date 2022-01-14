// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.App
{
    internal interface IExternalDocumentProperties
    {
        IExternalEnabledFeatures EnabledFeatures { get; }

        bool SupportsImplicitThisItem { get; }

        Dictionary<string, int> DisallowedFunctions { get; }
    }
}