#pragma warning disable CA1051
namespace PowerFXBenchmark.Builders
{
    using System;
    using System.Text.Json;
    using Microsoft.PowerFx.Types;
    using PowerFXBenchmark.Inputs.Models;
    using PowerFXBenchmark.UntypedObjects;

    public class RecordValueBuilder
    {
        protected IList<NamedValue> fields;


        public RecordValueBuilder()
        {
            fields = new List<NamedValue>();
        }

        public RecordValueBuilder WithTestObject(TestObject testObj)
        {
            fields.Add(new NamedValue("testObj", TestObjectUntypedObject.New(testObj)));
            return this;
        }

        public RecordValueBuilder WithEventJson(JsonElement json)
        {
            if (json.ValueKind != JsonValueKind.Null)
            {
                fields.Add(new NamedValue("event", FormulaValue.New(new JsonUntypedObject(json))));
            }

            return this;
        }

        public RecordValueBuilder WithEventJson(string json)
        {
            JsonElement result;
            using (var document = JsonDocument.Parse(json))
            {
                // Clone must be used here because the original element will be disposed
                result = document.RootElement.Clone();
            }

            if (result.ValueKind != JsonValueKind.Null)
            {
                fields.Add(new NamedValue("event", FormulaValue.New(new JsonUntypedObject(result))));
            }
            return this;
        }

        public RecordValue Build() => FormulaValue.NewRecordFromFields(fields);
    }
}
