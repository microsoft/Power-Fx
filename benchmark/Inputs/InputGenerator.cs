using PowerFXBenchmark.Builders;
using PowerFXBenchmark.Inputs.Models;

namespace PowerFXBenchmark.Inputs
{
    public static class InputGenerator
    {
        public static string GenerateJson(string? fileName = null)
        {
            fileName ??= "json_small";
            return File.ReadAllText($"Inputs\\Json\\{fileName}.json");
        }

        public static TestObject GenerateTestObject()
        {
            var builder = new TestObjectBuilder();
            return builder
                .WithId("powerfx-test-1")
                .WithType("powerfx-test-1")
                .WithRootProperty("Temperature", 112)
                .WithRootProperty("Humidity", 123.5)
                .WithRootProperty("DisplayName", "i am display name")
                .Build();
        }
        public static TestObjectSchema GenerateTestObjectSchema()
        {
            var builder = new TestObjectSchemaBuilder();
            return builder
                .AddNestedPrimitiveSchema("Temperature", TestObjDataKind.Integer)
                .AddNestedPrimitiveSchema("ComfortIndex", TestObjDataKind.Integer)
                .AddNestedPrimitiveSchema("Humidity", TestObjDataKind.Double)
                .AddNestedPrimitiveSchema("DisplayName", TestObjDataKind.String)
                .Build();
        }
    }
}
