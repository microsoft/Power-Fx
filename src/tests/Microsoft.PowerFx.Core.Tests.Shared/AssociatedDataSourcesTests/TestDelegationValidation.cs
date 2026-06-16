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
        [InlineData("recordValue.'Account Name'", false)]
        [InlineData("Coalesce(recordValue.'Account Name', \"fallback\")", false)]
        [InlineData("recordValue.donotallowemails", false)]
        [InlineData("recordValue.numericoption", false)]
        [InlineData("recordValue.stringoption", false)]
        [InlineData("Coalesce(recordValue.donotallowemails, DoNotAllowEmailsOptions.No)", false)]
        [InlineData("Coalesce(recordValue.donotallowemails, false)", false)]
        [InlineData("Not(recordValue.donotallowemails)", true)]
        [InlineData("recordValue.donotallowemails = true", true)]
        [InlineData("recordValue.numericoption = 1", true)]
        [InlineData("Int(recordValue.numericoption)", true)]
        [InlineData("Coalesce(recordValue.stringoption, StringOptions.Alpha)", false)]
        [InlineData("Concatenate(recordValue.stringoption, \" suffix\")", false)]
        [InlineData("If(true, recordValueCopy, recordValue).donotallowemails", false)]
        [InlineData("Coalesce(If(true, recordValueCopy, recordValue).donotallowemails, false)", false)]
        [InlineData("If(true, recordValueCopy, recordValue).numericoption = 1", true)]
        [InlineData("Concatenate(If(true, recordValueCopy, recordValue).stringoption, \" suffix\")", false)]
        public void OptionSetValueCoercionsMarkAsync(string expression, bool expectedIsAsync)
        {
            var config = CreateOptionSetRecordVariableConfig();
            var engine = new Engine(config);
            var result = engine.Check(expression);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
            Assert.Equal(expectedIsAsync, result.Binding.IsAsync(result.Binding.Top));
        }

        [Theory]
        [InlineData("Coalesce(recordValue.donotallowemails, false)", "Boolean")]
        [InlineData("recordValue.address1_addresstypecode", "Number")]
        public void TopLevelOptionSetValueCoercionsMarkAsync(string expression, string coercedKind)
        {
            var config = CreateOptionSetRecordVariableConfig();
            var engine = new Engine(config);
            var result = engine.Check(expression);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
            Assert.False(result.Binding.IsAsync(result.Binding.Top));

            result.Binding.SetCoercedToplevelType(coercedKind switch
            {
                "Boolean" => DType.Boolean,
                "Number" => DType.Number,
                _ => throw new InvalidOperationException($"Unexpected coerced kind: {coercedKind}")
            });

            Assert.True(result.Binding.IsAsync(result.Binding.Top));
        }

        [Theory]
        [InlineData("CountRows(localRecords)")]
        public void OptionSetTableVariablesRemainSyncWithoutCoercion(string expression)
        {
            var boolOptionSet = CreateBooleanOptionSet();
            var accountsType = AccountsTypeHelper.GetDType()
                .Add(new TypedName(boolOptionSet.FormulaType._type, new DName("donotallowemails")));
            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddVariable("localRecords", FormulaType.Build(accountsType));

            var enumStoreBuilder = new EnumStoreBuilder();
            enumStoreBuilder.TestOnly_WithCustomEnum(boolOptionSet);

            var config = PowerFxConfig.BuildWithEnumStore(enumStoreBuilder, Features.PowerFxV1);
            config.SymbolTable = symbolTable;

            var engine = new Engine(config);
            var result = engine.Check(expression);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
            Assert.False(result.Binding.IsAsync(result.Binding.Top));
        }

        private static PowerFxConfig CreateOptionSetRecordVariableConfig()
        {
            var boolOptionSet = CreateBooleanOptionSet();
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
            var variableRecordType = accountsType.ToRecord();

            var symbolTable = new DelegatableSymbolTable();
            symbolTable.AddVariable("recordValue", FormulaType.Build(variableRecordType));
            symbolTable.AddVariable("recordValueCopy", FormulaType.Build(variableRecordType));

            var enumStoreBuilder = new EnumStoreBuilder();
            enumStoreBuilder.TestOnly_WithCustomEnum(boolOptionSet);
            enumStoreBuilder.TestOnly_WithCustomEnum(numericOptionSet, append: true);
            enumStoreBuilder.TestOnly_WithCustomEnum(stringOptionSet, append: true);

            var config = PowerFxConfig.BuildWithEnumStore(enumStoreBuilder, Features.PowerFxV1);
            config.SymbolTable = symbolTable;
            return config;
        }

        private static EnumSymbol CreateBooleanOptionSet()
        {
            return new EnumSymbol(
                new DName("DoNotAllowEmailsOptions"),
                DType.Boolean,
                new Dictionary<string, object>
                {
                    { "Yes", true },
                    { "No", false },
                },
                canCoerceFromBackingKind: true,
                canCoerceToBackingKind: true);
        }
    }
}
