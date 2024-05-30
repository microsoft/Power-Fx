// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

[assembly: AssemblyVersionAttribute("1.99.0")]

namespace Microsoft.PowerFx.Shims.Net462
{
    public static class Shims
    {
        public static IEnumerable<T> Select<T>(this MatchCollection matchCollection, Func<Match, T> func)
        {
            List<T> list = new List<T>();

            foreach (Match m in matchCollection)
            {
                list.Add(func(m));
            }

            return list;
        }

        public static async Task<string> ReadAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await content.ReadAsStringAsync();
        }

        public static Task CopyToAsync(this HttpContent content, Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return content.CopyToAsync(stream);
        }
    } 
}
