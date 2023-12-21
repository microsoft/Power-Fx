// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    internal static class Helpers
    {
        public static Stream GetStream(string name)
        {
            if (!Path.IsPathRooted(name))
            {
                string assemblyNamespace = "Microsoft.PowerFx.Connectors.Tests";
                string fullName = assemblyNamespace + "." + name.Replace('\\', '.');

                var assembly = typeof(BasicRestTests).Assembly;
                var stream = assembly.GetManifestResourceStream(fullName);

                Assert.NotNull(stream);
                return stream;
            }

            return File.OpenRead(name);
        }

        // returns byte[] or string depending on name extension
        public static object ReadStream(string name)
        {
            using (var stream = GetStream(name))
            {
                // return byte[] for images
                if (name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
                else
                {
                    using var streamReader = new StreamReader(stream);                    
                    return streamReader.ReadToEnd();                    
                }
            }
        }

        // Get a swagger file from the embedded resources. 
        public static OpenApiDocument ReadSwagger(string name)
        {
            using (var stream = GetStream(name))
            {
                var doc = new OpenApiStreamReader().Read(stream, out OpenApiDiagnostic diag);

                if ((doc == null || doc.Paths == null || doc.Paths.Count == 0) && diag != null && diag.Errors.Count > 0)
                { 
                    throw new InvalidDataException($"Unable to parse Swagger file: {string.Join(", ", diag.Errors.Select(err => err.Message))}");
                }

                return doc;
            }
        }
    }
}
