// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.AssociatedDataSourcesTests
{
    public class TestUpdateDataQuerySelects
    {
        [Theory]
        [InlineData("SortByColumns(Accounts, 'Account Name', SortOrder.Ascending)", "accountid,name")]
        [InlineData("SortByColumns(Accounts, 'Account Name', SortOrder.Ascending, 'Address 1: City')", "accountid,name,address1_city")]
        [InlineData("SortByColumns(Accounts, 'Account Name', SortOrder.Descending, 'Address 1: Street 1')", "accountid,name,address1_line1")]
        [InlineData("SortByColumns(Accounts, name, SortOrder.Descending, address1_line1)", "accountid,name,address1_line1")]
        [InlineData("ShowColumns(Accounts, 'Account Name', 'Address 1: City')", "accountid,name,address1_city")]
        [InlineData("RenameColumns(Accounts, 'Account Name', 'The name', 'Address 1: City', 'The city')", "accountid,name,address1_city")]
        [InlineData("Search(Accounts, \"something to search\", 'Account Name', address1_line1, 'Address 1: City')", "accountid,name,address1_city,address1_line1")]
        public void TestSelects(string expression, string expectedSelects)
        {
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddEntity(new AccountsEntity());

            var config = new PowerFxConfig()
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(expression);
            Assert.True(result.IsSuccess);

            var callNode = result.Binding.Top.AsCall();
            Assert.NotNull(callNode);

            var callInfo = result.Binding.GetInfo(callNode);
            var dataSourceToQueryOptionsMap = new DataSourceToQueryOptionsMap();
            callInfo.Function.UpdateDataQuerySelects(callNode, result.Binding, dataSourceToQueryOptionsMap);
            var queryOptions = dataSourceToQueryOptionsMap.GetQueryOptions();
            var actualSelectsList = new List<string>();
            foreach (var queryOption in queryOptions)
            {
                actualSelectsList.AddRange(queryOption.Selects);
            }

            var expectedSelectsList = expectedSelects.Split(",").ToList();
            Assert.Equal(expectedSelectsList.Count, actualSelectsList.Count);
            foreach (var expectedSelect in expectedSelectsList)
            {
                Assert.Contains(expectedSelect, actualSelectsList);
            }
        }
    }
}
