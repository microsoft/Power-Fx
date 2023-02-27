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

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, new NumberType());
            Assert.NotNull(fv2);
            Assert.True(fv2 is NumberValue);
            Assert.Equal(17, ((NumberValue)nv).Value);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new StringType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new BooleanType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new GuidType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new ColorType()));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);

            Assert.Equal(17d, ((UntypedObjectValue)fv3).Impl.GetDouble());

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

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, new StringType());
            Assert.NotNull(fv2);
            Assert.True(fv2 is StringValue);
            Assert.Equal("abc", ((StringValue)sv).Value);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new NumberType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new BooleanType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new GuidType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new ColorType()));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);

            Assert.Equal("abc", ((UntypedObjectValue)fv3).Impl.GetString());

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

            foreach (FormulaType ft in new FormulaType[] { TableType.Empty() , RecordType.Empty(), new StringType(), new NumberType(), new BooleanType(), new GuidType(), new ColorType() })
            {
                FormulaValue fv5 = FormulaValueJSON.FromJson(expr, ft);
                Assert.NotNull(fv5);
                Assert.True(fv5 is BlankValue);
            }

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);
            Assert.Null(((UntypedObjectValue)fv3).Impl.GetString());
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

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, new BooleanType());
            Assert.NotNull(fv2);
            Assert.True(fv2 is BooleanValue);
            Assert.Equal(expectedBoolean, ((BooleanValue)bv).Value);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));            
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new NumberType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new StringType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new GuidType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new ColorType()));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);

            Assert.Equal(expectedBoolean, ((UntypedObjectValue)fv3).Impl.GetBoolean());

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
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new NumberType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new StringType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new BooleanType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new GuidType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new ColorType()));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);

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
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new NumberType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new StringType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new BooleanType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new GuidType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new ColorType()));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            UntypedObjectValue uo = (UntypedObjectValue)fv3;
            Assert.NotNull(uo.Impl);

            Assert.True(uo.Impl.TryGetProperty("a", out IUntypedObject a));
            Assert.True(a.GetBoolean());
            Assert.True(uo.Impl.TryGetProperty("b", out IUntypedObject b));
            Assert.Equal("str", b.GetString());
            Assert.True(uo.Impl.TryGetProperty("c", out IUntypedObject c));
            Assert.Equal(17.5d, c.GetDouble());
            Assert.True(uo.Impl.TryGetProperty("d", out IUntypedObject d));
            Assert.Null(d.GetString());
            Assert.True(uo.Impl.TryGetProperty("e", out IUntypedObject e));
            Assert.Equal(2, e.GetArrayLength());
            Assert.Equal(1d, e[0].GetDouble());
            Assert.Equal(2d, e[1].GetDouble());
            Assert.True(uo.Impl.TryGetProperty("f", out IUntypedObject f));
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
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new NumberType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new StringType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new BooleanType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new GuidType()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, new ColorType()));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is TableValue);
        }
    }
}
