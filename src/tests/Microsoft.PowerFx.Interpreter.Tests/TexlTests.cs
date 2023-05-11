// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class InterpreterTexlTests
    {
        [Theory]
        [InlineData("IsMatch(\"LiteralText\", \"LiteralRegEx\")")]
        [InlineData("IsMatch(VariableText, \"LiteralRegEx\")")]
        [InlineData("IsMatch(NotText, \"LiteralRegEx\")")] // Coercion allowed in first argument
        public void TexlFunctionTypeSemanticsIsMatch(string script)
        {
            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("b"),
                symbolTableActions: table =>
                {
                    table.AddVariable("VariableText", FormulaType.String);
                    table.AddVariable("NotText", FormulaType.Number);
                });
        }

        [Theory]
        [InlineData("IsMatch(\"LiteralText\", VariableText)")]
        [InlineData("IsMatch(\"LiteralText\", NotText)")]
        [InlineData("IsMatch(VariableText, VariableText)")]
        [InlineData("IsMatch(VariableText, NotText)")]
        public void TexlFunctionTypeSemanticsIsMatch_Negative(string script)
        {
            TestBindingErrors(
                script,
                TestUtils.DT("b"),
                symbolTableActions: table =>
                {
                    table.AddVariable("VariableText", FormulaType.String);
                    table.AddVariable("NotText", FormulaType.Number);
                });
        }

        [Theory]
        [InlineData("Match(\"LiteralText\", \"LiteralRegEx\")", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n]")]
        [InlineData("Match(VariableText, \"([A-Z][a-z]+)\")", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n]")]
        [InlineData("Match(VariableText, \"(?<Word>[A-Z][a-z]+)\")", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n, Word:s]")]
        [InlineData("Match(VariableText, \"(?<hour>[0-9]{2})\\:(?<minute>[0-9]{2})\\:(?<second>[0-9]{2})\")", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n, hour:s, minute:s, second:s]")]
        [InlineData("Match(NotText, \"LiteralRegEx\")", "![FullMatch:s, StartMatch:n, SubMatches:*[Value:s]]")]
        public void TexlFunctionTypeSemanticsMatch(string script, string expectedDType)
        {
            foreach (var matchAll in new[] { false, true })
            {
                if (matchAll)
                {
                    script = script.Replace("Match(", "MatchAll(");
                    expectedDType = expectedDType.Replace("![FullMatch:", "*[FullMatch:");
                }

                SymbolTable table = new SymbolTable();
                table.AddVariable("VariableText", FormulaType.String);
                table.AddVariable("NotText", FormulaType.Number);

                TestSimpleBindingSuccess(
                    script,
                    TestUtils.DT(expectedDType),
                    symbolTableActions: table =>
                    {
                        table.AddVariable("VariableText", FormulaType.String);
                        table.AddVariable("NotText", FormulaType.Number);
                    });
            }
        }

        [Theory]
        [InlineData("Match(\"An e-mail johndoe@contoso.com\", Match.Email)", "![FullMatch: s, SubMatches: *[Value: s], StartMatch:n]")]
        [InlineData("Match(\"John Doe <john@doe.com>\", \"\\<(?<email>\" & Match.Email & \")\\>\")", "![FullMatch: s, SubMatches: *[Value: s], StartMatch:n, email:s]")]
        [InlineData("Match(\"Hello world\", Concatenate(Match.MultipleNonSpaces, Match.MultipleSpaces, Match.MultipleNonSpaces))", "![FullMatch: s, SubMatches: *[Value: s], StartMatch:n]")]
        public void TexlFunctionTypeSemanticsMatch_MatchEnumeration(string script, string expectedDType)
        {
            foreach (var matchAll in new[] { false, true })
            {
                if (matchAll)
                {
                    script = script.Replace("Match(", "MatchAll(");
                    expectedDType = expectedDType.Replace("![FullMatch:", "*[FullMatch:");
                }

                var expectedType = TestUtils.DT(expectedDType);

                TestSimpleBindingSuccess(
                    script,
                    expectedType,
                    symbolTableActions: table =>
                    {
                        table.AddVariable("VariableText", FormulaType.String);
                        table.AddVariable("NotText", FormulaType.Number); 
                    });
            }
        }

        [Theory]
        [InlineData("Match(\"LiteralText\", VariableText)")]
        [InlineData("Match(\"LiteralText\", NotText)")]
        [InlineData("Match(VariableText, VariableText)")]
        [InlineData("Match(VariableText, NotText)")]
        [InlineData("Match(VariableText, \"BadRegex\\\")")]
        [InlineData("Match(NonStringRegex, 123)")]
        [InlineData("Match(NonSupportedFunction, Text(123))")]
        public void TexlFunctionTypeSemanticsMatch_Negative(string script)
        {
            foreach (var matchAll in new[] { false, true })
            {
                DType expectedType;
                if (matchAll)
                {
                    script = script.Replace("Match(", "MatchAll(");
                    expectedType = DType.EmptyTable;
                }
                else
                {
                    expectedType = DType.EmptyRecord;
                }

                TestBindingErrors(
                    script,
                    expectedType,
                    symbolTableActions: table =>
                    {
                        table.AddVariable("VariableText", FormulaType.String);
                        table.AddVariable("NotText", FormulaType.Number);
                    });
            }
        }

        private void TestBindingErrors(string script, DType expectedType, Action<SymbolTable> symbolTableActions = null, bool numberIsFloat = true, OptionSet[] optionSets = null, Features features = null)
        {
            features = features ?? Features.None;
            var config = new PowerFxConfig(features);
            symbolTableActions?.Invoke(config.SymbolTable);

            config.EnableRegExFunctions(new TimeSpan(0, 0, 5));

            if (optionSets != null)
            {
                foreach (var optionSet in optionSets)
                {
                    config.AddOptionSet(optionSet);
                }
            }

            var engine = new Engine(config);
            var opts = new ParserOptions() { NumberIsFloat = numberIsFloat };
            var result = engine.Check(script, opts);

            Assert.Equal(expectedType, result.Binding.ResultType);
            Assert.False(result.IsSuccess);
        }

        private static void TestSimpleBindingSuccess(string script, DType expectedType, Action<SymbolTable> symbolTableActions = null, Features features = null, bool numberIsFloat = true)
        {
            features ??= Features.None;
            var config = new PowerFxConfig(features);
            symbolTableActions?.Invoke(config.SymbolTable);

            config.EnableRegExFunctions(new TimeSpan(0, 0, 5));

            var engine = new Engine(config);
            var opts = new ParserOptions() { NumberIsFloat = numberIsFloat };
            var result = engine.Check(script, opts);
            Assert.Equal(expectedType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }
    }
}
