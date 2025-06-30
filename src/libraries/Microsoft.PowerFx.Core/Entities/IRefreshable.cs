// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    /// <summary>
    /// Represents an entity that can be refreshed, such as a Table or Record value.
    /// </summary>
    public interface IRefreshable
    {
        /// <summary>
        /// Refreshes the entity to update its data or state.
        /// </summary>
        void Refresh();
    }
}
