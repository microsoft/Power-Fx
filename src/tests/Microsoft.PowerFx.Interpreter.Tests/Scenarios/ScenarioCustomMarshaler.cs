// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // Demonstrate custom marshaler with attributes
    public class ScenarioCustomMarshaller : PowerFxTest
    {
        private class TestObj
        {
            [DisplayName("FieldDisplay1")]
            public int Field1 { get; set; }

            public int Field2 { get; set; }
        }

        private class CustomObjectMarshallerProvider : ObjectMarshallerProvider
        {
            public override string GetFxName(PropertyInfo propertyInfo)
            {
                var attr = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
                if (attr != null)
                {
                    return attr.DisplayName;
                }

                return propertyInfo.Name;
            }
        }

        [Fact]
        public void Test()
        {
            // Be sure to use 'var' instead of 'object' so that we have compiler-time access to fields.           
            var obj = new TestObj
            {
                Field1 = 10,
                Field2 = 20
            };

            var custom = new CustomObjectMarshallerProvider();
            var cache = new TypeMarshallerCache().NewPrepend(custom);

            var x = cache.Marshal(obj);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            // Properties are renamed. 
            var result1 = engine.Eval("x.FieldDisplay1");
            Assert.Equal(10.0, ((NumberValue)result1).Value);

            var check1 = engine.Check("x.Field");
            Assert.False(check1.IsSuccess);

            var result2 = engine.Eval("x.Field2");
            Assert.Equal(20.0, ((NumberValue)result2).Value);
        }

        // Scenario for Wrapper<T> objects. Marshal as a T. 
        // We can just add a new ITypeMarshallerProvider that chains to existing marshallers.
        private class Wrapper<T>
        {
            public T _value;
        }

        // TypeMarshaller that will claim all Wrapper<T> objects and:
        // - marshal it as a T. This can chain to other marshallers. 
        // - but still ToObject() as the original Wrapper<T>. 
        private class WrapperMarshallerProvider : ITypeMarshallerProvider
        {
            private static bool GetElementType(Type type, Type genericDef, out Type elementType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDef)
                {
                    elementType = type.GenericTypeArguments[0];
                    return true;
                }
                else
                {
                    elementType = null;
                    return false;
                }
            }

            public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, out ITypeMarshaller marshaller)
            {
                if (!GetElementType(type, typeof(Wrapper<>), out var elementType))
                {
                    marshaller = null;
                    return false;
                }

                var innerMarshaller = cache.GetMarshaller(elementType);

                var t2 = typeof(Adapter<>).MakeGenericType(elementType);
                marshaller = (ITypeMarshaller)Activator.CreateInstance(t2, innerMarshaller);
                return true;
            }

            // The adapter will access Wrapper<T>._value and chain to inner marshaller.
            public class Adapter<T> : ITypeMarshaller
            {
                private readonly ITypeMarshaller _inner;

                public Adapter(ITypeMarshaller inner)
                {
                    _inner = inner;
                }                   

                public FormulaType Type => _inner.Type;                

                public FormulaValue Marshal(object value)
                {
                    var x = (Wrapper<T>)value;
                    var result = _inner.Marshal(x._value);

                    // Wrap so that we can still chain ToObject(). 
                    return new WrapperRecordValue((RecordValue)result, value);
                }
            }

            // Demonstrating wrapping a RecordValue as overriding the ToObject() property
            private class WrapperRecordValue : RecordValue
            {
                private readonly RecordValue _inner;
                private readonly object _source;

                public WrapperRecordValue(RecordValue inner, object source)
                    : base(inner.Type)
                {
                    _inner = inner;
                    _source = source;
                }

                public override object ToObject()
                {
                    // Return the override for ToObject. 
                    return _source;
                }

                protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
                {
                    // Forward all field lookups
                    result = _inner.GetField(fieldName);
                    return true;
                }
            }
        }

        [Fact]
        public void TestWrapper()
        {
            // Demonstrate that we can create a marshaller to handle Wrapper<T>, 
            // and it chains the T meaning that it picks up any other custom
            // marshallers for the T. 
            var custom1 = new WrapperMarshallerProvider();
            var custom2 = new CustomObjectMarshallerProvider();
            var cache = TypeMarshallerCache.New(custom2).NewPrepend(custom1);

            var inner = new TestObj
            {
                // custom2 will marshall as 'FieldDisplay1'
                Field1 = 10,
                Field2 = 20
            };

            var obj = new Wrapper<TestObj> { _value = inner };

            var x = cache.Marshal(obj);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            var result2 = engine.Eval("x");
            Assert.True(ReferenceEquals(result2.ToObject(), obj));

            var result1 = engine.Eval("x.FieldDisplay1");
            Assert.Equal(10.0, ((NumberValue)result1).Value);
        }

        // Demonstrate how to lazily marshal a property bag to a strongly typed value if we're given the type
        private static RecordValue Marshal(Dictionary<string, object> values, TypeMarshallerCache cache, BaseRecordType type)
        {
            var fieldGetters = new Dictionary<string, ObjectMarshaller.FieldTypeAndValueMarshallerGetter>();
            foreach (var kv in values)
            {
                var fieldName = kv.Key;

                (FormulaType fieldType, ObjectMarshaller.FieldValueMarshaller fieldValueMarshaller) TypeAndMarshallerGetter()
                {
                    var marshaller = cache.GetMarshaller(kv.Value.GetType());
                    var expectedType = type.GetFieldType(fieldName);                        

                    Assert.True(expectedType.DType.Accepts(marshaller.Type.DType, exact: true));

                    return (marshaller.Type,
                            (object objSource) =>
                            {
                                var dict = (Dictionary<string, object>)objSource;
                                var fieldValue = dict[fieldName];

                                var result = marshaller.Marshal(fieldValue);
                                return result;
                            });
                }

                fieldGetters.Add(fieldName, TypeAndMarshallerGetter);
            }

            var om = new ObjectMarshaller(fieldGetters, typeof(Dictionary<string, object>));            
            var value = (RecordValue)om.Marshal(values);
            return value;
        }

        [Fact]
        public void TestDictWithKnownType()
        {
            // We can have a RecordValue backed by a dynamic dictionary, with lazy field evaluation.
            // We just need to know the record type. 
            var d = new Dictionary<string, object>
            {
                { "int", 123 },
                { "str", "string" },
                { "test", new TestObj { Field1 = 0, Field2 = 22 } },
                { "bool", true }
            };

            var recordTypeTestObj = new RecordType()
                .Add("Field1", FormulaType.Number)
                .Add("Field2", FormulaType.Number);

            var recordType = new RecordType()
                .Add("int", FormulaType.Number)
                .Add("str", FormulaType.String)
                .Add("test", recordTypeTestObj)
                .Add("bool", FormulaType.Boolean);

            var cache = new TypeMarshallerCache();
            var x = Marshal(d, cache, recordType);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            // runtime casting error when we access this field. 
            // This also proves access is lazy. 
            d["bool"] = "error: not a bool";

            d["int"] = 456; // lazy, takes latest values. 
            var result1 = engine.Eval("x.int");
            Assert.Equal(456.0, ((NumberValue)result1).Value);

            ((TestObj)d["test"]).Field1 = 12;
            var result2 = engine.Eval("x.test.Field1");
            Assert.Equal(12.0, ((NumberValue)result2).Value);

            // It's strongly typed, so we can give design-time errors for missing fields. 
            var result3 = engine.Check("x.missing");
            Assert.False(result3.IsSuccess);

            // runtime casting error.             
            Assert.Throws<AggregateException>(() => engine.Eval("x.bool"));
        }
    }
}
