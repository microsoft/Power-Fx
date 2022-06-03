// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    internal static class Helpers
    {
        public static Stream GetStream(string name)
        {
            var assemblyNamespace = "Microsoft.PowerFx.Connectors.Tests";
            var fullName = assemblyNamespace + "." + name.Replace('\\', '.');

            var assembly = typeof(BasicRestTests).Assembly;
            var stream = assembly.GetManifestResourceStream(fullName);

            Assert.NotNull(stream);
            return stream;
        }

        public static string ReadAllText(string name)
        {
            using (var stream = GetStream(name))
            using (var textReader = new StreamReader(stream))
            {
                return textReader.ReadToEnd();
            }
        }

        // Get a swagger file from the embedded resources. 
        public static OpenApiDocument ReadSwagger(string name)
        {
            using (var stream = GetStream(name))
            {
                var doc = new OpenApiStreamReader().Read(stream, out var diag);
                return doc;
            }
        }
    }
}
