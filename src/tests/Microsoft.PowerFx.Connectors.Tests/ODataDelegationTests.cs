// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class ODataDelegationTests : PowerFxTest
    {
        [Fact]
        public void Foo()
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);

            TableValue table = new TestODataTableValue();
            engine.UpdateVariable("Table", table);

            var odataTable = engine.Eval("Filter(Table, x > 10)") as ODataQueryableTableValue;
            Assert.NotNull(odataTable);
            Assert.Equal(new Uri("https://contoso.com/api/list?$filter=x+gt+10"), odataTable.GetUri());
        }
    }

    public sealed class TestODataTableValue : ODataQueryableTableValue
    {
        public TestODataTableValue()
            : base(TableType.Empty().Add("x", FormulaType.Number), new Uri("https://contoso.com/api/list"))
        {
        }

        private TestODataTableValue(NameValueCollection odataParams)
            : base(TableType.Empty().Add("x", FormulaType.Number), new Uri("https://contoso.com/api/list"), odataParams)
        {
        }

        protected override ODataQueryableTableValue WithQuery(NameValueCollection odataParamsNew)
        {
            return new TestODataTableValue(odataParamsNew);
        }

        protected override Task<List<DValue<RecordValue>>> GetRowsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
