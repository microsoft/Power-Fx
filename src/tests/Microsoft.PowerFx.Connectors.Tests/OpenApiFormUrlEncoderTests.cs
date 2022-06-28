// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Connectors.Tests.OpenApiHelperFunctions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class OpenApiFormUrlEncoderTests : PowerFxTest
    {
        [Fact]
        public void UrlEncoderSerializer_Empty()
        {
            var str = SerializeUrlEncoder(null);
            Assert.Equal(string.Empty, str);
        }

        [Fact]
        public void UrlEncoderSerializer_SingleInteger()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaInteger, FormulaValue.New(1))
            });

            Assert.Equal("a=1", str);
        }

        [Fact]
        public void UrlEncoderSerializer_Number()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaNumber, FormulaValue.New(1.17e-4))
            });

            Assert.Equal("a=0.000117", str);
        }

        [Fact]
        public void UrlEncoderSerializer_EscapedKey()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a\\b\"c"] = (SchemaInteger, FormulaValue.New(1))
            });

            Assert.Equal("a%5cb%22c=1", str);
        }

        [Fact]
        public void UrlEncoderSerializer_SingleInteger_NoValue()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaInteger, null)
            }));

            Assert.Equal("Expected NumberValue (integer) and got <null> value, for property a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_SingleInteger_NoSchema()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (null, null)
            }));

            Assert.Equal("Missing schema for property a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_Integer_String_Mismatch()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaInteger, FormulaValue.New("abc"))
            }));

            Assert.Equal("Expected NumberValue (integer) and got StringValue value, for property a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_Number_String_Mismatch()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaNumber, FormulaValue.New("abc"))
            }));

            Assert.Equal("Expected NumberValue (number) and got StringValue value, for property a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_String_Integer_Mismatch()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaString, FormulaValue.New(11))
            }));

            Assert.Equal("Expected StringValue and got NumberValue value, for property a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_Bool_String_Mismatch()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaBoolean, FormulaValue.New("abc"))
            }));

            Assert.Equal("Expected BooleanValue and got StringValue value, for property a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_TwoIntegers()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaInteger, FormulaValue.New(1)),
                ["b"] = (SchemaInteger, FormulaValue.New(-2))
            });

            Assert.Equal("a=1&b=-2", str);
        }

        [Fact]
        public void UrlEncoderSerializer_String()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaString, FormulaValue.New("abc"))
            });

            Assert.Equal("a=abc", str);
        }

        [Fact]
        public void UrlEncoderSerializer_Bool()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaBoolean, FormulaValue.New(true)),
                ["b"] = (SchemaBoolean, FormulaValue.New(false))
            });

            Assert.Equal("a=true&b=false", str);
        }

        [Fact]
        public void UrlEncoderSerializer_InvalidSchema()
        {
            var ex = Assert.Throws<NotImplementedException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "unknown" }, FormulaValue.New(1)),
            }));

            Assert.Equal("Not supported property type unknown for property a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_Null()
        {
            var ex = Assert.Throws<NotImplementedException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "null" }, FormulaValue.New(1)),
            }));

            Assert.Equal("null schema type not supported yet for property a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_Array_Integer()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayInteger, GetArray(1, 2))
            });

            Assert.Equal("a=1&a=2", str);
        }

        [Fact]
        public void UrlEncoderSerializer_Array_Empty()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayInteger, GetArray(Array.Empty<int>()))
            });

            Assert.Equal("a=", str);
        }

        [Fact]
        public void UrlEncoderSerializer_Array_Blank()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayInteger, GetArray(GetRecord(("Value", FormulaValue.NewBlank()))))
            });

            Assert.Equal("a=", str);
        }

        [Fact]
        public void UrlEncoderSerializer_Array_String()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["z"] = (SchemaArrayInteger, GetArray("a", "b"))
            });

            Assert.Equal("z=a&z=b", str);
        }

        [Fact]
        public void UrlEncoderSerializer_Array_Boolean()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayString, GetArray(true, false))
            });

            Assert.Equal("a=true&a=false", str);
        }

        [Fact]
        public void UrlEncoderSerializer_Array_DateTime()
        {
            var dt1 = new DateTime(1904, 11, 4, 2, 35, 33, 981);
            var dt2 = new DateTime(2022, 6, 22, 17, 5, 11, 117);

            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["A"] = (SchemaArrayDateTime, GetArray(dt1, dt2))
            });

            var dates = new Regex(@"A=(?<dt>[^&]+)").Matches(str).Cast<Match>().Select(m => DateTime.Parse(HttpUtility.UrlDecode(m.Groups["dt"].Value))).ToArray();

            Assert.Collection(
                dates, 
                d1 => Assert.Equal(dt1, d1), 
                d2 => Assert.Equal(dt2, d2));
        }

        [Fact]
        public void UrlEncoderSerializer_Array_Record_Invalid()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayObject, GetArray(GetRecord(("x", FormulaValue.New(1)))))
            }));

            Assert.Equal("Incompatible Table for supporting array, RecordValue doesn't have 'Value' column - propertyName a", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_Array_Record_Invalid2()
        {
            var ex = Assert.Throws<NotImplementedException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayObject, GetArray(GetRecord(("Value", GetRecord(("z", FormulaValue.New(2)))))))
            }));

            Assert.Equal("Not supported type Microsoft.PowerFx.Types.InMemoryRecordValue for value", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_Array_Invalid()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayInteger, GetTable(GetRecord(("a", FormulaValue.New(1)), ("b", FormulaValue.New("foo")))))
            }));

            Assert.Equal("Incompatible Table for supporting array, RecordValue has more than one column - propertyName a, number of fields 2", ex.Message);
        }

        [Fact]
        public void UrlEncoderSerializer_Object()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaObject(("x", SchemaInteger), ("y", SchemaString)), GetRecord(("x", FormulaValue.New(1)), ("y", FormulaValue.New("foo"))))
            });

            Assert.Equal("a.x=1&a.y=foo", str);
        }

        [Fact]
        public void UrlEncoderSerializer_ComplexObject()
        {
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaObject(
                            ("x", SchemaInteger), 
                            ("y", SchemaString), 
                            ("z", SchemaObject(("a", SchemaInteger)))),
                         GetRecord(
                             ("x", FormulaValue.New(1)), 
                             ("y", FormulaValue.New("foo")), 
                             ("z", GetRecord(("a", FormulaValue.New(-1))))))
            });

            Assert.Equal("a.x=1&a.y=foo&z.a.a=-1", str);
        }

        [Fact]
        public void UrlEncoderSerializer_Object_MissingObjectProperty()
        {
            var ex = Assert.Throws<NotImplementedException>(() => SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaObject(("x", SchemaInteger), ("y", SchemaString)), GetRecord(("x", FormulaValue.New(1))))
            }));

            Assert.Equal("Missing property y, object is too complex or not supported", ex.Message);
        }

        [Theory]
        [InlineData("2022-06-21T14:36:59.9353993+02:00")]
        [InlineData("2022-06-21T14:36:59.9353993-08:00")]
        public void UrlEncoderSerializer_Date(string dateString)
        {
            var date = DateTime.Parse(dateString);
            var str = SerializeUrlEncoder(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["A"] = (SchemaDateTime, FormulaValue.New(date))
            });
            
            var dateStr = str.Substring(2);
            Assert.Equal(date, DateTime.Parse(HttpUtility.UrlDecode(dateStr)));            
        }       
    }
}
