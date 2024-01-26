// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface IHasUnsupportedFunctions
    {
        bool IsDeprecated { get; }

        bool IsInternal { get; }

        bool IsNotSupported { get; }

        IReadOnlyCollection<ErrorResourceKey> Warnings { get; }

        // For internal use only. Not customer friendly, no need for localization.
        string NotSupportedReason { get; }
    }
}
