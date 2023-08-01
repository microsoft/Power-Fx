// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface ISupportsDeprecatedFunctions
    {
        bool IsDeprecated { get; }
    }
}
