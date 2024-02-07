// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class StringBlob : BlobContent
    {
        private readonly string _string;

        internal StringBlob(string str)
        {
            _string = str;
        }

        internal override Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // defaults to UTF8
            return Task.FromResult(FromStringToBytes(_string, null));
        }
    }
}
