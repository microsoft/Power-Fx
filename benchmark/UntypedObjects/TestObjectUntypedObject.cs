using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using PowerFXBenchmark.Inputs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerFXBenchmark.UntypedObjects
{
    public class TestObjectUntypedObject : IUntypedObject
    {
        public TestObject testObj;

        public TestObjectUntypedObject(TestObject testObj)
        {
            this.testObj = testObj;
        }

        public IUntypedObject this[int index] => throw new NotImplementedException();

        public FormulaType Type => ExternalType.ObjectType;

        public static UntypedObjectValue New(TestObject testObj)
        {
            var x = new TestObjectUntypedObject(testObj);

            return FormulaValue.New(x);
        }

        public int GetArrayLength()
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            throw new NotImplementedException();
        }

        public string GetString()
        {
            throw new NotImplementedException();
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
        {
            result = null;
            switch (value)
            {
                case "$id":
                    result = SimpleUntypedObject.New(testObj.Id);
                    return true;
                case "$testSessionId":
                    result = SimpleUntypedObject.New(testObj.TestSessionId);
                    return true;
                case "$metadata":
                    result = new MetadataUntypedObject(testObj.RootMetadata);
                    return true;
                default:
                    if (testObj.TryGetProperty(value, out JToken prop))
                    {
                        result = SimpleUntypedObject.New(prop);
                        return true;
                    }

                    return false;
            }
        }
    }

    public abstract class BaseObjectUntypedObject : IUntypedObject
    {
        public IUntypedObject this[int index] => throw new NotImplementedException();

        public FormulaType Type => ExternalType.ObjectType;

        public int GetArrayLength()
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            throw new NotImplementedException();
        }

        public string GetString()
        {
            throw new NotImplementedException();
        }

        public abstract bool TryGetProperty(string value, out IUntypedObject result);
    }

    public class MetadataUntypedObject : BaseObjectUntypedObject, IUntypedObject
    {
        public RootMetadata metadata;

        public static UntypedObjectValue New(RootMetadata metadata)
        {
            var x = new MetadataUntypedObject(metadata);

            return FormulaValue.New(x);
        }

        public MetadataUntypedObject(RootMetadata metadata)
        {
            this.metadata = metadata;
        }

        public override bool TryGetProperty(string value, out IUntypedObject result)
        {
            result = null;
            switch (value)
            {
                case "type":
                    result = SimpleUntypedObject.New(metadata.Type);
                    return true;
                case "time":
                    result = SimpleUntypedObject.New(metadata.Time);
                    return true;
                default:
                    if (metadata.PropertyMetadata.ContainsKey(value))
                    {
                        result = new PropertyMetadataUntypedObject(metadata.PropertyMetadata[value]);
                        return true;
                    }

                    return false;
            }
        }
    }

    public class PropertyMetadataUntypedObject : BaseObjectUntypedObject, IUntypedObject
    {
        public PropertyMetadata metadata;

        public static UntypedObjectValue New(PropertyMetadata metadata)
        {
            var x = new PropertyMetadataUntypedObject(metadata);

            return FormulaValue.New(x);
        }

        public PropertyMetadataUntypedObject(PropertyMetadata metadata)
        {
            this.metadata = metadata;
        }

        public override bool TryGetProperty(string value, out IUntypedObject result)
        {
            result = null;
            switch (value)
            {
                case "type":
                    result = SimpleUntypedObject.New(metadata.Type);
                    return true;
                case "time":
                    result = SimpleUntypedObject.New(metadata.Time);
                    return true;
                default:
                    return false;
            }
        }
    }

    public class SimpleUntypedObject : IUntypedObject
    {
        public IUntypedObject this[int index] => throw new NotImplementedException();

        private readonly FormulaValue Value;

        public FormulaType Type => Value.Type;

        private SimpleUntypedObject(FormulaValue value)
        {
            Value = value;
        }

        public static SimpleUntypedObject New(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.String:
                    return new SimpleUntypedObject(FormulaValue.New(value.ToString()));
                case JTokenType.Boolean:
                    return new SimpleUntypedObject(FormulaValue.New((bool)value));
                case JTokenType.Float:
                    return new SimpleUntypedObject(FormulaValue.New((float)value));
                case JTokenType.Integer:
                    return new SimpleUntypedObject(FormulaValue.New((int)value));
                case JTokenType.None:
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return new SimpleUntypedObject(FormulaValue.NewBlank());
                case JTokenType.Date:
                case JTokenType.Object:
                case JTokenType.Array:
                default:
                    throw new NotSupportedException();
            }
        }

        public int GetArrayLength()
        {
            throw new NotImplementedException();
        }
        public bool GetBoolean()
        {
            return ((BooleanValue)Value).Value;
        }

        public double GetDouble()
        {
            return ((NumberValue)Value).Value;
        }

        public string GetString()
        {
            return ((StringValue)Value).Value;
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
        {
            throw new NotImplementedException();
        }
    }

    public class JsonNodeUntypedObject : IUntypedObject
    {
        private readonly JsonNode jsonNode;

        public IUntypedObject this[int index] => throw new NotImplementedException();

        public FormulaType Type => FormulaType.UntypedObject;

        public JsonNodeUntypedObject(JsonNode jsonNode)
        {
            this.jsonNode = jsonNode;
        }

        public int GetArrayLength()
        {
            if (jsonNode is JsonArray arr)
            {
                return arr.Count;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public bool GetBoolean()
        {
            if (jsonNode is JsonValue val && val.TryGetValue(out bool boolValue))
            {
                return boolValue;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public double GetDouble()
        {
            if (jsonNode is JsonValue val && val.TryGetValue(out double num))
            {
                return num;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public string GetString()
        {
            if (jsonNode is JsonValue val && val.TryGetValue(out string str))
            {
                return str;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
        {
            result = null;
            if (jsonNode is JsonObject jsonObj && jsonObj.TryGetPropertyValue(value, out JsonNode? propNode))
            {
                if (propNode == null)
                {
                    return false;
                }

                result = new JsonNodeUntypedObject(propNode);

                return true;
            }

            return false;
        }
    }
}
