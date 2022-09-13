// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Types;
using Microsoft.PowerFx.Types.Serializer;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SerializerTests
    {
        [Fact]
        public void Number()
        {
            var val = FormulaValue.New(123);

            var s = new FormulaValueSerializer();
            var payload = s.Serialize(val);

            var val2 = s.Deserialize(payload);

            Assert.Equal(val.Dump(), val2.Dump());
        }

        [Fact]
        public void Record()
        {
            var val = FormulaValue.NewRecordFromFields(
                new NamedValue("Field1", FormulaValue.New(12)),
                new NamedValue("Field2", FormulaValue.New(34)));

            var s = new FormulaValueSerializer();
            var payload = s.Serialize(val);

            var val2 = s.Deserialize(payload);

            Assert.Equal(val.Dump(), val2.Dump());
        }

        [Fact]
        public void CustomRecord()
        {
            var database = new Dictionary<string, int>
            {
                { "key1_Field1", 99 }
            };

            var val = new CustomRecordValue("key1", database);

            var s = new FormulaValueSerializer();
            s.Add<CustomRecordValue, CustomRecordValue.CustomSchema>(
                (recordValue) => recordValue.ToSchema(),
                (poco) => poco.ToValue(database));

            var payload = s.Serialize(val);

            // "Field1" from customObject never appears in serialized output. 
            Assert.DoesNotContain("Field1", payload);
            Assert.Contains("SchemaKey", payload); // instead, schema's key appears.

            var val2 = s.Deserialize(payload);

            Assert.Equal(val.Dump(), val2.Dump());
        }
    }

    // $$$ Serializer may need persistent state?
    internal class CustomRecordValue : RecordValue
    {
        private readonly string _key;
        private readonly Dictionary<string, int> _database;

        public static readonly RecordType _type = RecordType.Empty()
            .Add("Field1", FormulaType.Number);

        public CustomRecordValue(string key, Dictionary<string, int> database)
            : base(_type)
        {
            _key = key;
            _database = database;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            // Simulate database lookup
            if (_database.TryGetValue(_key + "_" + fieldName, out var x))
            {
                result = FormulaValue.New(x);
                return true;
            }

            result = null;
            return false;
        }

        // $$$ could this just be a virtual no registration needed? 
        // $$$ Does Poco type matter?
        public CustomSchema ToSchema()
        {
            return new CustomSchema
            {
                SchemaKey = _key
            };
        }

        public class CustomSchema
        {
            public string SchemaKey { get; set; }

            public CustomRecordValue ToValue(Dictionary<string, int> database)
            {
                return new CustomRecordValue(SchemaKey, database);
            }
        }
    }
}
