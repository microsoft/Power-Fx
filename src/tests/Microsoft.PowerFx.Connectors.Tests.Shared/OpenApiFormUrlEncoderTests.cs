// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        public async Task UrlEncoderSerializer_Empty()
        {
            var str = await SerializeUrlEncoderAsync(null);
            Assert.Equal(string.Empty, str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_SingleInteger()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaInteger, FormulaValue.New(1))
            });

            Assert.Equal("a=1", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Number()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaNumber, FormulaValue.New(1.17e-4))
            });

            Assert.Equal("a=0.000117", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_EscapedKey()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a\\b\"c"] = (SchemaInteger, FormulaValue.New(1))
            });

            Assert.Equal("a%5cb%22c=1", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_SingleInteger_NoValue()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaInteger, null)
            }));

            Assert.Equal("Expected NumberValue (integer) and got <null> value, for property a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_SingleInteger_NoSchema()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (null, null)
            }));

            Assert.Equal("Missing schema for property a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Integer_String_Mismatch()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaInteger, FormulaValue.New("abc"))
            }));

            Assert.Equal("Expected NumberValue (integer) and got StringValue value, for property a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Number_String_Mismatch()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaNumber, FormulaValue.New("abc"))
            }));

            Assert.Equal("Expected NumberValue (number) and got StringValue value, for property a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_String_Integer_Mismatch()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaString, FormulaValue.New(11))
            }));

            Assert.Equal("Expected StringValue and got DecimalValue value, for property a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Bool_String_Mismatch()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaBoolean, FormulaValue.New("abc"))
            }));

            Assert.Equal("Expected BooleanValue and got StringValue value, for property a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_TwoIntegers()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaInteger, FormulaValue.New(1)),
                ["b"] = (SchemaInteger, FormulaValue.New(-2))
            });

            Assert.Equal("a=1&b=-2", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_String()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaString, FormulaValue.New("abc"))
            });

            Assert.Equal("a=abc", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Bool()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaBoolean, FormulaValue.New(true)),
                ["b"] = (SchemaBoolean, FormulaValue.New(false))
            });

            Assert.Equal("a=true&b=false", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_InvalidSchema()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "unknown" }, FormulaValue.New(1)),
            }));

            Assert.Equal("Not supported property type unknown for property a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Null()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "null" }, FormulaValue.New(1)),
            }));

            Assert.Equal("null schema type not supported yet for property a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_Integer()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayInteger, GetArray(1, 2))
            });

            Assert.Equal("a=1&a=2", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_Empty()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayInteger, GetArray(Array.Empty<int>()))
            });

            Assert.Equal("a=", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_Blank()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayInteger, GetArray(GetRecord(("Value", FormulaValue.NewBlank()))))
            });

            Assert.Equal("a=", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_String()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["z"] = (SchemaArrayInteger, GetArray("a", "b"))
            });

            Assert.Equal("z=a&z=b", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_Boolean()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayString, GetArray(true, false))
            });

            Assert.Equal("a=true&a=false", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_DateTime()
        {
            var dt1 = new DateTime(1904, 11, 4, 2, 35, 33, 981, DateTimeKind.Local);
            var dt2 = new DateTime(2022, 6, 22, 17, 5, 11, 117, DateTimeKind.Local);

            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["A"] = (SchemaArrayDateTime, GetArray(dt1, dt2))
            });

            var dates = new Regex(@"A=(?<dt>[^&]+)").Matches(str).Cast<Match>().Select(m => DateTime.Parse(HttpUtility.UrlDecode(m.Groups["dt"].Value))).ToArray();

            Assert.Collection(
                dates, 
                d1 => Assert.Equal(dt1, d1.ToUniversalTime()), 
                d2 => Assert.Equal(dt2, d2.ToUniversalTime()));
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_Record_Invalid()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayObject, GetArray(GetRecord(("x", FormulaValue.New(1)))))
            }));

            Assert.Equal("Incompatible Table for supporting array, RecordValue doesn't have 'Value' column - propertyName a", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_Record_Invalid2()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayObject, GetArray(GetRecord(("Value", GetRecord(("z", FormulaValue.New(2)))))))
            }));

            Assert.Equal("Not supported type Microsoft.PowerFx.Types.InMemoryRecordValue for value", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Array_Invalid()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaArrayInteger, GetTable(GetRecord(("a", FormulaValue.New(1)), ("b", FormulaValue.New("foo")))))
            }));

            Assert.Equal("Incompatible Table for supporting array, RecordValue has more than one column - propertyName a, number of fields 2", ex.Message);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Object()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaObject(("x", SchemaInteger, false), ("y", SchemaString, false)), GetRecord(("x", FormulaValue.New(1)), ("y", FormulaValue.New("foo"))))
            });

            Assert.Equal("a.x=1&a.y=foo", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_ComplexObject()
        {
            var str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaObject(
                            ("x", SchemaInteger, false), 
                            ("y", SchemaString, false), 
                            ("z", SchemaObject(("a", SchemaInteger, false)), false)),
                         GetRecord(
                             ("x", FormulaValue.New(1)), 
                             ("y", FormulaValue.New("foo")), 
                             ("z", GetRecord(("a", FormulaValue.New(-1))))))
            });

            Assert.Equal("a.x=1&a.y=foo&z.a.a=-1", str);
        }

        [Fact]
        public async Task UrlEncoderSerializer_Object_MissingObjectProperty()
        {
            var ex = await Assert.ThrowsAsync<PowerFxConnectorException>(async () => await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (SchemaObject(("x", SchemaInteger, true), ("y", SchemaString, true)), GetRecord(("x", FormulaValue.New(1))))
            }));

            Assert.Equal("Missing property y, object is too complex or not supported", ex.Message);
        }

        [Theory]
        [InlineData("2022-06-21T14:36:59.9353993+02:00")]
        [InlineData("2022-06-21T14:36:59.9353993-08:00")]
        public async Task UrlEncoderSerializer_Date(string dateString)
        {
            DateTime date = DateTime.Parse(dateString);
            RuntimeConfig rtConfig = new RuntimeConfig();
            rtConfig.SetTimeZone(TimeZoneInfo.Local);            
            string str = await SerializeUrlEncoderAsync(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>() { ["A"] = (SchemaDateTime, FormulaValue.New(date)) }, new ConvertToUTC(TimeZoneInfo.Local));
            
            string dateStr = str.Substring(2);            
            date = date.AddTicks(-(date.Ticks % 10000));
            Assert.Equal(date, DateTime.Parse(HttpUtility.UrlDecode(dateStr)));            
        }       
    }
}
