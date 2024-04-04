// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Tests.IntellisenseTests;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Tests.CustomFunctions;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class InterpreterSuggestTests : IntellisenseTestBase
    {
        private string[] SuggestStrings(string expression, PowerFxConfig config, RecordType parameterType)
        {
            Assert.NotNull(expression);

            var intellisense = Suggest(expression, config, culture: null, parameterType);
            return intellisense.Suggestions.Select(suggestion => suggestion.DisplayText.Text).ToArray();
        }

        private string[] SuggestStrings(string expression, Engine engine, RecordType parameterType)
        {
            (var expression2, var cursorPosition) = Decode(expression);

            var intellisense = engine.Suggest(expression, parameterType, cursorPosition);

            return intellisense.Suggestions.Select(suggestion => suggestion.DisplayText.Text).ToArray();
        }

        // Intellisense isn't actually all that great when it comes to context-sensitive suggestions
        // Without a refactor, this is the best it can currently do. 
        // Ideally, for BinaryOp nodes we only suggest things with relevant types,
        // but for now we can at least get them to appear higher in the sort order
        [Theory]
        [InlineData("OptionSet.Optio|", "Option1", "Option2")]
        [InlineData("Option|", "OptionSet", "OtherOptionSet", "TopOptionSetField", "TraceOptions", "TraceOptions.IgnoreUnsupportedTypes", "TraceOptions.None")]
        [InlineData("Opt|", "OptionSet", "OtherOptionSet", "TopOptionSetField", "TraceOptions", "TraceOptions.IgnoreUnsupportedTypes", "TraceOptions.None")]
        [InlineData("Opti|on", "OptionSet", "OtherOptionSet", "TopOptionSetField", "TraceOptions", "TraceOptions.IgnoreUnsupportedTypes", "TraceOptions.None")]
        [InlineData("TopOptionSetField <> |", "OptionSet", "TopOptionSetField", "XXX")]
        [InlineData("TopOptionSetField <> Opt|", "OptionSet", "TopOptionSetField", "OtherOptionSet", "TraceOptions", "TraceOptions.IgnoreUnsupportedTypes", "TraceOptions.None")]
        public void TestSuggestOptionSets(string expression, params string[] expectedSuggestions)
        {
            var config = PowerFxConfig.BuildWithEnumStore(new EnumStoreBuilder());

            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            var otherOptionSet = new OptionSet("OtherOptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "99", "OptionA" },
                    { "112", "OptionB" },
                    { "35694", "OptionC" },
                    { "123412983", "OptionD" },
            }));
            config.AddEntity(optionSet);
            config.AddEntity(otherOptionSet);

            var parameterType = RecordType.Empty()
                .Add(new NamedFormulaType("XXX", optionSet.FormulaType)) // Test filtering, shouldn't show up.
                .Add(new NamedFormulaType("TopOptionSetField", optionSet.FormulaType))
                .Add(new NamedFormulaType("Nested", RecordType.Empty()
                    .Add(new NamedFormulaType("InnerOtherOptionSet", otherOptionSet.FormulaType))));

            var engine = new RecalcEngine(config)
            {
                SupportedFunctions = new SymbolTable() // Clear
            };

            var actualSuggestions = SuggestStrings(expression, engine, parameterType);
            Assert.Equal(expectedSuggestions, actualSuggestions);

            // Now try with Globals instead of RowScope
            foreach (var field in parameterType.GetFieldTypes())
            {
                engine.UpdateVariable(field.Name, FormulaValue.NewBlank(field.Type));
            }

            var actualSuggestions2 = SuggestStrings(expression, engine, RecordType.Empty());
            Assert.Equal(expectedSuggestions, actualSuggestions2);
        }

        // Test with display names. 
        [Theory]
        [InlineData("Dis|", "DisplayOpt", "DisplayRowScope")] // Match to row scope       
        public void TestSuggestOptionSetsDisplayName(string expression, params string[] expectedSuggestions)
        {
            var config = PowerFxConfig.BuildWithEnumStore(new EnumStoreBuilder(), new TexlFunctionSet());

            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            config.AddEntity(optionSet, new DName("DisplayOpt"));

            var parameterType = RecordType.Empty()
                .Add(new NamedFormulaType("XXX", optionSet.FormulaType, new DName("DisplayRowScope")));

            var engine = new Engine(config);

            var actualSuggestions = SuggestStrings(expression, engine, parameterType);
            Assert.Equal(expectedSuggestions, actualSuggestions);
        }

        [Theory]
        [InlineData("Hou|", "Hour", "TimeUnit.Hours")]
        public void TestSuggestHour(string expression, params string[] expectedSuggestions)
        {
            // Hour is a function, so that is included with the default set. 
            // We don't suggest "Hours" because that is an unqualified enum. Only suggest qualified enum, "TimeUnit.Hours"
            var config = SuggestTests.Default;

            var actualSuggestions = SuggestStrings(expression, config, null);
            Assert.Equal(expectedSuggestions, actualSuggestions);
        }

        [Theory]
        [InlineData("Fi")]
        [InlineData("fi")]
        [InlineData("fileIndex")]
        [InlineData("FILEINDEX")]
        public void TestSuggestVariableName(string suggestion)
        {
            const string varName = "fileIndex";

            var pfxConfig = new PowerFxConfig();
            var recalcEngine = new RecalcEngine(pfxConfig);

            recalcEngine.UpdateVariable(varName, FormulaValue.New(12));
            var checkResult = recalcEngine.Check(suggestion);
            var suggestions = recalcEngine.Suggest(checkResult, 2);
            var s1 = suggestions.Suggestions.OfType<IntellisenseSuggestion>();

            Assert.NotNull(s1);
            Assert.Equal(8, s1.Count());

            var s = s1.FirstOrDefault(su => su.Text == varName);

            Assert.NotNull(s);
            Assert.Equal("fileIndex", s.DisplayText.Text);
            Assert.Null(s.FunctionName);
            Assert.Equal(SuggestionIconKind.Other, s.IconKind);
            Assert.Equal(SuggestionKind.Global, s.Kind);
            Assert.Equal(DType.Decimal, s.Type);

            var resolver = (IGlobalSymbolNameResolver)recalcEngine.TestCreateResolver();

            var kvp = resolver.GlobalSymbols.FirstOrDefault(gs => gs.Key == varName);

            Assert.True(kvp.Key == varName);
            Assert.Equal(BindKind.PowerFxResolvedObject, kvp.Value.Kind);
            Assert.IsType<NameSymbol>(kvp.Value.Data);

            var b = resolver.Lookup(new DName(varName), out var nameInfo, NameLookupPreferences.GlobalsOnly);

            Assert.True(b);
            Assert.Equal(BindKind.PowerFxResolvedObject, nameInfo.Kind);
            Assert.IsType<NameSymbol>(nameInfo.Data);
        }

        [Theory]
        [InlineData("Collect|", "Collect", "ClearCollect")]
        [InlineData("Patc|", "Patch")]
        [InlineData("Collect(Table({a:1, b:2}), {|", "a:", "b:")]
        [InlineData("Collect(Table({a:1, 'test space': \"test\"), {|", "a:", "'test space':")]
        [InlineData("ClearCollect(Table({a:1, b:2}), {|", "a:", "b:")]
        [InlineData("Remove(Table({a:1, b:2}), {|", "a:", "b:")]
        [InlineData("Error(Ab|  Collect()", "Abs", "Color.OliveDrab", "ErrorKind.NotApplicable", "ErrorKind.ServiceUnavailable", "JSONFormat.FlattenValueTables", "Match.Tab", "Table")]

        //[InlineData("Patch({a:1, b:2}, {|", "a:", "b:")] This test case will demand a binder change to work.
        //[InlineData("Patch(Table({a:1, b:2}), {|", "a:", "b:")] This test case will demand a binder change to work.
        public void TestSuggestMutationFunctions(string expression, params string[] expectedSuggestions)
        {
            var config = SuggestTests.Default;
            config.SymbolTable.EnableMutationFunctions();

            var actualSuggestions = SuggestStrings(expression, config, null);
            Assert.Equal(expectedSuggestions.OrderBy(x => x), actualSuggestions.OrderBy(x => x));
        }

        [Theory]
        [InlineData("Collect(|", "Entity1", "Entity2", "Table1", "table2")]
        [InlineData("Patch(|", "record1", "record2", "User")]
        [InlineData("Remove(|", "Entity1", "Entity2", "Table1", "table2")]

        // doesn't suggest Irrelevant global variables if type1 is non empty aggregate.
        [InlineData("Collect(table2,|", "record2")]
        [InlineData("Patch(table2, record2, |", "record2")]
        [InlineData("Remove(table2, |", "record2")]

        [InlineData("Collect(|, record1", "Entity1", "Entity2", "Table1", "table2")]
        [InlineData("Sum(|", "num", "str")]
        [InlineData("Text(|", "num", "str")]
        [InlineData("Language(|")]
        [InlineData("Filter([1,2], |", "ThisRecord", "Value")]

        // Suggests Enum.
        [InlineData("Text(Now(), |", "str", "DateTimeFormat.LongDate", "DateTimeFormat.LongDateTime", "DateTimeFormat.LongDateTime24", "DateTimeFormat.LongTime", "DateTimeFormat.LongTime24", "DateTimeFormat.ShortDate", "DateTimeFormat.ShortDateTime", "DateTimeFormat.ShortDateTime24", "DateTimeFormat.ShortTime", "DateTimeFormat.ShortTime24", "DateTimeFormat.UTC")]

        // Custom Function has arg with signature of tableType2, So only suggest table2
        [InlineData("RecordsTest(|", "table2")]

        // No suggestion if function is not in binder.
        [InlineData("InvalidFunction(|")]

        // Binary Op Suggestions.
        [InlineData("1 = |", "num")]
        [InlineData("1 + |", "num", "str")]

        [InlineData("Patch(table2,|", "record2")]
        public void TestArgSuggestion(string expression, params string[] expectedSuggestions)
        {
            var map = new SingleSourceDisplayNameProvider(new Dictionary<DName, DName>
            {
                { new DName("dv_entity1"), new DName("Entity1") },
                { new DName("dv_entity2"), new DName("Entity2") }
            });

            // It is important to put TableType as Place Holder for intellisense to work.
            var dataverseMock = ReadOnlySymbolTable.NewFromDeferred(
                map, 
                (disp, logical) => TableType.Empty().Add(new NamedFormulaType("f1", FormulaType.String)), 
                TableType.Empty());

            var config = PowerFxConfig.BuildWithEnumStore(new EnumStoreBuilder().WithDefaultEnums(), new TexlFunctionSet());

            config.SymbolTable.EnableMutationFunctions();

            config.SymbolTable.AddHostObject("User", RecordType.Empty(), (sp) => RecordValue.NewRecordFromFields());
            
            var tableType1 = TableType.Empty();
            config.SymbolTable.AddVariable("table1", tableType1, displayName: "Table1");

            var tableType2 = TableType.Empty().Add(new NamedFormulaType("f1", FormulaType.String));
            config.SymbolTable.AddVariable("table2", tableType2);

            // Do not suggest Deferred.
            config.SymbolTable.AddVariable("deferred", FormulaType.Deferred);

            config.SymbolTable.AddVariable("record1", tableType1.ToRecord());
            config.SymbolTable.AddVariable("record2", tableType2.ToRecord());

            config.SymbolTable.AddVariable("num", FormulaType.Number);
            config.SymbolTable.AddVariable("str", FormulaType.String);

            var customFunction = new TestAggregateIdentityCustomFunction<TableType, TableValue>(tableType2);
            config.AddFunction(customFunction);

            var engine = new RecalcEngine(config);
            var check = engine.Check(expression, null, dataverseMock);

            var cursorPos = expression.IndexOf('|');
            var result = engine.Suggest(check, cursorPos);

            Assert.Equal(expectedSuggestions, result.Suggestions.Select(suggestion => suggestion.DisplayText.Text).ToArray());
        }

        [Theory]

        // record fields.
        [InlineData(
            "RecordInputTest( {|",
            "field1:")]
        [InlineData(
            "RecordInputTest( {field1 : 1}, \"test\", {|",
            "id:",
            "name:")]

        // do not repeat already used fields.
        [InlineData(
            "RecordInputTest( {field1 : 2}, \"test\", { id: 1, |",
            "name:")]

        [InlineData(
            "RecordInputTest( {field1 : 2}, \"test\", { name: \"test\", |",
            "id:")]

        [InlineData(
            "RecordInputTest( {field1 : 2}, \"test\", { id: 1, name:\"test name\", |}")]

        [InlineData(
            "RecordInputTest( {field1 : 2}, \"test\", { id: 1, name: \"test\"}, {|",
            "nested:",
            "nested2:")]

        // nested record field.
        [InlineData(
            "RecordInputTest( {field1 : 3}, \"test\", { id: 1, name: \"test\"}, { nested:{|",
            "field1:")]
        [InlineData(
            "RecordInputTest( {field1 : 4}, \"test\", { id: 1, name: \"test\"}, { nested2:{|",
            "id:",
            "name:")]

        // do not repeat already used fields.
        [InlineData(
            "RecordInputTest( {field1 : 4}, \"test\", { id: 1, name: \"test\"}, { nested2:{ id: 2, |",
            "name:")]
        [InlineData(
            "RecordInputTest( {field1 : 4}, \"test\", { id: 1, name: \"test\"}, { nested2:{ id: 2, name: \"test\", |")]

        [InlineData(
            "RecordInputTest( {field1 : 3}, \"test\", { id: 1, name: \"test\"}, { nested:{ field1: 1}, nested2: {|",
            "id:",
            "name:")]

        [InlineData(
            "RecordInputTest({field1:1}, \"test\", {id:1,name:\"test\"}, {nested2:{id:1,name:\"test\"}},[{nested:{field1:1}}], {topNested:{nested2:{|",
            "id:",
            "name:")]

        // No suggestion, if there is no curly brace open.
        [InlineData(
            "RecordInputTest( {field1 : 3}, \"test\", { id: 1, name: \"test\"}, { nested:{ field1: 1}, nested2: |")]

        // table type arg.
        [InlineData(
            "RecordInputTest( {field1 : 5}, \"test\", { id: 1, name: \"test\"}, { nested2:{ id: 1, name: \"test\"} }, [{|",
            "nested:",
            "nested2:")]

        // table type arg with nested record field.
        [InlineData(
            "RecordInputTest( {field1 : 6}, \"test\", { id: 1, name: \"test\"}, { nested2:{ id: 1, name: \"test\"} }, [ { nested: {|",
            "field1:")]

        [InlineData(
            "RecordInputTest( {field1 : 7}, \"test\", { id: 1, name: \"test\"}, { nested2:{ id: 1, name: \"test\"} }, [ { nested2: {|",
            "id:",
            "name:")]
        public void TestCustomFunctionSuggestion(string expression, params string[] expectedSuggestions)
        {
            var config = SuggestTests.Default;

            config.SymbolTable.EnableMutationFunctions();

            // With Input record type param
            config.AddFunction(new TestRecordInputCustomFunction());

            var actualSuggestions = SuggestStrings(expression, config, null);
            Assert.Equal(expectedSuggestions, actualSuggestions);
        }
    }
}
