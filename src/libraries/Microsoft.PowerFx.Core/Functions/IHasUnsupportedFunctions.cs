// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface IHasUnsupportedFunctions
    {
        bool IsDeprecated { get; }

        bool IsInternal { get; }

        bool IsNotSupported { get; }

        IReadOnlyCollection<ExpressionError> Warnings { get; }

        // For internal use only. Not customer friendly, no need for localization.
        string NotSupportedReason { get; }
    }
}
