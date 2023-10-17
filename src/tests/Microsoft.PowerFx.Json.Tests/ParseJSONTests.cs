﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
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
            config.EnableJsonFunctions();
            var engine = new RecalcEngine(config);
            var result = engine.Eval("Value(ParseJSON(\"5\"))");
            Assert.Equal(5m, result.ToObject());
        }

        [Fact]
        public void SingleColumnTableFromJson()
        {
            var tableType = RecordType.Empty().Add("Value", FormulaType.String).ToTable();
            var res = FormulaValueJSON.FromJson("[ { \"Value\": \"Seattle\"}, { \"Value\": \"Redmond\"} ]", tableType);

            var symbol = new SymbolTable();
            var slot = symbol.AddVariable("table", res.Type);
            var symValue = new SymbolValues(symbol);
            symValue.Set(slot, res);

            var config = new PowerFxConfig();
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);
            var result = engine.EvalAsync("First(table).Value", CancellationToken.None, symValue).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.Equal("Seattle", result.ToObject());
        }

        [Fact]
        public void ParseJsonNumber()
        {
            string expr = "17";
            FormulaValue fv = FormulaValueJSON.FromJson(expr);
            Assert.NotNull(fv);
            Assert.True(fv is DecimalValue);
            
            DecimalValue wv = (DecimalValue)fv;
            Assert.Equal(17m, wv.Value);
            Assert.Equal("w", wv.Type.ToStringWithDisplayNames());

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, FormulaType.Number);
            Assert.NotNull(fv2);
            Assert.True(fv2 is NumberValue);
            NumberValue nv = (NumberValue)fv2;
            Assert.Equal(17, nv.Value);
            Assert.Equal("n", nv.Type.ToStringWithDisplayNames());

            FormulaValue wv2 = FormulaValueJSON.FromJson(expr, FormulaType.Decimal);
            Assert.NotNull(wv2);
            Assert.True(wv2 is DecimalValue);
            Assert.Equal(17m, ((DecimalValue)wv2).Value);
            Assert.Equal("w", wv2.Type.ToStringWithDisplayNames());

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);

            Assert.Equal(17d, ((UntypedObjectValue)fv3).Impl.GetDouble());
            Assert.Equal(17m, ((UntypedObjectValue)fv3).Impl.GetDecimal());

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is DecimalValue);
            Assert.Equal(17m, ((DecimalValue)fv4).Value);
        }

        [Fact]
        public void ParseJsonNumber_NumberIsFloat()
        {
            string expr = "17";
            FormulaValue fv = FormulaValueJSON.FromJson(expr, numberIsFloat: true);
            Assert.NotNull(fv);
            Assert.True(fv is NumberValue);

            NumberValue wv = (NumberValue)fv;
            Assert.Equal(17, wv.Value);
            Assert.Equal("n", wv.Type.ToStringWithDisplayNames());

            // for the rest of these tests with explicit typing, numberIsFloat should have no effect,
            // the results should be the same as above
            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, FormulaType.Number, numberIsFloat: true);
            Assert.NotNull(fv2);
            Assert.True(fv2 is NumberValue);
            NumberValue nv = (NumberValue)fv2;
            Assert.Equal(17, nv.Value);
            Assert.Equal("n", nv.Type.ToStringWithDisplayNames());

            FormulaValue wv2 = FormulaValueJSON.FromJson(expr, FormulaType.Decimal, numberIsFloat: true);
            Assert.NotNull(wv2);
            Assert.True(wv2 is DecimalValue);
            Assert.Equal(17m, ((DecimalValue)wv2).Value);
            Assert.Equal("w", wv2.Type.ToStringWithDisplayNames());

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty(), numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty(), numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String, numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean, numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid, numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color, numberIsFloat: true));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType(), numberIsFloat: true);
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);

            Assert.Equal(17d, ((UntypedObjectValue)fv3).Impl.GetDouble());
            Assert.Equal(17m, ((UntypedObjectValue)fv3).Impl.GetDecimal());

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType(), numberIsFloat: true);
            Assert.NotNull(fv4);
            Assert.True(fv4 is NumberValue);
            Assert.Equal(17, ((NumberValue)fv4).Value);
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
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Decimal));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

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

            foreach (FormulaType ft in new FormulaType[] { TableType.Empty(), RecordType.Empty(), FormulaType.String, FormulaType.Number, FormulaType.Boolean, FormulaType.Guid, FormulaType.Color })
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

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, FormulaType.Boolean);
            Assert.NotNull(fv2);
            Assert.True(fv2 is BooleanValue);
            Assert.Equal(expectedBoolean, ((BooleanValue)bv).Value);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, RecordType.Empty()));            
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Decimal));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

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
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Decimal));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

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
            Assert.Equal("![a:b, b:s, c:w, d:N, e:*[Value:w], f:![g:w]]", rv.Type.ToStringWithDisplayNames());
            Assert.True(rv.GetField("a") is BooleanValue);  
            Assert.True(((BooleanValue)rv.GetField("a")).Value);
            Assert.True(rv.GetField("b") is StringValue);
            Assert.Equal("str", ((StringValue)rv.GetField("b")).Value);
            Assert.True(rv.GetField("c") is DecimalValue);
            Assert.Equal(17.5m, ((DecimalValue)rv.GetField("c")).Value);
            Assert.True(rv.GetField("d") is BlankValue);
            Assert.True(rv.GetField("e") is TableValue);
            TableValue array = (TableValue)rv.GetField("e");
            Assert.Equal("*[Value:w]", array.Type.ToStringWithDisplayNames());
            Assert.Equal(1m, ((DecimalValue)array.Index(1).Value.GetField("Value")).Value);
            Assert.Equal(2m, ((DecimalValue)array.Index(2).Value.GetField("Value")).Value);
            Assert.True(rv.GetField("f") is RecordValue);
            RecordValue innerRecord = (RecordValue)rv.GetField("f");
            Assert.True(innerRecord.GetField("g") is DecimalValue);
            Assert.Equal(7m, ((DecimalValue)innerRecord.GetField("g")).Value);

            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, rv.Type);
            Assert.NotNull(fv2);
            Assert.True(fv2 is RecordValue);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty()));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Decimal));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

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
            Assert.Equal(17.5m, c.GetDecimal());
            Assert.True(uo.Impl.TryGetProperty("d", out IUntypedObject d));
            Assert.Null(d.GetString());
            Assert.True(uo.Impl.TryGetProperty("e", out IUntypedObject e));
            Assert.Equal(2, e.GetArrayLength());
            Assert.Equal(1d, e[0].GetDouble());
            Assert.Equal(2d, e[1].GetDouble());
            Assert.Equal(1m, e[0].GetDecimal());
            Assert.Equal(2m, e[1].GetDecimal());
            Assert.True(uo.Impl.TryGetProperty("f", out IUntypedObject f));
            Assert.True(f.TryGetProperty("g", out IUntypedObject g));
            Assert.Equal(7, g.GetDouble());
            Assert.Equal(7m, g.GetDecimal());

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is RecordValue);
            
            rv = (RecordValue)fv4;
            Assert.Equal(6, rv.Fields.Count());
            Assert.Equal("![a:b, b:s, c:w, d:N, e:*[Value:w], f:![g:w]]", rv.Type.ToStringWithDisplayNames());            
        }

        [Fact]
        public void ParseJsonRecord_NumberIsFloat()
        {
            string expr = @"{""a"": true, ""b"": ""str"", ""c"": 17.5, ""d"": null, ""e"": [ 1, 2 ], ""f"": { ""g"": 7 } }";
            FormulaValue fv = FormulaValueJSON.FromJson(expr, numberIsFloat: true);
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

            // for the rest of these tests with explicit typing, numberIsFloat should have no effect,
            // the results should be the same as above
            FormulaValue fv2 = FormulaValueJSON.FromJson(expr, rv.Type, numberIsFloat: true);
            Assert.NotNull(fv2);
            Assert.True(fv2 is RecordValue);

            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, TableType.Empty(), numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Number, numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Decimal, numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String, numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean, numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid, numberIsFloat: true));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color, numberIsFloat: true));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType(), numberIsFloat: true);
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
            Assert.Equal(17.5m, c.GetDecimal());
            Assert.True(uo.Impl.TryGetProperty("d", out IUntypedObject d));
            Assert.Null(d.GetString());
            Assert.True(uo.Impl.TryGetProperty("e", out IUntypedObject e));
            Assert.Equal(2, e.GetArrayLength());
            Assert.Equal(1d, e[0].GetDouble());
            Assert.Equal(2d, e[1].GetDouble());
            Assert.Equal(1m, e[0].GetDecimal());
            Assert.Equal(2m, e[1].GetDecimal());
            Assert.True(uo.Impl.TryGetProperty("f", out IUntypedObject f));
            Assert.True(f.TryGetProperty("g", out IUntypedObject g));
            Assert.Equal(7, g.GetDouble());
            Assert.Equal(7m, g.GetDecimal());

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType(), numberIsFloat: true);
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
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Decimal));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.String));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Boolean));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Guid));
            Assert.Throws<NotImplementedException>(() => FormulaValueJSON.FromJson(expr, FormulaType.Color));

            FormulaValue fv3 = FormulaValueJSON.FromJson(expr, new UntypedObjectType());
            Assert.NotNull(fv3);
            Assert.True(fv3 is UntypedObjectValue);
            Assert.NotNull(((UntypedObjectValue)fv3).Impl);

            FormulaValue fv4 = FormulaValueJSON.FromJson(expr, new BlankType());
            Assert.NotNull(fv4);
            Assert.True(fv4 is TableValue);
        }

        [Theory]
        [InlineData("[1, 2, 3]", "*[Value:w]", 1, 2, 3)]
        [InlineData("[\"a\", \"b\", \"c\"]", "*[Value:s]", "a", "b", "c")]
        [InlineData("[true, false]", "*[Value:b]", true, false)]
        public void FromJsonHomogeneousPrimitiveArray(string json, string expectedType, params object[] expected)
        {
            var res = (TableValue)FormulaValueJSON.FromJson(json);

            Assert.Equal(expectedType, res.Type.ToStringWithDisplayNames());

            Assert.Equal(expected.Length, res.Rows.Count());

            int i = 0;
            foreach (var row in res.Rows)
            {
                if (expected[i] is int)
                {
                    Assert.Equal(expected[i++].ToString(), row.Value.GetField("Value").ToObject().ToString());
                }
                else
                {
                    Assert.Equal(expected[i++], row.Value.GetField("Value").ToObject());
                }
            }
        }

        [Fact]

        // Heterogeneous arrays are unionized, the record that didn't have the particular field will return blank.  
        public void FromJsonHeterogeneousRecordArray()
        {
            var json = "[{\"f1\" : \"str\"}, {\"f2\" : 1}]";

            var res = (TableValue)FormulaValueJSON.FromJson(json);

            Assert.Equal("*[f1:s, f2:w]", res.Type.ToStringWithDisplayNames());
            Assert.Equal(2, res.Count());

            var rows = res.Rows.GetEnumerator();
            rows.MoveNext();
            Assert.Equal("str", rows.Current.Value.GetField("f1").ToObject());
            Assert.Null(rows.Current.Value.GetField("f2").ToObject());

            rows.MoveNext();
            Assert.Null(rows.Current.Value.GetField("f1").ToObject());
            Assert.Equal(1m, rows.Current.Value.GetField("f2").ToObject());
        }

        [Theory]
        [InlineData("[1, \"2\", \"3\"]", "O", 1d, "2", "3")]
        [InlineData("[\"a\", 1, true]", "O", "a", 1, true)]
        [InlineData("[true, 0]", "O", true, 0)]
        [InlineData("[true, \"false\"]", "O", true, "false")]

        // Heterogeneous arrays are converted to Untyped object, rather than homogeneous TableValue. 
        public void FromJsonHeterogeneousPrimitiveArray(string json, string expectedType, params object[] expected)
        {
            var res = (UntypedObjectValue)FormulaValueJSON.FromJson(json);
            Assert.Equal(expectedType, res.Type.ToStringWithDisplayNames());
            Assert.NotNull(expected);

            var expectedLength = expected.Length;
            Assert.Equal(expectedLength, res.Impl.GetArrayLength());

            var array = res.Impl;

            for (var i = 0; i < expectedLength; i++)
            {
                switch (expected[i])
                {
                    case double:
                        Assert.Equal(expected[i], array[i].GetDouble());
                        Assert.Equal(expected[i], (double)array[i].GetDecimal());
                        break;
                    case string:
                        Assert.Equal(expected[i], array[i].GetString());
                        break;
                    case bool:
                        Assert.Equal(expected[i], array[i].GetBoolean());
                        break;
                }
            }
        }
    }
}
