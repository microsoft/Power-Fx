﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
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
            PowerFxConfig config = new PowerFxConfig();                        
            RecalcEngine engine = new RecalcEngine(config);

            config.EnableSetFunction();
            engine.UpdateVariable("t", TableValue.NewTable(RecordType.Empty()));            
            FormulaValue result = engine.Eval("Refresh(t)", null, new ParserOptions { AllowsSideEffects = true });
            
            Assert.True(result is ErrorValue);
            ErrorValue ev = (ErrorValue)result;

            Assert.Equal("Only managed connections can be refreshed.", ev.Errors[0].Message);
        }

        [Fact]
        public void RefreshTest_RefreshableTable()
        {
            PowerFxConfig config = new PowerFxConfig();
            RecalcEngine engine = new RecalcEngine(config);
            TestTableValue ttv = new TestTableValue(RecordType.Empty());
            
            engine.UpdateVariable("t", ttv);
            FormulaValue result = engine.Eval("Refresh(t)", null, new ParserOptions { AllowsSideEffects = true });

            // Validate no error + TableValue has been refreshed
            Assert.True(result is BooleanValue);
            Assert.True(((BooleanValue)result).Value);
            Assert.Equal(1, ttv.RefreshCount);
        }

        [Fact]
        public void RefreshTest_RefreshableTable2()
        {
            PowerFxConfig config = new PowerFxConfig();
            RecalcEngine engine = new RecalcEngine(config);
            TestTableValue ttv = new TestTableValue(RecordType.Empty().Add(new NamedFormulaType("RefreshCount", FormulaType.Decimal)));

            engine.UpdateVariable("t", ttv);
            FormulaValue result = engine.Eval("With({ before: First(t).RefreshCount }, Refresh(t); before & First(t).RefreshCount)", null, new ParserOptions { AllowsSideEffects = true });
                        
            Assert.Equal(1, ttv.RefreshCount);
            Assert.Equal("01", result.ToObject());
        }

        public sealed class TestTableValue : TableValue, IRefreshable
        {
            public TestTableValue(RecordType recordType)
                : base(recordType)
            {
            }

            public override bool TryShallowCopy(out FormulaValue copy)
            {
                copy = this;
                return true;
            }

            public override bool CanShallowCopy => true;

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
