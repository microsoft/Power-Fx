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

        internal override bool IsString => false;

        internal override bool IsBase64 => false;

        internal override bool IsByteArray => true;

        internal override byte[] GetAsByteArray()
        {
            byte[] copy = new byte[_data.Length];
            Array.Copy(_data, copy, _data.Length);
            return copy;
        }

        internal override Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(GetAsByteArray());
        }

        internal override string GetAsString(Encoding encoding)
        {
            return FromBytesToString(_data, encoding);
        }

        internal override Task<string> GetAsStringAsync(Encoding encoding, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(GetAsString(encoding));
        }

        internal override string GetAsBase64()
        {
            return FromBytesToBase64(_data);
        }

        internal override Task<string> GetAsBase64Async(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(GetAsBase64());
        }
    }
}
