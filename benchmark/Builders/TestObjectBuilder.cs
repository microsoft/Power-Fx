#pragma warning disable CA1051
namespace PowerFXBenchmark.Builders
{
    using System;
    using Newtonsoft.Json.Linq;
    using PowerFXBenchmark.Inputs.Models;

    public class TestObjectBuilder
    {
        protected TestObject testObj;

        private string updateTime = DateTime.UtcNow.ToString("o");

        public TestObjectBuilder() : this(new TestObject())
        {
        }

        public TestObjectBuilder(TestObject testObj)
        {
            this.testObj = testObj;
        }

        public TestObjectBuilder WithId(string id)
        {
            testObj.Id = id;
            return this;
        }

        public TestObjectBuilder WithType(string typeName)
        {
            testObj.RootMetadata.Type = typeName;
            return this;
        }

        public TestObjectBuilder WithRootProperty(string propertyName, JToken propertyValue)
        {
            testObj.JTokenBag[propertyName] = propertyValue;
            testObj.RootMetadata.PropertyMetadata[propertyName] = new PropertyMetadata()
            {
                Time = updateTime,
            };
            return this;
        }

        public TestObjectBuilder AtTime(DateTime time)
        {
            updateTime = time.ToString("o");
            return this;
        }

        public TestObjectBuilder AtTime(DateTime time, string propertyName)
        {
            testObj.RootMetadata.Time = time.ToString("o");
            testObj.RootMetadata.PropertyMetadata[propertyName].Time = time.ToString("o");
            return this;
        }

        public TestObject Build() => testObj;
    }
}
