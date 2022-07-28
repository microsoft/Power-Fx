using PowerFXBenchmark.Builders;
using PowerFXBenchmark.Inputs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerFXBenchmark.Inputs
{
    public static class InputGenerator
    {
        public static string GenerateJson(string? fileName = null)
        {
            fileName ??= "telemetry_6KB";
            return File.ReadAllText($"Inputs\\Json\\{fileName}.json");
        }

        public static TestObject GenerateTestObject()
        {
            var builder = new TestObjectBuilder();
            return builder
                .WithId("powerfx-test-1")
                .WithType("powerfx-test-1")
                .WithRootProperty("Temperature", 1)
                .WithRootProperty("Humidity", 123)
                .WithRootProperty("DisplayName", "i am display name")
                .Build();
        }
    }
}
