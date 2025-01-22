// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Tests.AssociatedDataSourcesTests;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class DependencyTests : PowerFxTest
    {
        [Theory]
        [InlineData("1+2", "")] // none
        [InlineData("ThisRecord.'Address 1: City' & 'Account Name'", "Read Accounts: address1_city, name;")] // basic read

        [InlineData("numberofemployees%", "Read Accounts: numberofemployees;")] // unary op
        [InlineData("ThisRecord", "Read Accounts: ;")] // whole scope
        [InlineData("{x:5}.x", "")]
        [InlineData("With({x : ThisRecord}, x.'Address 1: City')", "Read Accounts: address1_city;")] // alias
        [InlineData("With({'Address 1: City' : \"Seattle\"}, 'Address 1: City' & 'Account Name')", "Read Accounts: name;")] // 'Address 1: City' is shadowed
        [InlineData("With({'Address 1: City' : 5}, ThisRecord.'Address 1: City')", "")] // shadowed
        [InlineData("LookUp(local,'Address 1: City'=\"something\")", "Read Accounts: address1_city;")] // Lookup and RowScope
        [InlineData("Filter(local,numberofemployees > 200)", "Read Accounts: numberofemployees;")]
        [InlineData("First(local)", "Read Accounts: ;")]
        [InlineData("First(local).'Address 1: City'", "Read Accounts: address1_city;")]
        [InlineData("Last(local)", "Read Accounts: ;")]
        [InlineData("local", "Read Accounts: ;")] // whole table
        [InlineData("12 & true & \"abc\" ", "")] // walker ignores literals
        [InlineData("12;'Address 1: City';12", "Read Accounts: address1_city;")] // chaining
        [InlineData("ParamLocal1.'Address 1: City'", "Read Accounts: address1_city;")] // basic read

        // Basic scoping
        [InlineData("Min(local,numberofemployees)", "Read Accounts: numberofemployees;")]
        [InlineData("Average(local,numberofemployees)", "Read Accounts: numberofemployees;")]

        // Patch
        [InlineData("Patch(local, First(local), { 'Account Name' : \"some name\"})", "Read Accounts: ; Write Accounts: name;")]
        [InlineData("Patch(local, {'Address 1: City':\"test\"}, { 'Account Name' : \"some name\"})", "Read Accounts: address1_city; Write Accounts: name;")]
        [InlineData("Patch(local, {accountid:GUID(), 'Address 1: City':\"test\"})", "Read Accounts: accountid; Write Accounts: address1_city;")]
        [InlineData("Patch(local, Table({accountid:GUID(), 'Address 1: City':\"test\"},{accountid:GUID(), 'Account Name':\"test\"}))", "Read Accounts: accountid; Write Accounts: address1_city, name;")]
        [InlineData("Patch(local, Table({accountid:GUID(), 'Address 1: City':\"test\"},{accountid:GUID(), 'Account Name':\"test\"}),Table({'Address 1: City':\"test\"},{'Address 1: City':\"test\",'Account Name':\"test\"}))", "Read Accounts: accountid, address1_city, name; Write Accounts: address1_city, name;")]

        // Collect and ClearCollect.
        [InlineData("Collect(local, Table({ 'Account Name' : \"some name\"}))", "Write Accounts: name;")]
        [InlineData("Collect(local, local)", "Write Accounts: ;")]
        [InlineData("ClearCollect(local, local)", "Write Accounts: ;")]
        [InlineData("ClearCollect(local, Table({ 'Account Name' : \"some name\"}))", "Write Accounts: name;")]

        // Inside with.
        [InlineData("With({r: local}, Filter(r, 'Number of employees' > 0))", "Read Accounts: numberofemployees;")]
        [InlineData("With({r: local}, LookUp(r, 'Number of employees' > 0))", "Read Accounts: numberofemployees;")]

        // Option set.
        [InlineData("Filter(local, dayofweek = StartOfWeek.Monday)", "Read Accounts: dayofweek;")]

        [InlineData("Filter(ForAll(local, ThisRecord.numberofemployees), Value < 20)", "Read Accounts: numberofemployees;")]

        // Summarize is special, becuase of ThisGroup.
        [InlineData("Summarize(local, 'Account Name', Sum(ThisGroup, numberofemployees) As Employees)", "Read Accounts: name, numberofemployees;")]
        [InlineData("Summarize(local, 'Account Name', Sum(ThisGroup, numberofemployees * 2) As TPrice)", "Read Accounts: name, numberofemployees;")]

        // Join
        [InlineData("Join(remote As l, local As r, l.contactid = r.contactid, JoinType.Inner, r.name As AccountName)", "Read Contacts: contactid; Read Accounts: contactid, name;")]
        [InlineData("Join(remote As l, local As r, l.contactid = r.contactid, JoinType.Inner, r.name As AccountName, l.contactnumber As NewContactNumber)", "Read Contacts: contactid, contactnumber; Read Accounts: contactid, name;")]

        // Set
        [InlineData("Set(numberofemployees, 200)", "Write Accounts: numberofemployees;")]
        [InlineData("Set('Address 1: City', 'Account Name')", "Read Accounts: name; Write Accounts: address1_city;")]
        [InlineData("Set('Address 1: City', 'Address 1: City' & \"test\")", "Read Accounts: address1_city; Write Accounts: address1_city;")]
        public void GetDependencies(string expr, string expected)
        {
            var opt = new ParserOptions() { AllowsSideEffects = true };
            var engine = new Engine();

            var check = new CheckResult(engine)
                .SetText(expr, opt)
                .SetBindingInfo(GetSymbols());

            check.ApplyBinding();            

            var info = check.ApplyDependencyAnalysis();
            var actual = info.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            Assert.Equal(expected, actual);
        }

        private ReadOnlySymbolTable GetSymbols()
        {
            var localType = Accounts();
            var remoteType = Contacts();
            var customSymbols = new SymbolTable { DebugName = "Custom symbols " };
            var opt = new ParserOptions() { AllowsSideEffects = true };

            var thisRecordScope = ReadOnlySymbolTable.NewFromRecord(localType.ToRecord(), allowThisRecord: true, allowMutable: true);

            customSymbols.AddFunction(new JoinFunction());
            customSymbols.AddFunction(new CollectFunction());
            customSymbols.AddFunction(new CollectScalarFunction());
            customSymbols.AddFunction(new ClearCollectFunction());
            customSymbols.AddFunction(new ClearCollectScalarFunction());
            customSymbols.AddFunction(new PatchFunction());
            customSymbols.AddFunction(new PatchAggregateFunction());
            customSymbols.AddFunction(new PatchAggregateSingleTableFunction());
            customSymbols.AddFunction(new PatchSingleRecordFunction());
            customSymbols.AddFunction(new SummarizeFunction());
            customSymbols.AddFunction(new RecalcEngineSetFunction());
            customSymbols.AddVariable("local", localType, mutable: true);
            customSymbols.AddVariable("remote", remoteType, mutable: true);

            // Simulate a parameter            
            var parameterSymbols = new SymbolTable { DebugName = "Parameters " };
            parameterSymbols.AddVariable("ParamLocal1", localType.ToRecord(), mutable: true);
            parameterSymbols.AddVariable("NewRecord", localType.ToRecord(), new SymbolProperties() { CanMutate = false, CanSet = false, CanSetMutate = true });

            return ReadOnlySymbolTable.Compose(customSymbols, thisRecordScope, parameterSymbols);
        }

        private TableType Accounts()
        {
            var tableType = (TableType)FormulaType.Build(AccountsTypeHelper.GetDType());
            tableType = tableType.Add("dayofweek", BuiltInEnums.StartOfWeekEnum.FormulaType);
            tableType = tableType.Add("contactid", FormulaType.Guid);

            return tableType;
        }

        private TableType Contacts()
        {
            var simplifiedAccountsSchema = "*[contactid:g, contactnumber:s, name`Contact Name`:s, address1_addresstypecode:l, address1_city`Address 1: City`:s, address1_composite:s, address1_country:s, address1_county:s, address1_line1`Address 1: Street 1`:s, numberofemployees:n]";

            DType contactType = TestUtils.DT2(simplifiedAccountsSchema);
            var dataSource = new TestDataSource(
                "Contacts",
                contactType,
                keyColumns: new[] { "contactid" },
                selectableColumns: new[] { "name", "address1_city", "contactid", "address1_country", "address1_line1" },
                hasCachedCountRows: false);
            var displayNameMapping = dataSource.DisplayNameMapping;
            displayNameMapping.Add("name", "Contact Name");
            displayNameMapping.Add("address1_city", "Address 1: City");
            displayNameMapping.Add("address1_line1", "Address 1: Street 1");
            displayNameMapping.Add("numberofemployees", "Number of employees");

            contactType = DType.AttachDataSourceInfo(contactType, dataSource);

            return (TableType)FormulaType.Build(contactType);
        }

        // Some functions might require an different dependecy scan. This test case is to ensure that any new functions that
        // is not self-contained or has a scope info has been assessed and either added to the exception list or has a dependency scan.
        [Fact]
        public void DepedencyScanFunctionTests()
        {
            var names = new List<string>();
            var functions = new List<TexlFunction>();
            functions.AddRange(BuiltinFunctionsCore.BuiltinFunctionsLibrary);

            var exceptionList = new HashSet<string>()
            {
                "AddColumns",
                "Average",
                "Concat",
                "CountIf",
                "DropColumns",
                "Filter",
                "ForAll",
                "IfError",
                "LookUp",
                "Max",
                "Min",
                "Refresh",
                "RenameColumns",
                "Search",
                "ShowColumns",
                "Sort",
                "SortByColumns",
                "StdevP",
                "Sum",
                "Trace",
                "VarP",
                "With",
            };

            foreach (var func in functions)
            {
                if (!func.IsSelfContained || func.ScopeInfo != null)
                {
                    var visitor = new DependencyVisitor();
                    var context = new DependencyVisitor.DependencyContext();
                    var node = new CallNode(IRContext.NotInSource(FormulaType.String), func);
                    var overwritten = func.ComposeDependencyInfo(node, visitor, context);

                    if (!overwritten && !exceptionList.Contains(func.Name))
                    {
                        names.Add(func.Name);
                    }
                }
            }

            if (names.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("The following functions do not have a dependency scan:");
                foreach (var name in names)
                {
                    sb.AppendLine(name);
                }

                Assert.Fail(sb.ToString());
            }
        }
    }
}
