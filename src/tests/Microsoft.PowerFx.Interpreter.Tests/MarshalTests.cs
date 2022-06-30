// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Test marshaling between C# objects and Power Fx values. 
    public class MarshalTests : PowerFxTest
    {
        // Do a trivial marshal.
        [Fact]
        public void Primitive()
        {
            var cache = new TypeMarshallerCache();
            var tm = cache.GetMarshaller(typeof(int));

            var value = tm.Marshal(5);

            Assert.Equal(5.0, ((NumberValue)value).Value);
        }

        // A FormulaValue should be passed through. 
        [Fact]
        public void MarshalFormulaValue()
        {
            var cache = new TypeMarshallerCache();
            var val1 = FormulaValue.New(17);
         
            var val2 = cache.Marshal(val1);

            Assert.Equal(val1.ToObject(), val2.ToObject());
        }

        // Can't marshal FormulaValue type statically 
        [Fact]
        public void CantMarshalFormulaValueType()
        {
            var cache = new TypeMarshallerCache();

            Assert.Throws<InvalidOperationException>(() => cache.GetMarshaller(typeof(NumberValue)));
            Assert.Throws<InvalidOperationException>(() => cache.GetMarshaller(typeof(NumberType)));
        }

        [Fact]
        public void MarshalWithoutType()
        {
            var cache = new TypeMarshallerCache();

            // Type is a required parameter - being explicit. 
            Assert.Throws<ArgumentNullException>(() => cache.Marshal(17, null));

            // Must be more specific than object
            Assert.Throws<ArgumentException>(() => cache.Marshal((object)17));

            cache.Marshal(17); // ok, 
        }

        [Fact]
        public void Nesting()
        {
            // Be sure to use 'var' instead of 'object' so that we have compiler-time access to fields.           
            var oneObj = new
            {
                data1 = "one",
                two = new
                {
                    data2 = "two",
                    three = new
                    {
                        data3 = "three"
                    }
                }
            };

            var cache = new TypeMarshallerCache();
            var t = cache.GetMarshaller(oneObj.GetType());

            var x = t.Marshal(oneObj);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            var result1 = engine.Eval("x.two.three.data3");
            Assert.Equal("three", ((StringValue)result1).Value);
        }

        private class TestNode
        {
            public int Data { get; set; }

            public TestNode Next { get; set; }
        }

        [Fact]
        public void TestBlank()
        {
            var node1 = new TestNode
            {
                Data = 10,
                Next = null
            };

            var cache = new TypeMarshallerCache();
            var x = cache.Marshal(node1);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            var result1 = engine.Eval("x.Next");
            Assert.IsType<BlankValue>(result1);
        }

        [Fact]
        public void TestRecursion()
        {
            // Be sure to use 'var' instead of 'object' so that we have compiler-time access to fields.           
            var node1 = new TestNode
            {
                Data = 10,
                Next = new TestNode
                {
                    Data = 20,
                    Next = new TestNode
                    {
                        Data = 30
                    }
                }
            };

            // create a cycle. 
            // Recursion has a default marshalling depth. 
            node1.Next.Next.Next = node1;

            var cache = new TypeMarshallerCache();
            var x = cache.Marshal(node1);

            // If we made it here, at leat we didn't hang marshalling the infinite cycle. 

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            var result1 = engine.Eval("x.Data");
            Assert.Equal(10.0, ((NumberValue)result1).Value);

            var result2 = engine.Eval("x.Next.Data");
            Assert.Equal(20.0, ((NumberValue)result2).Value);

            var result3 = engine.Eval("x.Next.Next.Data");
            Assert.Equal(30.0, ((NumberValue)result3).Value);

            // Recursion not supported - gets truncated...
            // https://github.com/microsoft/Power-Fx/issues/225
            var result4 = engine.Check("x.Next.Next.Next.Next.Next.Next");
            Assert.False(result4.IsSuccess);
        }

        // Basic marshaling hook. 
        private class Custom1ObjectMarshallerProvider : ObjectMarshallerProvider
        {
            public int _hookCounter = 0;

            public override string GetFxName(PropertyInfo propertyInfo)
            {
                _hookCounter++;

                return propertyInfo.Name.StartsWith("_") ?
                        null : // skip
                        propertyInfo.Name + "Prop";
            }
        }

        // Marshal objects with a custom hook. 
        [Fact]
        public void CustomMarshaling()
        {
            var custom = new Custom1ObjectMarshallerProvider();
            Assert.Equal(0, custom._hookCounter);

            // Be sure to use 'var' instead of 'object' so that we have compiler-time access to fields.           
            var fileObj = new
            {
                Filename = "foo.txt",
                Length = 12.0,
                _Skip = "skip me!"
            };

            var cache = TypeMarshallerCache.New(custom);

            var t = cache.GetMarshaller(fileObj.GetType());

            var x = t.Marshal(fileObj);
            Assert.Equal(3, custom._hookCounter); // Called once per property

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            // Properties are renamed. 
            var result1 = engine.Eval("x.LengthProp");
            Assert.Equal(((NumberValue)result1).Value, fileObj.Length);

            // Get object back out
            var result2 = engine.Eval("If(true, x, Blank())");
            var fileObj2 = ((ObjectRecordValue)result2).Source;
            Assert.True(ReferenceEquals(fileObj, fileObj2));

            // Ensure skipped property is not visible. 
            var check3 = engine.Check("x._SkipProp");
            Assert.False(check3.IsSuccess);

            Assert.Equal(3, custom._hookCounter); // no furher calls during execution
        }

        // Marshaller where all names collide. 
        private class CollideObjectMarshallerProvider : ObjectMarshallerProvider
        {
            public override string GetFxName(PropertyInfo propertyInfo)
            {
                return "NameCollision";
            }
        }

        [Fact]
        public void NameCollision()
        {
            // Be sure to use 'var' instead of 'object' so that we have compiler-time access to fields.           
            var obj = new
            {
                Prop1 = 1.0,
                Prop2 = "two"
            };

            var cache = TypeMarshallerCache.New(new CollideObjectMarshallerProvider());

            Assert.Throws<NameCollisionException>(() => cache.GetMarshaller(obj.GetType()));
        }

        private class TestObj
        {
            internal int _counter;

            public int Field1
            {
                get
                {
                    _counter++; // for observing number of Gets
                    return _counter;
                }
            }

            internal int _counter2;

            public int Field2
            {
                get
                {
                    _counter2++; // for observing number of Gets
                    return _counter2;
                }
            }
        }

        // Verify that marshalling doesn't eagerly evaluate fields.
        [Fact]
        public void LazyFields()
        {
            var obj1 = new TestObj();

            Assert.Equal(1, obj1.Field1);
            Assert.Equal(2, obj1.Field1);
            Assert.Equal(2, obj1._counter);

            var cache = new TypeMarshallerCache();

            var x = cache.Marshal(obj1);
            Assert.Equal(2, obj1._counter); // doesn't increment

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            Assert.Equal(2, obj1._counter); // doesn't increment
            var result1 = engine.Eval("x.Field1");
            Assert.Equal(3.0, ((NumberValue)result1).Value);
        }

        // Test using a marshalled object inside of With()
        [Theory]
        [InlineData("With(x, ThisRecord.Field1*10+ThisRecord.Field1)", 2)]
        [InlineData("{ f1 : x }.f1.Field1", 1)]
        public void With(string expr, int expectedCount)
        {
            var obj1 = new TestObj();

            Assert.Equal(0, obj1._counter);

            var cache = new TypeMarshallerCache();

            var x = cache.Marshal(obj1);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            Assert.Equal(0, obj1._counter); // doesn't increment
            Assert.Equal(0, obj1._counter2);

            // Verify lazy access around With(). 
            _ = engine.Eval(expr);

            Assert.Equal(expectedCount, obj1._counter); // each field access is +1. 
            Assert.Equal(0, obj1._counter2); // Didn't touch field2. 
        }

        // Test that derived RecordValues can pass through the system "unharmed" and don't get
        // flattened into InMemoryRecordValue.
        // There's no type unification here.
        [Theory]
        [InlineData("{ f1 : x }.f1")]
        [InlineData("If(false, { field1 : 12 }, x)")]
        [InlineData("Last(Table(y,x))")]
        public void PassThroughRecordValue(string expr)
        {
            var x = new MyRecordValue();
            var y = new MyRecordValue();
            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);
            engine.UpdateVariable("y", y);

            // Wrap in a record. 
            var result = engine.Eval(expr);

            var obj = result.ToObject();
            Assert.True(object.ReferenceEquals(x, obj));
        }

        [Fact]
        public void MixRecordValueImplementationsTable()
        {
            var x = new MyRecordValue();
            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x); // x has field1. 

            // Wrap in a record. 
            engine.SetFormula("t", "Table(x,{field2:12})", (str, val) => { });
            var result1 = engine.Eval("First(t).field2");
            Assert.IsType<BlankValue>(result1);

            var result2 = engine.Eval("Last(t).field1");
            Assert.IsType<BlankValue>(result2);
        }

        [Fact]
        public void MixRecordValueImplementationsIf()
        {
            var x = new MyRecordValue();
            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x); // x has field1. 

            // Wrap in a record. 
            var result1 = engine.Eval("If(false, {field1:11, field2:22}, x).field1");
            Assert.Equal(999.0, result1.ToObject());
        }
        
        [Fact]
        public void TypeProjectionWithCustomRecords()
        {
            var x = new MyRecordValue();
            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x); // x has field1. 

            // Ensure that compile-time type unification works even when 
            // mixing custom derived RecordValues and builtin record values. 
            var result = engine.Eval(
@"First(
        Table(
            {a:x},
            {a:{field2:222},b:22}
        )).a.field1
