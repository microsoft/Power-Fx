// Copyright (c) Microsoft Corporation.
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
            Assert.True(ttv.HasBeenRefreshed);
        }

        public class TestTableValue : TableValue, IRefreshable
        {
            public TestTableValue(RecordType recordType) 
                : base(recordType)
            {
            }

            public bool HasBeenRefreshed = false;

            public override IEnumerable<DValue<RecordValue>> Rows => Enumerable.Empty<DValue<RecordValue>>();

            public void Refresh()
            {
                HasBeenRefreshed = true;
            }
        }
    }
}
