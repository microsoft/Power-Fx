// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Connector.Tests
{
    public class PublicSurfaceTests
    {
        [Fact]
        public void Test()
        {
            var asm = typeof(PowerPlatformConnectorClient).Assembly;

            // The goal for public namespaces is to make the SDK easy for the consumer. 
            // Namespace principles for public classes:            // 
            // - prefer fewer namespaces. See C# for example: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis
            // - For easy discovery, but Engine in "Microsoft.PowerFx".
            // - For sub areas with many related classes, cluster into a single subnamespace.
            // - Avoid nesting more than 1 level deep

            var allowed = new HashSet<string>()
            {
              "Microsoft.PowerFx.ConfigExtensions",
              "Microsoft.PowerFx.Connectors.PowerPlatformConnectorClient",
              "Microsoft.PowerFx.Connectors.ICachingHttpClient",
              "Microsoft.PowerFx.Connectors.NonCachingClient",
              "Microsoft.PowerFx.Connectors.CachingHttpClient",
              "Microsoft.PowerFx.Connectors.OpenApiExtensions"
            };

            var sb = new StringBuilder();
            var count = 0;
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic))
            {
                var name = type.FullName;
                if (!allowed.Contains(name))
                {
                    sb.AppendLine(name);
                    count++;
                }

                allowed.Remove(name);
            }

            Assert.True(count == 0, $"Unexpected public types: {sb}");

            // Types we expect to be in the assembly are all there. 
            Assert.Empty(allowed);
        }
    }
}
