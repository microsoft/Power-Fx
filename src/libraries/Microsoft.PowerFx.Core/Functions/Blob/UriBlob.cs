// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    public class UriBlob : BlobElementBase
    {        
        public Uri Uri { get; }

        public UriBlob(Uri uri)            
        {            
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public override async Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (Uri.Scheme == "https")
            {
                using HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = await httpClient.GetAsync(Uri, token).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }

            throw new InvalidOperationException("Invalid scheme for URI blob element");
        }
    }
}
