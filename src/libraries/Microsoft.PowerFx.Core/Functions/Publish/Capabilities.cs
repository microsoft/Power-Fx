// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.Functions.Publish
{
    [TransportType(TransportKind.Enum)]
    [Flags]
    public enum Capabilities : uint
    {
        None = 0x0,

        // Provides access to incoming data from the internet.
        OutboundInternetAccess = 0x1,

        // Provides access to resources on private networks (home, work etc.)
        PrivateNetworkAccess = 0x2,

        // Provides access to connect to servers in an enterprise
        EnterpriseAuthentication = 0x4,

        // Provides access to local state for reading/writing files.
        LocalStateAccess = 0x8,

        // Provides access to files in user's pictures library.
        PicturesLibraryAccess = 0x10,

        // Provides access to files in user's video library.
        VideoLibraryAccess = 0x20,

        // Provides access to files in user's music library.
        MusicLibraryAccess = 0x40,

        // Provides access to the video feed of a built-in camera 
        // or external webcam, which allows the app to capture 
        // photos and videos.
        Camera = 0x80,

        // Provides access to the microphone’s audio feed, which allows 
        // the app to record audio from connected microphones.
        Microphone = 0x100,

        // Provides access to location functionality, which you get from dedicated 
        // hardware like a GPS sensor or is derived from available network info.
        Location = 0x200,
    }
}
