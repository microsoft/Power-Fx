// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;

using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class MergeRecordTests
    {
        [Fact]
        public void MergeRecords_Null()
        {            
            RecordValue rv = new RecalcEngine().Eval("{}") as RecordValue;
            Assert.NotNull(rv);

            Assert.Throws<ArgumentNullException>(() => ArgumentMapper.MergeRecords(null, rv));
            Assert.Throws<ArgumentNullException>(() => ArgumentMapper.MergeRecords(rv, null));            
        }

        [Fact]
        public void MergeRecords_Empty()
        {
            RecordValue rv = new RecalcEngine().Eval("{}") as RecordValue;
            Assert.NotNull(rv);

            RecordValue rv2 = ArgumentMapper.MergeRecords(rv, rv);

            Assert.NotNull(rv2);
            Assert.Equal("{}", System.Text.Json.JsonSerializer.Serialize(rv2.ToObject()));
        }

        [Fact]
        public void MergeRecords_Idempotent()
        {
            RecalcEngine engine = new RecalcEngine();
            RecordValue[] rvs = new RecordValue[] 
            {
                engine.Eval("{}") as RecordValue,
                engine.Eval("{a:1}") as RecordValue,
                engine.Eval(@"{a:""x""}") as RecordValue,
                engine.Eval(@"{a:""x"", b:4.3}") as RecordValue,
                engine.Eval(@"{a:""x"", h:4.3, c:{}}") as RecordValue,
                engine.Eval(@"{a:""x"", z:{d: 4}}") as RecordValue,
                engine.Eval(@"{a:""x"", c:[4, 5], u:[], x:{}, w:Blank()}") as RecordValue
            };

            foreach (RecordValue rv1 in rvs)
            {
                Assert.NotNull(rv1);
                RecordValue rv2 = ArgumentMapper.MergeRecords(rv1, rv1);

                Assert.NotNull(rv2);
                Assert.Equal(System.Text.Json.JsonSerializer.Serialize(rv1.ToObject()), System.Text.Json.JsonSerializer.Serialize(rv2.ToObject()));
            }
        }

        [Fact]
        public void MergeRecords_NoConflict_Simple()
        {
            RecalcEngine engine = new RecalcEngine();
            RecordValue rv1 = engine.Eval("{a:1}") as RecordValue;
            RecordValue rv2 = engine.Eval(@"{b:""x""}") as RecordValue;
            RecordValue rv3;
            
            rv3 = ArgumentMapper.MergeRecords(rv1, rv2);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":1,""b"":""x""}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));

            rv3 = ArgumentMapper.MergeRecords(rv2, rv1);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":1,""b"":""x""}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));
        }

        [Fact]
        public void MergeRecords_SimpleConflict() 
        {
            RecalcEngine engine = new RecalcEngine();
            RecordValue rv1 = engine.Eval("{a:1}") as RecordValue;
            RecordValue rv2 = engine.Eval("{a:2}") as RecordValue;
            RecordValue rv3;

            rv3 = ArgumentMapper.MergeRecords(rv1, rv2);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":2}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));

            rv3 = ArgumentMapper.MergeRecords(rv2, rv1);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":1}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));
        }

        [Fact]
        public void MergeRecords_Invalid()
        {
            RecalcEngine engine = new RecalcEngine();
            RecordValue rv1 = engine.Eval("{a:1}") as RecordValue;
            RecordValue rv2 = engine.Eval(@"{a:""x""}") as RecordValue;            

            var ex = Assert.Throws<ArgumentException>(() => ArgumentMapper.MergeRecords(rv1, rv2));
            Assert.Equal("Cannot merge a of type NumberValue with a of type StringValue", ex.Message);
        }

        [Fact]
        public void MergeRecords_Complex()
        {
            RecalcEngine engine = new RecalcEngine();
            RecordValue rv1 = engine.Eval("{a:{a: 4}}") as RecordValue;
            RecordValue rv2 = engine.Eval("{a:{b: 5}}") as RecordValue;
            RecordValue rv3;

            rv3 = ArgumentMapper.MergeRecords(rv1, rv2);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":{""a"":4,""b"":5}}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));

            rv3 = ArgumentMapper.MergeRecords(rv2, rv1);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":{""a"":4,""b"":5}}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));
        }

        [Fact]
        public void MergeRecords_Complex2()
        {
            RecalcEngine engine = new RecalcEngine();
            RecordValue rv1 = engine.Eval("{a:{b: 4}}") as RecordValue;
            RecordValue rv2 = engine.Eval("{b:{a: 5}}") as RecordValue;
            RecordValue rv3;

            rv3 = ArgumentMapper.MergeRecords(rv1, rv2);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":{""b"":4},""b"":{""a"":5}}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));

            rv3 = ArgumentMapper.MergeRecords(rv2, rv1);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":{""b"":4},""b"":{""a"":5}}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));
        }

        [Fact]
        public void MergeRecords_Complex3()
        {
            RecalcEngine engine = new RecalcEngine();
            RecordValue rv1 = engine.Eval("{a:{c: 4}, b:{d: 3}}") as RecordValue;
            RecordValue rv2 = engine.Eval("{b:{a: 5}, a:{c: 7}}") as RecordValue;
            RecordValue rv3;

            rv3 = ArgumentMapper.MergeRecords(rv1, rv2);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":{""c"":7},""b"":{""a"":5,""d"":3}}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));

            rv3 = ArgumentMapper.MergeRecords(rv2, rv1);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":{""c"":4},""b"":{""a"":5,""d"":3}}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));
        }

        [Fact]
        public void MergeRecords_Complex4()
        {
            RecalcEngine engine = new RecalcEngine();
            RecordValue rv1 = engine.Eval("{a:{c: 4, k:{f: 1}}, b:{d: 3}}") as RecordValue;
            RecordValue rv2 = engine.Eval("{b:{a: 5}, a:{c: 7, k:{h:4, f:2}}}") as RecordValue;
            RecordValue rv3;

            rv3 = ArgumentMapper.MergeRecords(rv1, rv2);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":{""c"":7,""k"":{""f"":2,""h"":4}},""b"":{""a"":5,""d"":3}}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));

            rv3 = ArgumentMapper.MergeRecords(rv2, rv1);

            Assert.NotNull(rv3);
            Assert.Equal(@"{""a"":{""c"":4,""k"":{""f"":1,""h"":4}},""b"":{""a"":5,""d"":3}}", System.Text.Json.JsonSerializer.Serialize(rv3.ToObject()));
        }
    }
}
