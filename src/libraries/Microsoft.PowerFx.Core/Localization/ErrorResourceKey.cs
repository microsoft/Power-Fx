// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Localization
{
    /// <summary>
    /// Key Type of string resources related to errors. 
    /// </summary>
    /// <remarks>
    /// Used by BaseError in DocError.cs to ensure that it is passed a key as opposed to a generic string, such as the contents of the error message. 
    /// Existing keys for error messages are split between here (for general document errors) and Strings.cs (for Texl errors).
    /// </remarks>
    [ThreadSafeImmutable]
    public struct ErrorResourceKey
    {
        /// <summary>
        /// Gets the key for the error resource.
        /// </summary>
        public string Key { get; }

        internal IExternalStringResources ResourceManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResourceKey"/> struct with the specified key.
        /// </summary>
        /// <param name="key">The key for the error resource.</param>
        public ErrorResourceKey(string key)
        {
            Contracts.AssertNonEmpty(key);

            Key = key;
            ResourceManager = StringResources.LocalStringResources;
        }

        internal ErrorResourceKey(string key, IExternalStringResources externalStringResources = null)
        {
            Contracts.AssertNonEmpty(key);

            Key = key;
            ResourceManager = externalStringResources;
        }
    }
}
