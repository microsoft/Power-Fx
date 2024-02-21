// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class ByteArrayBlob : BlobContent
    {
        private readonly byte[] _data;

        internal int Length => _data.Length;

        internal ByteArrayBlob(byte[] data)
        {
            _data = data;
        }

        public override Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            byte[] copy = new byte[_data.Length];
            Array.Copy(_data, copy, _data.Length);
            return Task.FromResult(copy);
        }

        public override Task<string> GetAsStringAsync(Encoding encoding, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(FromBytesToString(_data, encoding));
        }

        public override Task<string> GetAsBase64Async(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(FromBytesToBase64(_data));
        }
    }
}
