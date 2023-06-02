// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.Scenarios
{
#if false
NewDict(); // returns a string/string dictionary, which acts like an opaque object. 

GetValue(dict, key) // value or blank. returns string

Add(dict, key, value) // no return 

GetKeys(dict); // returns single column table of all keys in the dict

#endif

    public class DictionaryFunctionTests
    {
        [Fact]
        public void Test1()
        {
            var dict = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" }
            };

            var config = new PowerFxConfig();
            config.AddFunction(new XGetValueFunction());

            var engine = new RecalcEngine(config);
            engine.UpdateVariable("dict", new MyDictionaryValue(dict));

            var result = engine.Eval("XGetValue(dict, \"Key1\") & 999").ToObject();
            Assert.Equal("Value999", result);
        }

        // Marshal a dictionary 
        [Fact]
        public void Test2()
        {
            var dict = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" }
            };
            var poco = new MyPoco
            {
                IntField = 123,
                MyProps = dict
            };

            var config = new PowerFxConfig();
            config.AddFunction(new XGetValueFunction());

            var engine = new RecalcEngine(config);

            var cache = new TypeMarshallerCache()
                .NewPrepend(new MyDictionaryMarshaller());
            var fxPoco = cache.Marshal(poco);

            engine.UpdateVariable("poco", fxPoco);

            var result = engine.Eval("XGetValue(poco.MyProps, \"Key1\") & 999").ToObject();
            Assert.Equal("Value999", result);
        }
    }

    public class MyDictionaryMarshaller : ITypeMarshallerProvider
    {
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, out ITypeMarshaller marshaller)
        {
            if (type == typeof(Dictionary<string, string>))
            {
                marshaller = new MyMarshaller();
                return true;
            }

            marshaller = null;
            return false;
        }

        private class MyMarshaller : ITypeMarshaller
        {
            public FormulaType Type => MyDictionaryValue.ParamType;

            public FormulaValue Marshal(object value)
            {
                var dict = (Dictionary<string, string>)value;

                return new MyDictionaryValue(dict);
            }
        }
    }

    // Wraps a dictionary. 
    // Treated as an opaque type, only accessible via functions
    public class MyDictionaryValue : RecordValue
    {
        public readonly Dictionary<string, string> _dict;

        // We don't have a type-safe way to limit to this type. 
        public static readonly RecordType ParamType = RecordType.Empty();

        public MyDictionaryValue(Dictionary<string, string> dict)
            : base(MyDictionaryValue.ParamType)
        {
            _dict = dict;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            result = null;
            return false;
        }
    }

    // We don't have a type-safe way to pass
    public class XGetValueFunction : ReflectionFunction
    {
        public XGetValueFunction() 
            : base("XGetValue", FormulaType.String, MyDictionaryValue.ParamType, FormulaType.String)
        {
        }

        public StringValue Execute(MyDictionaryValue dict, StringValue key)
        {
            var val = dict._dict[key.Value];
            return FormulaValue.New(val);
        }
    }

    public class MyPoco
    {
        // Normal field, easy for marshaller. 
        public int IntField { get; set; }

        // Dynamic 
        public Dictionary<string, string> MyProps { get; set; }
    }
}
