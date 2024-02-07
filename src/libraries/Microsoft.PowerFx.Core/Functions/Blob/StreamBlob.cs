// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class StreamBlob : ByteArrayBlob
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamBlob"/> class.
        /// Copies the stream to a byte array and creates a new StreamBlob.
        /// </summary>
        /// <param name="stream"></param>
        internal StreamBlob(Stream stream)
            : base(FromStreamToBytes(stream))
        {
        }

        private static byte[] FromStreamToBytes(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
