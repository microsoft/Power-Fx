﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class RefreshTests
    {
        [Fact]
        public void RefreshTest_InMemoryTable()
        {
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);                        
            RecalcEngine engine = new RecalcEngine(config);

            config.EnableSetFunction();
            engine.UpdateVariable("t", TableValue.NewTable(RecordType.Empty()));            
            FormulaValue result = engine.Eval("Set(t, Table()); Refresh(t)", null, new ParserOptions { AllowsSideEffects = true });

            // Refresh function returns nothing, just check it's not an error
            Assert.True(result is BlankValue);
        }

        [Fact]
        public void RefreshTest_RefreshableTable()
        {
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);
            TestTableValue ttv = new TestTableValue(RecordType.Empty());
            
            engine.UpdateVariable("t", ttv);
            FormulaValue result = engine.Eval("Refresh(t)", null, new ParserOptions { AllowsSideEffects = true });

            // Validate no error + TableValue has been refreshed
            Assert.True(result is BlankValue);
            Assert.Equal(1, ttv.RefreshCount);
        }

        [Fact]
        public void RefreshTest_RefreshableTable2()
        {
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);
            TestTableValue ttv = new TestTableValue(RecordType.Empty());

            engine.UpdateVariable("t", ttv);
            FormulaValue result = engine.Eval("With({ before: First(t).RefreshCount }, Refresh(t); before & First(t).RefreshCount;", null, new ParserOptions { AllowsSideEffects = true });

            // Validate no error + TableValue has been refreshed
            Assert.True(result is BlankValue);
            Assert.Equal(1, ttv.RefreshCount);
        }

        public class TestTableValue : TableValue, IRefreshable
        {
            public TestTableValue(RecordType recordType)
                : base(recordType)
            {
            }

            public int RefreshCount = 0;

            public override IEnumerable<DValue<RecordValue>> Rows => new DValue<RecordValue>[]
            {
                DValue<RecordValue>.Of(RecordValue.NewRecordFromFields(new NamedValue("RefreshCount", FormulaValue.New(RefreshCount))))
            };

            public void Refresh()
            {
                RefreshCount++;
            }
        }
    }
}
