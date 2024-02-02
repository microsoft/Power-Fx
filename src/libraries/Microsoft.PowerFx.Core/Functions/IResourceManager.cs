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
        /// <summary>
        /// Create a new resource element for string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <param name="fileType">Associated FileType.</param>
        /// <returns>Index of the new element.</returns>
        int NewElementFromString(string str, FileType fileType = FileType.Any);

        /// <summary>
        /// Create a new resource element for a base64 encoded string.
        /// </summary>
        /// <param name="base64str">Base64 encoded string.</param>
        /// <param name="fileType">Associated FileType.</param>
        /// <returns>Index of the new element.</returns>
        int NewElementFromBase64String(string base64str, FileType fileType = FileType.Any);

        /// <summary>
        /// Get resource element for index.
        /// </summary>
        /// <param name="i">Index.</param>
        /// <returns>Resource element.</returns>
        IResourceElement GetResource(int i);

        /// <summary>
        /// Get Uri for resource index.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>Uri.</returns>
        Uri GetUri(int index);
    }

    /// <summary>
    /// Interface for resource element.
    /// </summary>
    public interface IResourceElement
    {
        /// <summary>
        /// File type.
        /// </summary>
        FileType FileType { get; }

        /// <summary>
        /// String.
        /// </summary>
        string String { get; }

        /// <summary>
        /// Base64 encoded string.
        /// Note: this is the base64 encoded version of <c>String</c>.
        /// </summary>
        string Base64String { get; }        
    }    
}
