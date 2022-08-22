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
        [Theory]
        [InlineData("Filter(Table, x > 10)", "https://contoso.com/api/list?$filter=x+gt+10")]
        [InlineData("Filter(Table, x > 10 + 5)", "https://contoso.com/api/list?$filter=x+gt+15")]
        [InlineData("Filter(Table, x > 2 * myNumber)", "https://contoso.com/api/list?$filter=x+gt+100")]
        [InlineData("Filter(Filter(Table, x > 10), x < 5)", "https://contoso.com/api/list?$filter=(x+gt+10)+and+(x+lt+5)")]
        [InlineData("Filter(Table, date > Date(2010, 02, 05))", "https://contoso.com/api/list?$filter=date+gt+2010-02-05")]
        [InlineData("Filter(Table, name = \"Foo\")", "https://contoso.com/api/list?$filter=name+eq+%27Foo%27")]
        [InlineData("Filter(Table, name = \"O'Neil\")", "https://contoso.com/api/list?$filter=name+eq+%27O%27%27Neil%27")]
        [InlineData("Filter(Table, !(name = \"Foo\"))", "https://contoso.com/api/list?$filter=not(name+eq+%27Foo%27)")]
        [InlineData("Filter(Table, StartsWith(name, \"Prefix\"))", "https://contoso.com/api/list?$filter=startswith(name%2c+%27Prefix%27)")]
        [InlineData("Filter(Table, EndsWith(name, \"suffix\"))", "https://contoso.com/api/list?$filter=endswith(name%2c+%27suffix%27)")]
        [InlineData("Sort(Table, date)", "https://contoso.com/api/list?$orderby=date")]
        [InlineData("Sort(Table, date, SortOrder.Descending)", "https://contoso.com/api/list?$orderby=date+desc")]
        [InlineData("FirstN(Table, 10)", "https://contoso.com/api/list?$top=10")]
        [InlineData("FirstN(Sort(Filter(Table, x > 10), date), 10)", "https://contoso.com/api/list?$filter=x+gt+10&$orderby=date&$top=10")]
        public void TestDelegation(string expression, string uriExpected)
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);
            engine.UpdateVariable("myNumber", FormulaValue.New(50));

            TableType tableType = TableType.Empty()
                .Add("x", FormulaType.Number)
                .Add("date", FormulaType.DateTime)
                .Add("name", FormulaType.String);
            TableValue table = new TestODataTableValue(tableType);
            engine.UpdateVariable("Table", table);

            var odataTable = engine.Eval(expression) as ODataQueryableTableValue;
            Assert.NotNull(odataTable);
            Assert.Equal(new Uri(uriExpected), odataTable.GetUri());
        }
    }

    public sealed class TestODataTableValue : ODataQueryableTableValue
    {
        public TestODataTableValue(TableType tableType)
            : base(tableType, new Uri("https://contoso.com/api/list"))
        {
        }

        private TestODataTableValue(TableType tableType, ODataParams odataParams)
            : base(tableType, new Uri("https://contoso.com/api/list"), odataParams)
        {
        }

        protected override ODataQueryableTableValue WithParameters(ODataParams odataParamsNew)
        {
            return new TestODataTableValue((TableType)Type, odataParamsNew);
        }

        protected override Task<List<DValue<RecordValue>>> GetRowsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
