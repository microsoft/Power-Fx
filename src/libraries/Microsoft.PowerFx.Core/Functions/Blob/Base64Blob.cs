// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class Base64Blob : BlobContent
    {
        private readonly string _base64Str;

        internal Base64Blob(string base64Str)
        {
            _base64Str = base64Str;

            try
            {
                _ = Convert.FromBase64String(base64Str);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid base64 string", nameof(base64Str));
            }
        }

        internal override Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(FromBase64ToBytes(_base64Str));
        }

        internal override Task<string> GetAsBase64Async(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(_base64Str);
        }
    }
}
