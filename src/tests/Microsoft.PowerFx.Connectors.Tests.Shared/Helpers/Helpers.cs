// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;
using Microsoft.PowerFx.Connectors;
using Xunit;
using Xunit.Abstractions;

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

                Assert.True(stream != null, $"Cannot find resource {name} in assembly {assemblyNamespace}");
                return stream;
            }

            return File.OpenRead(name);
        }

        // returns byte[] or string depending on name extension
        public static object ReadStream(string name)
        {
            string[] byteArrayExtensions = new string[] { ".jpeg", ".jpg", ".png", ".pdf" };

            using (var stream = GetStream(name))
            {
                // return byte[] for images
                if (byteArrayExtensions.Any(ext => name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
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
        public static OpenApiDocument ReadSwagger(string name, ITestOutputHelper output)
        {
            using var stream = GetStream(name);            
            OpenApiReaderSettings oars = new OpenApiReaderSettings() { RuleSet = ConnectorFunction.DefaultValidationRuleSet };
            OpenApiDocument doc = new OpenApiStreamReader(oars).Read(stream, out OpenApiDiagnostic diag);

            if (diag != null && diag.Errors.Count > 0)
            {
                foreach (OpenApiError error in diag.Errors)
                {
                    if (error is OpenApiValidatorError vError)
                    {
                        output.WriteLine($"[OpenApi Error] {vError.RuleName} {vError.Pointer} {vError.Message}");
                    }
                    else
                    {
                        // Could be OpenApiError or OpenApiReferenceError
                        output.WriteLine($"[OpenApi Error] {error.Pointer} {error.Message}");
                    }
                }
            }

            return doc;
        }
    }
}
