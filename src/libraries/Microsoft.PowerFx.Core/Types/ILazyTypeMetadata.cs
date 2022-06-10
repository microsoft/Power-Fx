// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Types
{
    internal interface ILazyTypeMetadata
    {
        /// <summary>
        /// True if the lazily expanded type should be a table, false for a record.
        /// </summary>
        bool IsTable { get; }
    }
}
