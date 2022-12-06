// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface ITexlFunction
    {
        DPath Namespace { get; }

        string Name { get; }

        string LocaleInvariantName { get; }

        TexlFunction ToTexlFunctions();
    }
}
