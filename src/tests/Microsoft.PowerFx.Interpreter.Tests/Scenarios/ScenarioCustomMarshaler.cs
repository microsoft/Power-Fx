// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // Demonstrate custom marshaler with attributes
    public class ScenarioCustomMarshaller
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
    }
}
