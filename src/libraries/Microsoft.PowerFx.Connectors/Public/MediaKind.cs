// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// "x-ms-media-kind" enum.    
    /// </summary>
    public enum MediaKind : int
    {
        /// <summary>
        /// Not a binary media kind (dynamic intellisense, string results).
        /// </summary>
        NotBinary = -2,

        /// <summary>
        /// Unknown media kind.
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// File media kind (default).
        /// </summary>
        File = 0,

        /// <summary>
        /// Audio media kind.
        /// </summary>
        Audio,

        /// <summary>
        /// Video media kind.
        /// </summary>
        Video,

        /// <summary>
        /// Image media kind.
        /// </summary>
        Image
    }
}
