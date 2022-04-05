// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Tests.IntellisenseTests;
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

        [Theory]
        [InlineData("OptionSet.Optio|", "Option1", "Option2")]
        [InlineData("Option|", "OptionSet")]
        [InlineData("O|", "OptionSet", "OtherOptionSet")]
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
    }
}
