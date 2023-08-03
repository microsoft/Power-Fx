// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface IHasUnsupportedFunctions
    {
        bool IsDeprecated { get; }

        bool IsSupported { get; }

        string NotSupportedReason { get; }
    }
}