");

            Assert.Equal(999.0, result.ToObject());
        }

        // Example of a host-derived object. 
        private class MyRecordValue : RecordValue
        {
            private static readonly RecordType _type = new RecordType().Add("field1", FormulaType.Number);

            // Ctor to let tests override and provide wrong types.
            public MyRecordValue(RecordType type)
                : base(type)
            {
            }

            public MyRecordValue() 
                : base(_type)
            {
            }

            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                if (fieldName == "field1")
                {
                    result = FormulaValue.New(999);
                    return true;
                }

                result = null;
                return false;                
            }

            public override object ToObject()
            {
                return this;
            }
        }

        // Test that we catch poor implementations in host-provided RecordValues.
        // These are all fortifying against host bugs. 
        [Theory]
        [InlineData(typeof(MyRecordValue))]
        [InlineData(typeof(MyBadRecordValue))]
        [InlineData(typeof(MyBadRecordValue2))]
        [InlineData(typeof(MyBadRecordValueMismatch))]
        [InlineData(typeof(MyBadRecordValueThrows))]        
        public async Task HostBugNullMismatch(Type recordType)
        {
            var x = (RecordValue)Activator.CreateInstance(recordType);
            var shouldSucceed = recordType == typeof(MyRecordValue);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x); // x has field1. 

            var expr = "x.field1";
            var checkResult = engine.Check(expr);
            Assert.True(checkResult.IsSuccess);

            if (shouldSucceed)
            {
                // For comparison, verify we can succeed. 
                var result = await engine.EvalAsync("x.field1", CancellationToken.None);
                Assert.Equal(999.0, result.ToObject());
            }
            else
            { 
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await engine.EvalAsync("x.field1", CancellationToken.None));
            }            
        }

        private class MyBadRecordValueMismatch : MyRecordValue
        {
            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                // Error! we advertise field1 should be a number!
                result = FormulaValue.New("a string");
                return true;
            }
        }

        private class MyBadRecordValue : MyRecordValue
        {
            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                result = null;
                return true; // Should be false
            }
        }

        private class MyBadRecordValue2 : MyRecordValue
        {
            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                base.TryGetField(fieldType, fieldName, out result);
                return false; // should be true. 
            }
        }

        private class MyBadRecordValueThrows : MyRecordValue
        {
            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                // Exceptions here are implementation errors and should propagate. 
                // A fx runtime error should be a ErrorValue instead.
                throw new InvalidOperationException($"Throw from within");
            }
        }

        private class BasePoco
        {
            public int Base { get; set; }
        }

        private class DerivedPoco : BasePoco
        {
            public int Derived { get; set; }
        }

        // Inheritence
        // - derived class, pass in base type. 
        // - derived class, pass in derived type. 
        [Theory]
        [InlineData(typeof(BasePoco), "{Base:10}")] // don't include derived
        [InlineData(typeof(DerivedPoco), "{Base:10,Derived:20}")] // include all
        public void Inheritence(Type marshalAsType, string expected)
        {
            var obj = new DerivedPoco
            {
                Base = 10,
                Derived = 20,
            };

            var cache = new TypeMarshallerCache();
            var fxObj = (RecordValue)cache.Marshal(obj, marshalAsType);

            Assert.Equal(expected, fxObj.Dump());
        }
             
        // Use 'new' to create a property in derived that collides with base. 
        private class Derived2Poco : BasePoco
        {
            public new string Base { get; set; } // Use New to hide base field!
        }

        [Theory]
        [InlineData(typeof(Derived2Poco), null)]
        [InlineData(typeof(BasePoco), "{Base:10}")] // don't include derived
        public void InheritenceNewOverride(Type marshalAsType, string expected)
        {
            var obj = new Derived2Poco
            {
                Base = "hi"
            };
            ((BasePoco)obj).Base = 10;

            var cache = new TypeMarshallerCache();

            if (expected == null)
            {
                // Base and Derived both have a property called 'Base', that's a collision. 
                Assert.Throws<NameCollisionException>(() => cache.Marshal(obj, marshalAsType));
            }
            else
            {
                var fxObj = cache.Marshal(obj, marshalAsType);
                Assert.Equal(expected, fxObj.Dump());
            }
        }

        // Use 'new' to create a property in derived that collides with base. 
        private class VirtualBase
        {
            public virtual int Base { get; } = 15;
        }

        private class VirtualDerived : VirtualBase
        {
            public override int Base { get; } = 30;

            public int Derived { get; } = 40;
        }

        // With polymorphism, we pull the derived property. 
        [Theory]
        [InlineData(typeof(VirtualDerived), "{Base:30,Derived:40}")]
        [InlineData(typeof(VirtualBase), "{Base:30}")] 
        public void InheritenceVirtuals(Type marshalAsType, string expected)
        {
            var obj = new VirtualDerived();

            var cache = new TypeMarshallerCache();

            var fxObj = cache.Marshal(obj, marshalAsType);
            Assert.Equal(expected, fxObj.Dump());            
        }

        // Marshal an array of records to a table. 
        [Fact]
        public void TableFromRecordArray()
        {
            var array = new TestObj[]
            {
                new TestObj { _counter = 10 },
                new TestObj { _counter = 20 }
            };

            var cache = new TypeMarshallerCache();
            var x = cache.Marshal(array);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            var result1 = engine.Eval("Last(x).Field1");
            Assert.Equal(21.0, ((NumberValue)result1).Value);

            var result2 = engine.Eval("First(x).Field1");
            Assert.Equal(11.0, ((NumberValue)result2).Value);
        }

        // Marshal a SCT from an array of primitive. 
        [Fact]
        public void SingleColumnTableFromPrimitiveArray()
        {
            var array = new int[] { 10, 20, 30 };

            var cache = new TypeMarshallerCache();
            var x = cache.Marshal(array);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            var result1 = engine.Eval("Last(x).Value");
            Assert.Equal(30.0, ((NumberValue)result1).Value);
        }

        // None of these should marshal. 
        private class PocoNotMarshalled
        {
            private int Prop1 { get; set; } // not public

            protected int Prop2 { get; set; } // not public

            public int _field; // not a property 

            public static int PropStatic { get; set; } // not static

            public int PropNoGet { set => _field = value; } // write-only 
        }

        [Fact]
        public void NotMarshalled()
        {
            var cache = new TypeMarshallerCache();
            var tm = cache.GetMarshaller(typeof(PocoNotMarshalled));
            var fxType = (RecordType)tm.Type;

            Assert.Empty(fxType.GetNames());
        }

        private interface IWidget
        {
            public string Data { get; }
        }

        public class Widget : IWidget
        {
            public string Data { get; set; }
        }

        // Custom marshaller. Marshal Widget objects as Strings with a "W" prefix. 
        private class WidgetMarshallerProvider : ITypeMarshallerProvider
        {
            public int _counter = 0;

            public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaler)
            {
                if (type != typeof(IWidget))
                {
                    marshaler = null;
                    return false;
                }

                _counter++;

                marshaler = new WidgetMarshaller();
                return true;
            }

            private class WidgetMarshaller : ITypeMarshaller
            {
                public FormulaType Type => FormulaType.String;

                public FormulaValue Marshal(object value)
                {
                    // adding a "W" prefix ensures this code is run and it's not just some cast. 
                    var w = (Widget)value;
                    return FormulaValue.New("W" + w.Data);
                }
            }
        }

        // Fails to marshal without the customer marshaller. 
        [Fact]
        public void FailWithoutCustomMarshaller()
        {
            var cache = new TypeMarshallerCache();

            var obj = new
            {
                Length = 12.0,
                Widget1 = (IWidget)new Widget
                {
                    Data = "A"
                },
                Widget2 = (IWidget)new Widget
                {
                    Data = "B"
                }
            };
            Assert.Throws<InvalidOperationException>(() => cache.Marshal(obj));
        }

        // Test a custom marshaler. 
        [Fact]
        public void CustomMarshallerType()
        {
            var cache = new TypeMarshallerCache();
                        
            var marshaler = new WidgetMarshallerProvider();
            cache = cache.NewPrepend(marshaler);

            Assert.Equal(0, marshaler._counter);
            var obj = new
            {
                Length = 12.0,
                Widget1 = (IWidget)new Widget
                {
                    Data = "A"
                },
                Widget2 = (IWidget)new Widget
                {
                    Data = "B"
                }
            };

            var x = cache.Marshal(obj);
            Assert.Equal(1, marshaler._counter);

            // Verify TypeMarshaller comes from cache and we don't call TryGetMarshaller again. 
            _ = cache.Marshal(obj.Widget1);
            Assert.Equal(1, marshaler._counter);

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", x);

            // Properties are renamed. 
            var result1 = engine.Eval("x.Widget1 & x.Widget2");
            Assert.Equal("WAWB", ((StringValue)result1).Value);
        }
        
        // Test something that can't be marshalled. 
        [Fact]
        public void FailMarshal()
        {
            var cache = TypeMarshallerCache.Empty;

            Assert.Throws<InvalidOperationException>(() => cache.Marshal(5));
        }

        private class MyTable : IReadOnlyList<TestObj>
        {
            public TestObj[] _values;

            public TestObj this[int index]
            {
                get
                {
                    var x = _values[index];
                    x._counter2++; // Create a side-effect for the test. 
                    return x;
                }
            }

            public int Count => _values.Length;

            // Lazy access - shouldn't ever call the enumerator.
            public IEnumerator<TestObj> GetEnumerator() => throw new NotImplementedException();
            
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();            
        }

        // Verify it's lazy and enumerator is never called. 
        [Fact]
        public void TableIndex()
        {
            var cache = new TypeMarshallerCache();
            
            // _counter bumped for each field fetch. 
            // _counter2 bumped for each index fetch. 
            var values = new TestObj[]
            {
                new TestObj { _counter = 10 },
                new TestObj { _counter = 20 }
            };
            var myTable = new MyTable { _values = values };

            var fxTable = (TableValue)cache.Marshal(myTable);

            _ = fxTable.Index(2);
            Assert.Equal(10, values[0]._counter); // no field fetch
            Assert.Equal(20, values[1]._counter);
            Assert.Equal(0, values[0]._counter2);
            Assert.Equal(1, values[1]._counter2); // only fetch on requested index. 

            var engine = new RecalcEngine();
            engine.UpdateVariable("x", fxTable);

            var result2 = engine.Eval("Index(x, 2).Field1").ToObject();
            Assert.Equal(10, values[0]._counter); // unchanged.
            Assert.Equal(21, values[1]._counter);
            Assert.Equal(21.0, result2);

            Assert.Equal(0, values[0]._counter2);
            Assert.Equal(2, values[1]._counter2); // index fetch again
        }

        // IEnumerable only. 
        private class MyEnumeratorOnlyTable
        {
            public TestObj[] _data = new TestObj[]
            {
                new TestObj { _counter = 10 },
                new TestObj { _counter = 20 }
            };

            public IEnumerable<TestObj> GetTable()
            {
                // use yield to ensure we only impl IEnum, and not other interfaces like Array.
                foreach (var item in _data)
                {
                    yield return item;
                }
            }
        }

        [Fact]
        public void TableEnumeratorOnly()
        {
            var cache = new TypeMarshallerCache();
            var myTable = new MyEnumeratorOnlyTable();

            var fxTable = (TableValue)cache.Marshal(myTable.GetTable());

            // Table doesn't have indexer, so index is a linear scan. 
            var result1 = fxTable.Index(2).Value;
            Assert.Equal(21.0, result1.GetField("Field1").ToObject());
        }

        private class MyDynamicMarshaller : IDynamicTypeMarshaller
        {
            public FormulaValue _result;

            public bool TryMarshal(TypeMarshallerCache cache, object value, out FormulaValue result)
            {
                result = _result;
                return result != null;
            }
        }

        [Fact]
        public void DynamicMarshaller()
        {
            var marshaller = new MyDynamicMarshaller
            {
                _result = FormulaValue.New(15)
            };
            var cache = new TypeMarshallerCache()
                .WithDynamicMarshallers(marshaller);

            var obj = 333;

            // Invokes marshaller
            // Dynamic marshallers run before Static ones, so it will take precedence. 
            var result = cache.Marshal(obj);
            Assert.True(object.ReferenceEquals(marshaller._result, result));

            // Disable dynamic marshaller. Static will claim it. 
            marshaller._result = null;
            var result2 = cache.Marshal(obj);
            Assert.Equal(333.0, result2.ToObject());
        }
    }
}
