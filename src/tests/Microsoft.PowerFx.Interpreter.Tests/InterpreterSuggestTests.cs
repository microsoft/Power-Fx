// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
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
using Xunit.Abstractions;

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
        [InlineData("Option|", "OptionSet", "OtherOptionSet", "TopOptionSetField")]
        [InlineData("Opt|", "OptionSet", "OtherOptionSet", "TopOptionSetField")]
        [InlineData("Opti|on", "OptionSet", "OtherOptionSet", "TopOptionSetField")]
        [InlineData("TopOptionSetField <> |", "OptionSet", "OtherOptionSet")]
        [InlineData("TopOptionSetField <> Opt|", "OptionSet", "TopOptionSetField", "OtherOptionSet")]
        public void TestSuggestOptionSets(string expression, params string[] expectedSuggestions)
        {            
            var config = PowerFxConfig.BuildWithEnumStore(null, new EnumStoreBuilder());

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
            var config = PowerFxConfig.BuildWithEnumStore(null, new EnumStoreBuilder(), new TexlFunctionSet<TexlFunction>());

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
            Assert.Equal(DType.Number, s.Type);

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
    }
}
