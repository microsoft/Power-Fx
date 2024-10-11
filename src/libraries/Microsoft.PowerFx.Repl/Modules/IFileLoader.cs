// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.PowerFx.Repl
{
    /// <summary>
    /// A load can resolve a name to Module contents. 
    /// </summary>
    internal interface IFileLoader
    {
        /// <summary>
        /// Load a module poco from the given fulename. 
        /// </summary>
        /// <param name="filename">a filename. Could be full path or relative. The loader will resolve.</param>
        /// <returns>The loaded module contents and a new file loader for resolving any subsequent imports in this module.</returns>
        Task<(ModulePoco, IFileLoader)> LoadAsync(string filename);
    }
}
