// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    public class ByteArrayBlob : BlobElementBase
    {
        private readonly byte[] _data;

        public int Length => _data.Length;

        public ByteArrayBlob(byte[] data)
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
