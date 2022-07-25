// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Tests.IntellisenseTests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class InterpreterSuggestTests : IntellisenseTestBase
    {
        private string[] SuggestStrings(string expression, PowerFxConfig config, RecordType parameterType)
        {
            Assert.NotNull(expression);

            var intellisense = Suggest(expression, config, parameterType);
            return intellisense.Suggestions.Select(suggestion => suggestion.DisplayText.Text).ToArray();
        }

        // Intellisense isn't actually all that great when it comes to context-sensitive suggestions
        // Without a refactor, this is the best it can currently do. 
        // Ideally, for BinaryOp nodes we only suggest things with relevant types,
        // but for now we can at least get them to appear higher in the sort order
        [Theory]
        [InlineData("OptionSet.Optio|", "Option1", "Option2")]
        [InlineData("Option|", "OptionSet", "OtherOptionSet", "TopOptionSetField")]
        [InlineData("Opt|", "OptionSet", "OtherOptionSet", "TopOptionSetField")]
        [InlineData("Opti|on", "OptionSet", "OtherOptionSet", "TopOptionSetField")]
        [InlineData("TopOptionSetField <> |", "OptionSet", "OtherOptionSet")]
        [InlineData("TopOptionSetField <> Op|", "OptionSet", "TopOptionSetField", "OtherOptionSet")]
        public void TestSuggestOptionSets(string expression, params string[] expectedSuggestions)
        {
            var config = PowerFxConfig.BuildWithEnumStore(null, new EnumStoreBuilder(), new TexlFunction[0]);

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
                .Add(new NamedFormulaType("TopOptionSetField", optionSet.FormulaType))
                .Add(new NamedFormulaType("Nested", RecordType.Empty()
                    .Add(new NamedFormulaType("InnerOtherOptionSet", otherOptionSet.FormulaType))));

            var actualSuggestions = SuggestStrings(expression, config, parameterType);
            Assert.Equal(expectedSuggestions, actualSuggestions);
        }

        [Theory]
        [InlineData("Hou|", "Hour", "TimeUnit.Hours")]
        public void TestSuggestHour(string expression, params string[] expectedSuggestions)
        {
            var config = new PowerFxConfig();

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
            var suggestions = recalcEngine.Suggest(suggestion, null, 2);
            var s1 = suggestions.Suggestions.OfType<IntellisenseSuggestion>();

            Assert.NotNull(s1);
            Assert.Equal(8, s1.Count());

            var s = s1.FirstOrDefault(su => su.Text == varName);

            Assert.NotNull(s);
            Assert.Equal("fileIndex", s.DisplayText.Text);
            Assert.Null(s.FunctionName);
            Assert.Equal(SuggestionIconKind.Other, s.IconKind);
            Assert.Equal(SuggestionKind.Global, s.Kind);
            Assert.Equal(DType.Number, s.Type);

            var resolver = new RecalcEngineResolver(recalcEngine, pfxConfig);

            Assert.True(resolver.GlobalSymbols.ContainsKey(varName));
            Assert.Equal(BindKind.PowerFxResolvedObject, resolver.GlobalSymbols[varName].Kind);
            Assert.IsType<RecalcFormulaInfo>(resolver.GlobalSymbols[varName].Data);

            var b = resolver.Lookup(new DName(varName), out var nameInfo, NameLookupPreferences.GlobalsOnly);

            Assert.True(b);
            Assert.Equal(BindKind.PowerFxResolvedObject, nameInfo.Kind);
            Assert.IsType<RecalcFormulaInfo>(nameInfo.Data);
        }

        [Theory]
        [InlineData("d", "displayName")]
        [InlineData("D", "displayName")]
        [InlineData("di", "displayName")]
        [InlineData("DI", "displayName")]
        [InlineData("dis", "displayName")]
        [InlineData("DIs", "displayName")]
        [InlineData("display", "displayName")]
        [InlineData("displayname", "displayName")]
        [InlineData("l", "logicalB")]
        [InlineData("L", "logicalB")]
        [InlineData("lo", "logicalB")]
        [InlineData("LO", "logicalB")]
        [InlineData("logical", "logicalB")]
        [InlineData("logicalB", "logicalB")]
        public void TestSuggestIdentifier(string txt, string expected)
        {
            var pfxConfig = new PowerFxConfig(Features.SupportIdentifiers);
            var recalcEngine = new RecalcEngine(pfxConfig);
            var rt = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Number, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Number));

            var intellisenseResult = recalcEngine.Suggest($"DropColumns(myTable, {txt}", rt, 21 + txt.Length);

            Assert.NotNull(intellisenseResult);
            Assert.NotNull(intellisenseResult.Suggestions);
            Assert.True(intellisenseResult.Suggestions.Any());

            var intellisenseSuggestion = intellisenseResult.Suggestions.FirstOrDefault(s => s.DisplayText.Text == expected) as IntellisenseSuggestion;

            Assert.NotNull(intellisenseSuggestion);
            Assert.Equal(expected, intellisenseSuggestion.Text);
            Assert.Equal(DType.Number, intellisenseSuggestion.Type);
        }
    }
}
