// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    public class StringBlob : BlobElementBase
    {
        private readonly string _string;

        public StringBlob(string str)
        {
            _string = str;
        }

        public override Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // defaults to UTF8
            return Task.FromResult(FromStringToBytes(_string, null));
        }
    }
}
