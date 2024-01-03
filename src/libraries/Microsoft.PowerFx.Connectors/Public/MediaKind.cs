// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// "x-ms-media-kind" enum.    
    /// </summary>
    public enum MediaKind : int
    {
        // "x-ms-media-kind" is not defined / dynamic intellisense, we use a string to store results while all others are byte[]
        NotBinary = -2,

        // "x-ms-media-kind" is not corresponding to any valid value (normally, only "audio", "video" or "image")
        Unknown = -1,

        // "x-ms-media-kind" is not defined, defaulting to "File"
        File = 0,

        // "x-ms-media-kind" is "audio"
        Audio,

        // "x-ms-media-kind" is "video"
        Video,

        // "x-ms-media-kind" is "image"
        Image
    }
}
