// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Json.Tests
{
    public class ParseJSONTests
    {
        // This demos how to enable ParseJSON function.
        [Fact]
        public void BasicParseJson()
        {
            var config = new PowerFxConfig();
            config.EnableParseJSONFunction();
            var engine = new RecalcEngine(config);
            var result = engine.Eval("Value(ParseJSON(\"5\"))");
            Assert.Equal(5d, result.ToObject());
        }

        [Fact]
        public void ParseJsonNumber()
        {
            string expr = "17";
            FormulaValue fv = FormulaValueJSON.FromJson(expr);
            Assert.NotNull(fv);
            Assert.True(fv is NumberValue);
            
            NumberValue nv = (NumberValue)fv;
            Assert.Equal(17, nv.Value);
            Assert.Equal("n", nv.Type.ToStringWithDisplayNames());

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, FormulaType.Number);
            Assert.NotNull(fv2);
            Assert.True(fv2 is NumberValue);
            Assert.Equal(17, ((NumberValue)nv).Value);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Implementation);

            Assert.Equal(17d, ((UntypedObjectValue)fv3).Implementation.GetDouble());

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is NumberValue);
            Assert.Equal(17d, ((NumberValue)fv4).Value);
        }

        [Fact]
        public void ParseJsonString()
        {
            string expr = @"""abc""";
            FormulaValue fv = FormulaValueJSON.FromJson(expr);
            Assert.NotNull(fv);
            Assert.True(fv is StringValue);

            StringValue sv = (StringValue)fv;
            Assert.Equal("abc", sv.Value);
            Assert.Equal("s", sv.Type.ToStringWithDisplayNames());

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, FormulaType.String);
            Assert.NotNull(fv2);
            Assert.True(fv2 is StringValue);
            Assert.Equal("abc", ((StringValue)sv).Value);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Implementation);

            Assert.Equal("abc", ((UntypedObjectValue)fv3).Implementation.GetString());

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is StringValue);
            Assert.Equal("abc", ((StringValue)fv4).Value);
        }

        [Fact]
        public void ParseJsonNull()
        {
            string expr = @"null";
            FormulaValue fv = FormulaValueJSON.FromJson(expr);
            Assert.NotNull(fv);
            Assert.True(fv is BlankValue);

            BlankValue bv = (BlankValue)fv;            
            Assert.Equal("N", bv.Type.ToStringWithDisplayNames());

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv2);
            Assert.True(fv2 is BlankValue);

            foreach (FormulaType ft in new FormulaType[] { TableType.Empty(), RecordType.Empty(), FormulaType.String, FormulaType.Number, FormulaType.Boolean, FormulaType.Guid, FormulaType.Color })
            {
                FormulaValue fv5 = FormulaValueJSON.FromJson(expr, ft);
                Assert.NotNull(fv5);
                Assert.True(fv5 is BlankValue);
            }

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Implementation);
            Assert.Null(((UntypedObjectValue)fv3).Implementation.GetString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ParseJsonBoolean(bool expectedBoolean)
        {
            // True, with upper case T, isn't a valid Json string
            string expr = expectedBoolean.ToString().ToLowerInvariant();
            FormulaValue fv = FormulaValueJSON.FromJson(expr);
            Assert.NotNull(fv);
            Assert.True(fv is BooleanValue);

            BooleanValue bv = (BooleanValue)fv;
            Assert.Equal(expectedBoolean, bv.Value);
            Assert.Equal("b", bv.Type.ToStringWithDisplayNames());

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, FormulaType.Boolean);
            Assert.NotNull(fv2);
            Assert.True(fv2 is BooleanValue);
            Assert.Equal(expectedBoolean, ((BooleanValue)bv).Value);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));            
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Implementation);

            Assert.Equal(expectedBoolean, ((UntypedObjectValue)fv3).Implementation.GetBoolean());

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is BooleanValue);
            Assert.Equal(expectedBoolean, ((BooleanValue)fv4).Value);
        }       

        [Fact]
        public void ParseJsonEmptyRecord()
        {
            string expr = "{}";
            FormulaValue fv = FormulaValueJSON.FromJson(expr);
            Assert.NotNull(fv);
            Assert.True(fv is RecordValue);

            RecordValue rv = (RecordValue)fv;
            Assert.Empty(rv.Fields);
            Assert.Equal("![]", rv.Type.ToStringWithDisplayNames());            

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, rv.Type);
            Assert.NotNull(fv2);
            Assert.True(fv2 is RecordValue);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Implementation);

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is RecordValue);
        }

        [Fact]
        public void ParseJsonRecord()
        {
            string expr = @"{""a"": true, ""b"": ""str"", ""c"": 17.5, ""d"": null, ""e"": [ 1, 2 ], ""f"": { ""g"": 7 } }";
            FormulaValue fv = FormulaValueJSON.FromJson(expr);
            Assert.NotNull(fv);
            Assert.True(fv is RecordValue);

            RecordValue rv = (RecordValue)fv;
            Assert.Equal(6, rv.Fields.Count());
            Assert.Equal("![a:b, b:s, c:n, d:N, e:*[Value:n], f:![g:n]]", rv.Type.ToStringWithDisplayNames());
            Assert.True(rv.GetField("a") is BooleanValue);  
            Assert.True(((BooleanValue)rv.GetField("a")).Value);
            Assert.True(rv.GetField("b") is StringValue);
            Assert.Equal("str", ((StringValue)rv.GetField("b")).Value);
            Assert.True(rv.GetField("c") is NumberValue);
            Assert.Equal(17.5d, ((NumberValue)rv.GetField("c")).Value);
            Assert.True(rv.GetField("d") is BlankValue);
            Assert.True(rv.GetField("e") is TableValue);
            TableValue array = (TableValue)rv.GetField("e");
            Assert.Equal("*[Value:n]", array.Type.ToStringWithDisplayNames());
            Assert.Equal(1, ((NumberValue)array.Index(1).Value.GetField("Value")).Value);
            Assert.Equal(2, ((NumberValue)array.Index(2).Value.GetField("Value")).Value);
            Assert.True(rv.GetField("f") is RecordValue);
            RecordValue innerRecord = (RecordValue)rv.GetField("f");
            Assert.True(innerRecord.GetField("g") is NumberValue);
            Assert.Equal(7, ((NumberValue)innerRecord.GetField("g")).Value);

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, rv.Type);
            Assert.NotNull(fv2);
            Assert.True(fv2 is RecordValue);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            UntypedObjectValue uo = (UntypedObjectValue)fv3;
            Assert.NotNull(uo.Implementation);

            Assert.True(uo.Implementation.TryGetProperty("a", out IUntypedObject a));
            Assert.True(a.GetBoolean());
            Assert.True(uo.Implementation.TryGetProperty("b", out IUntypedObject b));
            Assert.Equal("str", b.GetString());
            Assert.True(uo.Implementation.TryGetProperty("c", out IUntypedObject c));
            Assert.Equal(17.5d, c.GetDouble());
            Assert.True(uo.Implementation.TryGetProperty("d", out IUntypedObject d));
            Assert.Null(d.GetString());
            Assert.True(uo.Implementation.TryGetProperty("e", out IUntypedObject e));
            Assert.Equal(2, e.GetArrayLength());
            Assert.Equal(1d, e.Index(0).GetDouble());
            Assert.Equal(2d, e.Index(1).GetDouble());
            Assert.True(uo.Implementation.TryGetProperty("f", out IUntypedObject f));
            Assert.True(f.TryGetProperty("g", out IUntypedObject g));
            Assert.Equal(7, g.GetDouble());

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is RecordValue);
            
            rv = (RecordValue)fv4;
            Assert.Equal(6, rv.Fields.Count());
            Assert.Equal("![a:b, b:s, c:n, d:N, e:*[Value:n], f:![g:n]]", rv.Type.ToStringWithDisplayNames());            
        }

        [Fact]
        public void ParseJsonEmptyArray()
        {
            string expr = "[]";
            FormulaValue fv = FormulaValueJSON.FromJson(expr);
            Assert.NotNull(fv);
            Assert.True(fv is TableValue);

            TableValue tv = (TableValue)fv;
            Assert.False(tv.IsColumn);
            Assert.Equal("*[]", tv.Type.ToStringWithDisplayNames());
            Assert.Empty(tv.Rows);

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, tv.Type);
            Assert.NotNull(fv2);
            Assert.True(fv2 is TableValue);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Implementation);

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is TableValue);
        }
    }

    public static class UoExtensions
    {
        public static double GetDouble(this IUntypedObject uo)
        {            
            if (uo is SupportsFxValue fxValue && fxValue.Type == FormulaType.Number)
            {
                return ((NumberValue)fxValue.Value).Value;
            }

            throw new Exception("Fail");
        }

        public static string GetString(this IUntypedObject uo)
        {
            if (uo is SupportsFxValue fxValue)
            {
                if (fxValue.Type == FormulaType.Blank)
                {
                    return null;
                }

                if (fxValue.Type == FormulaType.String)
                {
                    return ((StringValue)fxValue.Value).Value;
                }
            }

            throw new Exception("Fail");
        }

        public static bool GetBoolean(this IUntypedObject uo)
        {
            if (uo is SupportsFxValue fxValue && fxValue.Type == FormulaType.Boolean)
            {
                return ((BooleanValue)fxValue.Value).Value;
            }

            throw new Exception("Fail");
        }

        public static bool TryGetProperty(this IUntypedObject uo, string propertyName, out IUntypedObject result)
        {
            if (uo is ISupportsProperties record)
            {
                return record.TryGetProperty(propertyName, out result);
            }

            throw new Exception("Fail");
        }

        public static int GetArrayLength(this IUntypedObject uo)
        {
            if (uo is ISupportsArray array)
            {
                return array.Length;
            }

            throw new Exception("Fail");
        }

        public static IUntypedObject Index(this IUntypedObject uo, int index)
        {
            if (uo is ISupportsArray array)
            {
                return array[index];
            }

            throw new Exception("Fail");
        }
    }
}
