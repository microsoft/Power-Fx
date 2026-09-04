using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace PowerFXBenchmark.Inputs.Models
{
    public interface ITestObjectBaseSchema
    {
        public TestObjDataKind Kind { get; }
    }

    public class TestObjectSchema : ITestObjectBaseSchema
    {
        public TestObjDataKind Kind => TestObjDataKind.Root;

        public Dictionary<string, ITestObjectBaseSchema> NestedSchemas { get; set; }
    }

    public class NamedSchema : ITestObjectBaseSchema
    {
        public TestObjDataKind Kind => TestObjDataKind.Named;

        public string Name { get; set; }

        public ITestObjectBaseSchema Value { get; set; }
    }

    public class ArraySchema : ITestObjectBaseSchema
    {
        public TestObjDataKind Kind => TestObjDataKind.Array;

        public ITestObjectBaseSchema ArrayElementSchema { get; set; }
    }

    public class MapSchema : ITestObjectBaseSchema
    {
        public TestObjDataKind Kind => TestObjDataKind.Map;

        public NamedSchema MapElementSchema { get; set; }
    }

    public class PrimitiveSchema : ITestObjectBaseSchema
    {
        public TestObjDataKind Kind { get; private set;}

        public PrimitiveSchema (TestObjDataKind kind)
        {
            Kind = kind;
        }
    }

    public enum TestObjDataKind
    {
        Root,
        Named,
        Array,
        Boolean,
        Date,
        DateTime,
        Double,
        Float,
        Integer,
        Long,
        Map,
        String,
        Time,
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
