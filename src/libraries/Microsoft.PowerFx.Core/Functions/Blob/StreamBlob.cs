// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;

namespace Microsoft.PowerFx.Core.Functions
{
    public class StreamBlob : ByteArrayBlob
    {
        public StreamBlob(Stream stream)
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
