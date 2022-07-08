// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
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

            var parameterType = new RecordType()
                .Add(new NamedFormulaType("TopOptionSetField", optionSet.FormulaType))
                .Add(new NamedFormulaType("Nested", new RecordType()
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

        [Fact]
        public void TestSuggestVariableName()
        {                      
            var engine = new RecalcEngine(new PowerFxConfig());
            engine.UpdateVariable("fileIndex", FormulaValue.New(12));

            var suggestions = engine.Suggest("Fi", null, 2);
            var s = suggestions.Suggestions.OfType<IntellisenseSuggestion>().FirstOrDefault(su => su.Text == "fileIndex");

            Assert.NotNull(s);
            Assert.Equal("fileIndex", s.DisplayText.Text);
            Assert.Null(s.FunctionName);
            Assert.Equal(SuggestionIconKind.Other /* Variable*/ , s.IconKind);
            Assert.Equal(SuggestionKind.Global, s.Kind);            
            Assert.Equal(DType.Number, s.Type);
        }
    }
}
