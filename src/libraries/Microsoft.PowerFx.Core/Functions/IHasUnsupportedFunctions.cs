// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface IHasUnsupportedFunctions
    {
        bool IsDeprecated { get; }

        bool IsNotSupported { get; }

        // For internal use only. Not customer friendly, no need for localization.
        string NotSupportedReason { get; }
    }
}
