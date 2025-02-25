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
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class DependencyTests : PowerFxTest
    {
        [Theory]
        [InlineData("1+2", "")] // none
        [InlineData("ThisRecord.'Address 1: City' & 'Account Name'", "Entity Accounts: address1_city, name;")] // basic read

        [InlineData("numberofemployees%", "Entity Accounts: numberofemployees;")] // unary op
        [InlineData("ThisRecord", "Entity Accounts: ;")] // whole scope
        [InlineData("{x:5}.x", "")]
        [InlineData("With({x : ThisRecord}, x.'Address 1: City')", "Entity Accounts: address1_city;")] // alias
        [InlineData("With({'Address 1: City' : \"Seattle\"}, 'Address 1: City' & 'Account Name')", "Entity Accounts: name;")] // 'Address 1: City' is shadowed
        [InlineData("With({'Address 1: City' : 5}, ThisRecord.'Address 1: City')", "")] // shadowed
        [InlineData("LookUp(local,'Address 1: City'=\"something\")", "Entity Accounts: address1_city;")] // Lookup and RowScope
        [InlineData("Filter(local,numberofemployees > 200)", "Entity Accounts: numberofemployees;")]
        [InlineData("First(local)", "Entity Accounts: ;")]
        [InlineData("First(local).'Address 1: City'", "Entity Accounts: address1_city;")]
        [InlineData("Last(local)", "Entity Accounts: ;")]
        [InlineData("local", "Entity Accounts: ;")] // whole table
        [InlineData("12 & true & \"abc\" ", "")] // walker ignores literals
        [InlineData("12;'Address 1: City';12", "Entity Accounts: address1_city;")] // chaining
        [InlineData("ParamLocal1.'Address 1: City'", "Entity Accounts: address1_city;")] // basic read
        [InlineData("{test:First(local).name}", "Entity Accounts: name;")]
        [InlineData("AddColumns(simple1, z, 1)", "Entity Simple1: ;")]
        [InlineData("RenameColumns(simple1, b, c)", "Entity Simple1: b;")]
        [InlineData("SortByColumns(simple1, a, SortOrder.Descending)", "Entity Simple1: a;")]
        [InlineData("RenameColumns(If(false, simple1, Error({Kind:ErrorKind.Custom})), b, c)", "Entity Simple1: b;")]
        [InlineData("SortByColumns(If(false, simple1, Error({Kind:ErrorKind.Custom})), a, SortOrder.Descending)", "Entity Simple1: a;")]

        // Basic scoping
        [InlineData("Min(local,numberofemployees)", "Entity Accounts: numberofemployees;")]
        [InlineData("Average(local,numberofemployees)", "Entity Accounts: numberofemployees;")]

        // Patch
        [InlineData("Patch(simple2, First(simple1), { a : 1 })", "Entity Simple1: a, b; Entity Simple2: a, b;")]
        [InlineData("Patch(local, {'Address 1: City':\"test\"}, { 'Account Name' : \"some name\"})", "Entity Accounts: address1_city, name;")]
        [InlineData("Patch(local, {accountid:GUID(), 'Address 1: City':\"test\"})", "Entity Accounts: accountid, address1_city;")]
        [InlineData("Patch(local, Table({accountid:GUID(), 'Address 1: City':\"test\"},{accountid:GUID(), 'Account Name':\"test\"}))", "Entity Accounts: accountid, address1_city, name;")]
        [InlineData("Patch(local, Table({accountid:GUID(), 'Address 1: City':\"test\"},{accountid:GUID(), 'Account Name':\"test\"}),Table({'Address 1: City':\"test\"},{'Address 1: City':\"test\",'Account Name':\"test\"}))", "Entity Accounts: accountid, address1_city, name;")]
        [InlineData("Patch(simple2, First(simple1), { a : First(simple1).b  } )", "Entity Simple1: a, b; Entity Simple2: a, b;")]
        [InlineData("Patch(simple1, First(simple1), { a : First(simple1).b  } )", "Entity Simple1: a, b;")]

        // Remove
        [InlineData("Remove(local, {name: First(remote).name})", "Entity Accounts: name; Entity Contacts: name;")]

        // Collect and ClearCollect.
        [InlineData("Collect(local, Table({ 'Account Name' : \"some name\"}))", "Entity Accounts: name;")]
        [InlineData("Collect(simple2, simple1)", "Entity Simple2: a, b; Entity Simple1: a, b;")]
        [InlineData("Collect(simple2, { a : First(simple1).b  })", "Entity Simple2: a; Entity Simple1: b;")]
        [InlineData("Collect(local, { 'Address 1: City' : First(remote).'Contact Name'  })", "Entity Accounts: address1_city; Entity Contacts: name;")]
        [InlineData("ClearCollect(simple2, simple1)", "Entity Simple2: a, b; Entity Simple1: a, b;")]
        [InlineData("ClearCollect(local, Table({ 'Account Name' : \"some name\"}))", "Entity Accounts: name;")]

        // Inside with.
        [InlineData("With({r: local}, Filter(r, 'Number of employees' > 0))", "Entity Accounts: numberofemployees;")]
        [InlineData("With({r: local}, LookUp(r, 'Number of employees' > 0))", "Entity Accounts: numberofemployees;")]

        // Option set.
        [InlineData("Filter(local, dayofweek = StartOfWeek.Monday)", "Entity Accounts: dayofweek;")]

        [InlineData("Filter(ForAll(local, ThisRecord.numberofemployees), Value < 20)", "Entity Accounts: numberofemployees;")]

        // Summarize is special, becuase of ThisGroup.
        [InlineData("Summarize(local, 'Account Name', Sum(ThisGroup, numberofemployees) As Employees)", "Entity Accounts: name, numberofemployees;")]
        [InlineData("Summarize(local, 'Account Name', Sum(ThisGroup, numberofemployees * 2) As TPrice)", "Entity Accounts: name, numberofemployees;")]

        // Join
        [InlineData("Join(remote As l, local As r, l.contactid = r.contactid, JoinType.Inner, r.name As AccountName)", "Entity Contacts: contactid; Entity Accounts: contactid, name;")]
        [InlineData("Join(remote As l, local As r, l.contactid = r.contactid, JoinType.Inner, r.name As AccountName, l.contactnumber As NewContactNumber)", "Entity Contacts: contactid, contactnumber; Entity Accounts: contactid, name;")]
        [InlineData("Join(remote, local, LeftRecord.contactid = RightRecord.contactid, JoinType.Inner, RightRecord.name As AccountName, LeftRecord.contactnumber As NewContactNumber)", "Entity Contacts: contactid, contactnumber; Entity Accounts: contactid, name;")]

        // Set
        [InlineData("Set(numberofemployees, 200)", "Entity Accounts: numberofemployees;")]
        [InlineData("Set('Address 1: City', 'Account Name')", "Entity Accounts: address1_city, name;")]
        [InlineData("Set('Address 1: City', 'Address 1: City' & \"test\")", "Entity Accounts: address1_city;")]
        [InlineData("Set(NewRecord.'Address 1: City', \"test\")", "Entity Accounts: address1_city;")]

        [InlineData("Filter(Distinct(ShowColumns(simple2, a, b), a), Value < 20)", "Entity Simple2: a, b;")]
        [InlineData("Filter(Distinct(DropColumns(simple2, c), a), Value < 20)", "Entity Simple2: c, a;")]

        [InlineData("AddColumns(simple1, z, a+1)", "Entity Simple1: a;")]
        public void GetDependencies(string expr, string expected)
        {
            var opt = new ParserOptions() { AllowsSideEffects = true };
            var engine = new Engine();

            var check = new CheckResult(engine)
                .SetText(expr, opt)
                .SetBindingInfo(GetSymbols());

            check.ApplyBinding();

#pragma warning disable CS0618 // Type or member is obsolete
            var info = check.ApplyDependencyInfoScan();
#pragma warning restore CS0618 // Type or member is obsolete
            var actual = info.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            Assert.Equal(expected, actual);
        }

        private ReadOnlySymbolTable GetSymbols()
        {
            var localType = Accounts();
            var remoteType = Contacts();
            var simple1Type = Simple1();
            var simple2Type = Simple2();
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
            customSymbols.AddFunction(new RemoveFunction());
            customSymbols.AddVariable("local", localType, mutable: true);
            customSymbols.AddVariable("remote", remoteType, mutable: true);
            customSymbols.AddVariable("simple1", simple1Type, mutable: true);
            customSymbols.AddVariable("simple2", simple2Type, mutable: true);

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

        // Some test cases can produce a long list of dependencies.
        // This method is used to simplify the schema for those cases.
        private TableType Simple1()
        {
            var simplifiedchema = "*[a:w,b:w]";

            DType type = TestUtils.DT2(simplifiedchema);
            var dataSource = new TestDataSource(
                "Simple1",
                type,
                keyColumns: new[] { "a" });

            type = DType.AttachDataSourceInfo(type, dataSource);

            return (TableType)FormulaType.Build(type);
        }

        private TableType Simple2()
        {
            var simplifiedchema = "*[a:w,b:w,c:s]";

            DType type = TestUtils.DT2(simplifiedchema);
            var dataSource = new TestDataSource(
                "Simple2",
                type,
                keyColumns: new[] { "a" });

            type = DType.AttachDataSourceInfo(type, dataSource);

            return (TableType)FormulaType.Build(type);
        }

        /// <summary>
        /// This test case is to ensure that all functions that are not self-contained or 
        /// have a scope info have been assessed and either added to the exception list or overrides <see cref="TexlFunction.ComposeDependencyInfo"/>.
        /// </summary>
        [Fact]
        public void DepedencyScanFunctionTests()
        {
            var names = new List<string>();
            var functions = new List<TexlFunction>();
            functions.AddRange(BuiltinFunctionsCore.BuiltinFunctionsLibrary);

            // These methods use default implementation of ComposeDependencyInfo and do not neeed to override it. 
            var exceptionList = new HashSet<string>()
            {
                "AddColumns",
                "Average",
                "Concat",
                "CountIf",
                "Filter",
                "ForAll",
                "IfError",
                "LookUp",
                "Max",
                "Min",
                "Refresh",
                "Search",
                "Sort",
                "StdevP",
                "Set",
                "Sum",
                "Trace",
                "VarP",
                "With",
            };

            foreach (var func in functions)
            {
                if (!func.IsSelfContained || func.ScopeInfo != null)
                {
                    var irContext = IRContext.NotInSource(FormulaType.String);
                    var node = new CallNode(irContext, func, new ErrorNode(irContext, "test"));
                    var visitor = new DependencyVisitor();
                    var context = new DependencyVisitor.DependencyContext();
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
