// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    internal abstract class BlobContent
    {
        internal abstract Task<byte[]> GetAsByteArrayAsync(CancellationToken token);

        internal virtual async Task<string> GetAsStringAsync(Encoding encoding, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return FromBytesToString(await GetAsByteArrayAsync(token).ConfigureAwait(false), encoding);
        }

        internal virtual async Task<string> GetAsBase64Async(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return FromBytesToBase64(await GetAsByteArrayAsync(token).ConfigureAwait(false));
        }

        protected static string FromBytesToString(byte[] bytes, Encoding encoding)
        {
            return bytes == null ? null : (encoding ?? Encoding.UTF8).GetString(bytes);
        }

        protected static byte[] FromStringToBytes(string str, Encoding encoding)
        {
            return string.IsNullOrEmpty(str) ? Array.Empty<byte>() : (encoding ?? Encoding.UTF8).GetBytes(str);
        }

        protected static string FromBytesToBase64(byte[] bytes)
        {
            return bytes == null ? null : Convert.ToBase64String(bytes);
        }

        protected static byte[] FromBase64ToBytes(string base64Str)
        {
            return string.IsNullOrEmpty(base64Str) ? Array.Empty<byte>() : Convert.FromBase64String(base64Str);
        }
    }
}
