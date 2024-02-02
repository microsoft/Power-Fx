// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    /// <summary>
    /// Interface for resource manager.
    /// </summary>
    public interface IResourceManager
    {
        ResourceHandle AddElement(BaseResourceElement resourceElement);

        /// <summary>
        /// Get resource element for index.
        /// </summary>
        /// <param name="handle">Resource Handle.</param>
        /// <returns>Resource element.</returns>
        BaseResourceElement GetResource(ResourceHandle handle);

        bool RemoveResource(ResourceHandle handle);
    }
}
