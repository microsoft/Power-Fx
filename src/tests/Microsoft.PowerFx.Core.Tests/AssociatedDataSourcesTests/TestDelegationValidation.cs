// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.AssociatedDataSourcesTests
{
    public class TestDelegationValidation
    {
        [Theory]
        [InlineData("SortByColumns(Accounts, 'Account Name', SortOrder.Ascending)", true)]
        [InlineData("SortByColumns(Accounts, 'Account Name', SortOrder.Ascending, 'Address 1: City')", true)]
        [InlineData("SortByColumns(Accounts, 'Account Name', SortOrder.Descending, 'Non-sortable string column')", false)]
        [InlineData("SortByColumns(Accounts, name, SortOrder.Descending, address1_line1)", true)]
        [InlineData("ShowColumns(Accounts, 'Account Name', 'Address 1: City')", false)]
        [InlineData("RenameColumns(Accounts, 'Account Name', 'The name', 'Address 1: City', 'The city')", false)]
        [InlineData("Search(Accounts, \"something to search\", 'Account Name', address1_line1, 'Address 1: City')", true)]
        [InlineData("Search(Accounts, \"something to search\", 'Account Name', 'Non-searchable string column', 'Address 1: City')", false)]
        [InlineData("Filter(Accounts, IsBlank('Address 1: City'))", true)]
        [InlineData("Filter(Accounts, IsBlank(ThisRecord.'Address 1: City'))", true)]
        [InlineData("Filter(Accounts, Sqrt(ThisRecord.numberofemployees) > 1)", false)]
        [InlineData("CountIf(Accounts, IsBlank('Address 1: City'))", true)]
        [InlineData("CountIf(Accounts, Sqrt(ThisRecord.numberofemployees) > 1)", false)]
        [InlineData("Filter(Accounts, And(Not IsBlank('Address 1: City'), numberofemployees > 100))", true)]
        [InlineData("Sort(Accounts, 'Account Name')", true)]
        [InlineData("Sort(Accounts, 'Account Name', SortOrder.Descending)", true)]
        [InlineData("Sort(Accounts, 'Non-sortable string column', SortOrder.Ascending)", false)]
        public void TestDelegableExpressions_PowerFxV1(string expression, bool isDelegable)
        {
            TestDelegableExpressions(Features.PowerFxV1, expression, isDelegable);
        }

        [Theory]
        [InlineData("SortByColumns(Accounts, Left(\"name\", 4), SortOrder.Descending, address1_line1)", false)]
        public void TestDelegableExpressions_PrePowerFxV1(string expression, bool isDelegable)
        {
            var features = new Features(Features.PowerFxV1)
            {
                PowerFxV1CompatibilityRules = false
            };

            TestDelegableExpressions(features, expression, isDelegable);
        }

        [Theory]
        [InlineData("SortByColumns(Accounts, \"name\", SortOrder.Ascending)", true)]
        [InlineData("SortByColumns(Accounts, \"name\", SortOrder.Ascending, \"address1_city\")", true)]
        [InlineData("SortByColumns(Accounts, \"name\", SortOrder.Descending, \"nonsortablestringcolumn\")", false)]
        [InlineData("SortByColumns(Accounts, \"name\", SortOrder.Descending, \"address1_line1\")", true)]
        [InlineData("SortByColumns(Accounts, varString, SortOrder.Descending)", false)]
        [InlineData("SortByColumns(Accounts, Left(\"name\", 4), SortOrder.Descending)", false)]
        [InlineData("ShowColumns(Accounts, \"name\", \"address1_city\")", false)]
        [InlineData("RenameColumns(Accounts, \"name\", \"The name\", \"address1_city\", \"The city\")", false)]
        [InlineData("Search(Accounts, \"something to search\", \"name\", \"address1_line1\", \"address1_city\")", true)]
        [InlineData("Search(Accounts, \"something to search\", \"name\", \"nonsearchablestringcol\", \"address1_city\")", false)]
        public void TestDelegableExpressions_ColumnNamesAsLiteralStrings(string expression, bool isDelegable)
        {
            var features = new Features(Features.PowerFxV1)
            {
                SupportColumnNamesAsIdentifiers = false
            };

            TestDelegableExpressions(features, expression, isDelegable);
        }

        private void TestDelegableExpressions(Features features, string expression, bool isDelegable)
        {
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddEntity(new AccountsEntity());
            symbolTable.AddVariable("varString", FormulaType.String);

            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(expression);
            Assert.True(result.IsSuccess);

            var callNode = result.Binding.Top.AsCall();
            Assert.NotNull(callNode);

            var callInfo = result.Binding.GetInfo(callNode);

            var actualIsDelegable = callInfo.Function.IsServerDelegatable(callNode, result.Binding);
            Assert.Equal(isDelegable, actualIsDelegable);
        }
    }
}
