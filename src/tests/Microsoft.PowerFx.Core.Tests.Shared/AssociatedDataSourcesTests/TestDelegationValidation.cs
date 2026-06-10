// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
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
        [InlineData("SortByColumns(Accounts, varString, SortOrder.Descending)", false)]
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
        [InlineData("SortByColumns(Accounts, Left(\"name\", 4), SortOrder.Descending, \"address1_line1\")", false)]
        [InlineData("SortByColumns(Accounts, varString, SortOrder.Descending, \"address1_line1\")", false)]
        [InlineData("SortByColumns(Accounts, \"name\", SortOrder.Descending, \"address1_line1\")", true)]
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

        [Theory]
        [InlineData("UDF1()", "UDF1():Accounts = Accounts;", true)]
        [InlineData("Filter(UDF1(), \"name\" <> \"\")", "UDF1():Accounts = Accounts;", true)]
        public void TestDelegableExpressions_UserDfeinedFunction(string expression, string script, bool isDelegable)
        {
            TestDelegableExpressions(Features.PowerFxV1, expression, isDelegable, script);
        }

        private void TestDelegableExpressions(Features features, string expression, bool isDelegable, string udfScript = null)
        {
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddEntity(new AccountsEntity());
            symbolTable.AddVariable("varString", FormulaType.String);
            symbolTable.AddType(new DName("Accounts"), FormulaType.Build(AccountsTypeHelper.GetDType()));

            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            if (!string.IsNullOrWhiteSpace(udfScript))
            {
                engine.AddUserDefinedFunction(udfScript, CultureInfo.InvariantCulture);
            }

            var result = engine.Check(expression);
            Assert.True(result.IsSuccess);

            var callNode = result.Binding.Top.AsCall();
            Assert.NotNull(callNode);

            var callInfo = result.Binding.GetInfo(callNode);

            var actualIsDelegable = callInfo.Function.IsServerDelegatable(callNode, result.Binding);
            Assert.Equal(isDelegable, actualIsDelegable);

            // validate we can generate the display expression
            string displayExpr = engine.GetDisplayExpression(expression, symbolTable);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestCountRowsWarningForCachedData(bool isCachedData)
        {
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddEntity(new AccountsEntity(isCachedData));
            var config = new PowerFxConfig(Features.PowerFxV1)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check("CountRows(Accounts)");
            Assert.True(result.IsSuccess);

            if (!isCachedData)
            {
                Assert.Empty(result.Errors);
            }
            else
            {
                Assert.Single(result.Errors);
                var error = result.Errors.Single();
                Assert.Equal(ErrorSeverity.Warning, error.Severity);
            }

            // Only shows warning if data source is passed directly to CountRows
            result = engine.Check("CountRows(Filter(Accounts, IsBlank('Address 1: City')))");
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData("varAccount.'Account Name'", false)]
        [InlineData("Coalesce(varAccount.'Account Name', \"fallback\")", false)]
        [InlineData("varAccount.donotallowemails", true)]
        [InlineData("Coalesce(varAccount.donotallowemails, DoNotAllowEmailsOptions.No)", true)]
        [InlineData("Coalesce(varAccount.donotallowemails, false)", true)]
        [InlineData("varAccount.numericoption", true)]
        [InlineData("Coalesce(varAccount.numericoption, NumericOptions.One)", true)]
        [InlineData("Int(varAccount.numericoption)", true)]
        [InlineData("varAccount.stringoption", true)]
        [InlineData("Coalesce(varAccount.stringoption, StringOptions.Alpha)", true)]
        [InlineData("Concatenate(varAccount.stringoption, \" suffix\")", true)]
        [InlineData("DoNotAllowEmailsOptions.No", false)]
        [InlineData("Coalesce(DoNotAllowEmailsOptions.No, false)", false)]
        [InlineData("plainAccount.donotallowemails", false)]
        [InlineData("Coalesce(plainAccount.donotallowemails, false)", false)]
        [InlineData("plainAccount.numericoption", false)]
        [InlineData("Int(plainAccount.numericoption)", false)]
        [InlineData("plainAccount.stringoption", false)]
        [InlineData("Concatenate(plainAccount.stringoption, \" suffix\")", false)]
        [InlineData("varAccountCopy.donotallowemails", true)]
        [InlineData("varAccountCopy.numericoption", true)]
        [InlineData("varAccountCopy.stringoption", true)]
        [InlineData("If(true, varAccountCopy, varAccount).donotallowemails", true)]
        [InlineData("Coalesce(If(true, varAccountCopy, varAccount).donotallowemails, false)", true)]
        [InlineData("If(true, varAccountCopy, varAccount).numericoption", true)]
        [InlineData("If(true, varAccountCopy, varAccount).stringoption", true)]
        public void DataBackedRecordOptionSetField(string expression, bool expectedIsAsync)
        {
            var boolOptionSet = new EnumSymbol(
                new DName("DoNotAllowEmailsOptions"),
                DType.Boolean,
                new Dictionary<string, object>
                {
                   { "Yes", true },
                   { "No", false },
                },
                canCoerceFromBackingKind: true,
                canCoerceToBackingKind: true);

            var numericOptionSet = new EnumSymbol(
                new DName("NumericOptions"),
                DType.Number,
                new Dictionary<string, object>
                {
                   { "One", 1 },
                   { "Two", 2 },
                },
                canCoerceToBackingKind: true);

            var stringOptionSet = new EnumSymbol(
                new DName("StringOptions"),
                DType.String,
                new Dictionary<string, object>
                {
                   { "Alpha", "alpha" },
                   { "Beta", "beta" },
                },
                canCoerceToBackingKind: true);

            var accountsType = AccountsTypeHelper.GetDType()
                .Add(new TypedName(boolOptionSet.FormulaType._type, new DName("donotallowemails")))
                .Add(new TypedName(numericOptionSet.FormulaType._type, new DName("numericoption")))
                .Add(new TypedName(stringOptionSet.FormulaType._type, new DName("stringoption")));

            var dataSource = new TestDataSource("Accounts", accountsType, requiresAsync: true);
            var dataBackedAccountsType = dataSource.Type;

            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddVariable("plainAccount", FormulaType.Build(accountsType.ToRecord()));
            symbolTable.AddVariable("varAccount", FormulaType.Build(dataBackedAccountsType.ToRecord()));
            symbolTable.AddVariable("varAccountCopy", FormulaType.Build(dataBackedAccountsType.ToRecord()));

            var enumStoreBuilder = new EnumStoreBuilder();
            enumStoreBuilder.TestOnly_WithCustomEnum(boolOptionSet);
            enumStoreBuilder.TestOnly_WithCustomEnum(numericOptionSet, append: true);
            enumStoreBuilder.TestOnly_WithCustomEnum(stringOptionSet, append: true);

            var config = PowerFxConfig.BuildWithEnumStore(enumStoreBuilder, Features.PowerFxV1);
            config.SymbolTable = symbolTable;

            var engine = new Engine(config);
            var result = engine.Check(expression);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
            Assert.Equal(expectedIsAsync, result.Binding.IsAsync(result.Binding.Top));
        }

        [Theory]
        [InlineData("varAccount.'Account Name'", false)]
        [InlineData("Coalesce(varAccount.'Account Name', \"fallback\")", false)]
        [InlineData("varAccount.address1_addresstypecode", true)]
        public void DataBackedRecordExistingOptionSetField(string expression, bool expectedIsAsync)
        {
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddVariable("varAccount", FormulaType.Build(AccountsTypeHelper.GetDType().ToRecord()));

            var config = new PowerFxConfig(Features.PowerFxV1)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(expression);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
            Assert.Equal(expectedIsAsync, result.Binding.IsAsync(result.Binding.Top));
        }

        [Fact]
        public void TestDataSourceBackedTableVariablesRemainSync()
        {
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddVariable("localAccounts", FormulaType.Build(AccountsTypeHelper.GetDType()));

            var config = new PowerFxConfig(Features.PowerFxV1)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check("CountRows(localAccounts)");

            Assert.True(result.IsSuccess);
            Assert.False(result.Binding.IsAsync(result.Binding.Top));
        }

        [Fact]
        public void TestDataSourceBackedTableVariableRowScopeFieldsRemainSync()
        {
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddVariable("localAccounts", FormulaType.Build(AccountsTypeHelper.GetDType()));

            var config = new PowerFxConfig(Features.PowerFxV1)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check("Filter(localAccounts, name = \"Contoso\")");

            Assert.True(result.IsSuccess);
            Assert.False(result.Binding.IsAsync(result.Binding.Top));
        }

        [Theory]
        [InlineData("Filter(localAccounts, IsBlank(ThisRecord.address1_addresstypecode))")]
        [InlineData("Filter(localAccounts As account, IsBlank(account.address1_addresstypecode))")]
        public void TestDataSourceBackedTableVariableRowScopeOptionSetFieldsRemainSync(string expression)
        {
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddVariable("localAccounts", FormulaType.Build(AccountsTypeHelper.GetDType()));

            var config = new PowerFxConfig(Features.PowerFxV1)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(expression);

            Assert.True(result.IsSuccess);
            Assert.False(result.Binding.IsAsync(result.Binding.Top));
        }

        [Fact]
        public void TestExpandedDataSourceBackedTableVariablesRemainSync()
        {
            var accountsType = AccountsTypeHelper.GetDType();
            var expandInfo = new TestExpandInfo(accountsType)
            {
                DataSource = new TestDataSource("Accounts", accountsType, requiresAsync: true)
            };
            var expandedAccountsTableType = DType.CopyExpandInfo(accountsType, DType.CreateExpandType(expandInfo));
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddVariable("expandedAccounts", FormulaType.Build(expandedAccountsTableType));

            var config = new PowerFxConfig(Features.PowerFxV1)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check("CountRows(expandedAccounts)");

            Assert.True(result.IsSuccess);
            Assert.False(result.Binding.IsAsync(result.Binding.Top));
        }

        [Theory]
        [InlineData("constAccount.'Account Name'")]
        [InlineData("constAccount.address1_addresstypecode")]
        [InlineData("hostAccount.'Account Name'")]
        [InlineData("hostAccount.address1_addresstypecode")]
        [InlineData("If(true, constAccount, constAccount).address1_addresstypecode")]
        [InlineData("If(true, hostAccount, hostAccount).address1_addresstypecode")]
        public void TestDataSourceBackedResolvedObjectsRemainSync(string expression)
        {
            var accountRecordType = (RecordType)FormulaType.Build(AccountsTypeHelper.GetDType().ToRecord());
            var accountRecordValue = FormulaValue.NewRecordFromFields(
                accountRecordType,
                new NamedValue("name", FormulaValue.New("Contoso")));
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddConstant("constAccount", accountRecordValue);
            symbolTable.AddHostObject("hostAccount", accountRecordType, _ => accountRecordValue);

            var config = new PowerFxConfig(Features.PowerFxV1)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(expression);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
            Assert.False(result.Binding.IsAsync(result.Binding.Top));
        }
    }
}
