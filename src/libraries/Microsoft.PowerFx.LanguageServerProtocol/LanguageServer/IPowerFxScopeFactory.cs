// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Factory interface for creating or retrieving <see cref="IPowerFxScope"/> instances for a given document URI.
    /// </summary>
    public interface IPowerFxScopeFactory
    {
        /// <summary>
        /// Gets or creates an <see cref="IPowerFxScope"/> instance associated with the specified document URI.
        /// </summary>
        /// <param name="documentUri">The URI of the document for which to get or create the scope instance.</param>
        /// <returns>An <see cref="IPowerFxScope"/> instance for the specified document URI.</returns>
        IPowerFxScope GetOrCreateInstance(string documentUri);
    }
}
