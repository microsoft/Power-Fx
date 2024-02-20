// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class StringBlob : BlobContent
    {
        private readonly string _string;
        private readonly Encoding _encoding;

        internal StringBlob(string str, Encoding encoding = null)
        {
            _string = str;
            _encoding = encoding ?? Encoding.UTF8;
        }

        internal override bool IsBase64 => false;

        internal override bool IsByteArray => false;

        internal override bool IsString => true;

        internal override byte[] GetAsByteArray()
        {
            return FromStringToBytes(_string, _encoding);
        }

        internal override Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();            
            return Task.FromResult(GetAsByteArray());
        }
    }
}
