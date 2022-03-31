// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx
{
    internal static class Utility
    {
        // Could also get this from System.Linq.Async nuget. 
        // https://stackoverflow.com/questions/59380470/convert-iasyncenumerable-to-list
        public static async Task<List<T>> ToListAsync<T>(
            this IAsyncEnumerable<T> items,
            CancellationToken cancellationToken = default)
        {
            var results = new List<T>();
            await foreach (var item in items.WithCancellation(cancellationToken)
                                            .ConfigureAwait(false))
            {
                results.Add(item);
            }

            return results;
        }
    }
}
