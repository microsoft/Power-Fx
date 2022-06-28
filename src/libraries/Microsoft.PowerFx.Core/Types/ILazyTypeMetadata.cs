// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Types
{
    public interface ILazyTypeMetadata
    {
        bool IsFullExpansionAllowed { get; }
    }
}
