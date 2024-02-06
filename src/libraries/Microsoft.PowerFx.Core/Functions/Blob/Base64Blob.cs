// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    public class Base64Blob : BlobElementBase
    {
        private readonly string _base64Str;

        public Base64Blob(string base64Str)
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

        public override Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(FromBase64ToBytes(_base64Str));
        }
    }
}
