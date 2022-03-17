// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Test marshaling between C# objectrs and Power Fx values. 
    public class MarshalTests
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
            
            var value = cache.Marshal((object)17, (Type)null);

            Assert.Equal(17.0, ((NumberValue)value).Value);
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
        private int _hookCounter = 0;

        private string Hook(PropertyInfo propInfo)
        {
            _hookCounter++;
            return propInfo.Name.StartsWith("_") ?
               null : // skip
                propInfo.Name + "Prop";
        }

        // Marshal objects with a custom hook. 
        [Fact]
        public void CustomMarshaling()
        {
            Assert.Equal(0, _hookCounter);

            // Be sure to use 'var' instead of 'object' so that we have compiler-time access to fields.           
            var fileObj = new
            {
                Filename = "foo.txt",
                Length = 12.0,
                _Skip = "skip me!"
            };

            var cache = new TypeMarshallerCache();
            cache.Marshallers.OfType<ObjectMarshallerProvider>().First().PropertyMapperFunc = Hook;

            var t = cache.GetMarshaller(fileObj.GetType());

            var x = t.Marshal(fileObj);
            Assert.Equal(3, _hookCounter); // Called once per property

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

            Assert.Equal(3, _hookCounter); // no furher calls during execution
        }

        private static string HookCollide(PropertyInfo propInfo) => "NameCollision";

        [Fact]
        public void NameCollision()
        {
            // Be sure to use 'var' instead of 'object' so that we have compiler-time access to fields.           
            var obj = new
            {
                Prop1 = 1.0,
                Prop2 = "two"
            };

            var cache = new TypeMarshallerCache();
            cache.Marshallers.OfType<ObjectMarshallerProvider>().First().PropertyMapperFunc = HookCollide;

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
        [Fact]
        public void With()
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
            var result2 = engine.Eval("With(x, ThisRecord.Field1*10+ThisRecord.Field1)");

            Assert.Equal(2, obj1._counter); // each field access is +1. 
            Assert.Equal(0, obj1._counter2); // Didn't touch field2. 
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
        private class WidgetMarshalerProvider : ITypeMashallerProvider
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

                marshaler = new WidgetMarshaler();
                return true;
            }

            private class WidgetMarshaler : ITypeMarshaller
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
        public void CustomMarshalerType()
        {
            var cache = new TypeMarshallerCache();

            // Insert at 0 to get precedence over generic object marshaller
            var marshaler = new WidgetMarshalerProvider();
            cache.Marshallers.Insert(0, marshaler);

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

            // Verify TypeMarshaler comes from cache and we don't call TryGetMarshaler again. 
            var w1 = cache.Marshal(obj.Widget1);
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
            var cache = new TypeMarshallerCache();
            cache.Marshallers.RemoveAll(mp => mp is PrimitiveMarshallerProvider);

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

            var result1 = fxTable.Index(1);
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
            var result1 = (RecordValue)fxTable.Index(1).Value;
            Assert.Equal(21.0, result1.GetField("Field1").ToObject());
        }
    }
}
