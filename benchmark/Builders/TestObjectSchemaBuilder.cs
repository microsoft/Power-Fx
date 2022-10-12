namespace PowerFXBenchmark.Builders
{
    using System;
    using Newtonsoft.Json.Linq;
    using PowerFXBenchmark.Inputs.Models;

    public class TestObjectSchemaBuilder
    {
        private readonly Dictionary<string, ITestObjectBaseSchema> nestedSchemas;

        public TestObjectSchemaBuilder() : this(new Dictionary<string, ITestObjectBaseSchema>())
        {
        }

        public TestObjectSchemaBuilder(Dictionary<string, ITestObjectBaseSchema> nestedSchemas)
        {
            this.nestedSchemas = nestedSchemas;
        }

        public TestObjectSchemaBuilder AddNestedPrimitiveSchema(string name, TestObjDataKind kind)
        {
            nestedSchemas.Add(name, new PrimitiveSchema(kind));

            return this;
        }

        public TestObjectSchemaBuilder AddNestedArraySchema(string name, ITestObjectBaseSchema arrayElement)
        {
            nestedSchemas.Add(name,
                new ArraySchema
                {
                    ArrayElementSchema = arrayElement,
                });

            return this;
        }
        public TestObjectSchemaBuilder AddNestedMapSchema(string name, NamedSchema mapElementSchema)
        {
            nestedSchemas.Add(name,
                new MapSchema
                {
                    MapElementSchema = mapElementSchema,
                });

            return this;
        }

        public TestObjectSchema Build() => new() { NestedSchemas = nestedSchemas };
    }
}
