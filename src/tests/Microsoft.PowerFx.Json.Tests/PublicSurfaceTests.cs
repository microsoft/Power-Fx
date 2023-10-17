// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Json.Tests
{
    public class PublicSurfaceTests
    {
        [Fact]
        public void PublicSurfaceTest_Json()
        {
            var asm = typeof(Types.FormulaValueJSON).Assembly;

            // The goal for public namespaces is to make the SDK easy for the consumer. 
            // Namespace principles for public classes:            // 
            // - prefer fewer namespaces. See C# for example: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis
            // - For easy discovery, but Engine in "Microsoft.PowerFx".
            // - For sub areas with many related classes, cluster into a single subnamespace.
            // - Avoid nesting more than 1 level deep

            var allowed = new HashSet<string>()
            {
                "Microsoft.PowerFx.ConfigExtensions",
                "Microsoft.PowerFx.Core.FormulaTypeJsonConverter",
                "Microsoft.PowerFx.Core.FormulaTypeSerializerSettings",
                "Microsoft.PowerFx.JsonConfigExtensions",
                "Microsoft.PowerFx.Types.FormulaValueJSON",
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
