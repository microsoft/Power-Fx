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
        public void TexlFunctionTypeSemanticsIsMatch(string script)
        {
            SymbolTable table = new SymbolTable();
            table.AddVariable("VariableText", FormulaType.String);

            TestSimpleBindingSuccess(script, TestUtils.DT("b"), table);                
        }

        [Theory]
        [InlineData("IsMatch(NotText, \"LiteralRegEx\")")]
        [InlineData("IsMatch(\"LiteralText\", VariableText)")]
        [InlineData("IsMatch(\"LiteralText\", NotText)")]
        [InlineData("IsMatch(VariableText, VariableText)")]
        [InlineData("IsMatch(VariableText, NotText)")]
        public void TexlFunctionTypeSemanticsIsMatch_Negative(string script)
        {
            SymbolTable table = new SymbolTable();
            table.AddVariable("VariableText", FormulaType.String);
            table.AddVariable("NotText", FormulaType.Number);

            TestBindingErrors(script, TestUtils.DT("b"), table);
        }

        [Theory]
        [InlineData("Match(\"LiteralText\", \"LiteralRegEx\")", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n]")]
        [InlineData("Match(VariableText, \"([A-Z][a-z]+)\")", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n]")]
        [InlineData("Match(VariableText, \"(?<Word>[A-Z][a-z]+)\")", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n, Word:s]")]
        [InlineData("Match(VariableText, \"(?<hour>[0-9]{2})\\:(?<minute>[0-9]{2})\\:(?<second>[0-9]{2})\")", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n, hour:s, minute:s, second:s]")]
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

                TestSimpleBindingSuccess(script, TestUtils.DT(expectedDType), table);
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
                //using var disposableDocWrapper = NewTestDocumentWithDisposeValidation();
                //var document = disposableDocWrapper.Document;
                //var resolver = new GlobalNameResolver(document);

                TestSimpleBindingSuccess(script, expectedType); //, resolver);
            }
        }

        //[Theory]        
        //[InlineData("FullMatch", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:n]")]
        //[InlineData("SubMatches", "![FullMatch:s, SubMatches:s, StartMatch:n]")]
        //[InlineData("StartMatch", "![FullMatch:s, SubMatches:*[Value:s], StartMatch:s]")]
        //public void TexlFunctionTypeSemanticsMatch_OverridingDefaultFields(string fieldName, string expectedDType)
        //{
        //    foreach (var matchAll in new[] { false, true })
        //    {
        //        string script = $"Match(\"Hello world\", \"OverridingField(?<{fieldName}>.+)\")";
        //        if (matchAll)
        //        {
        //            script = script.Replace("Match(", "MatchAll(");
        //            expectedDType = expectedDType.Replace("![FullMatch:", "*[FullMatch:");
        //        }

        //        var expectedType = TestUtils.DT(expectedDType);
        //        var nameResolver = new TestUtils.MockNameResolver((DName name, out NameLookupInfo info) =>
        //        {
        //            info = default(NameLookupInfo);
        //            return false;
        //        });

        //        var result = TexlParser.ParseScript(script);
        //        Assert.IsFalse(result.HasError);
        //        Assert.IsNotNull(result.Root);

        //        TexlBinding bind = TexlBinding.Run(new DocumentBinderGlue(nameResolver?.Document), result.Root, nameResolver, ConfigurationHelper.GetBindingConfig(null, isBehavior: false));

        //        Assert.IsNotNull(bind, "Test failed for script " + script);
        //        Assert.IsTrue(bind.ErrorContainer.HasErrors());
        //        Assert.AreEqual(expectedType, bind.ResultType);
        //        Assert.IsTrue(bind.ErrorContainer.GetErrors().Count() == 1);
        //        Assert.IsTrue(bind.ErrorContainer.GetErrors().All(x =>
        //        {
        //            return x.ErrorKind == DocumentErrorKind.AXL && x.TextSpan != null && x.ShortMessage.Contains(fieldName);
        //        }));
        //    }
        //}

        [Theory]
        [InlineData("Match(NotText, \"LiteralRegEx\")")]
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

                SymbolTable table = new SymbolTable();
                table.AddVariable("VariableText", FormulaType.String);
                table.AddVariable("NotText", FormulaType.Number);

                TestBindingErrors(script, expectedType, table);
            }
        }

        private void TestBindingErrors(string script, DType expectedType, int expectedErrorCount, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.Equal(expectedType, result.Binding.ResultType);
            Assert.Equal(expectedErrorCount, result.Binding.ErrorContainer.GetErrors().Count());
            Assert.False(result.IsSuccess);
        }

        private void TestBindingErrors(string script, DType expectedType, SymbolTable symbolTable = null, bool numberIsFloat = true, OptionSet[] optionSets = null, Features features = null)
        {
            features = features ?? Features.None;
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

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

        private static void TestSimpleBindingSuccess(string script, DType expectedType, SymbolTable symbolTable = null, Features features = null, bool numberIsFloat = true, IExternalOptionSet[] optionSets = null)
        {
            features ??= Features.None;
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            config.EnableRegExFunctions(new TimeSpan(0, 0, 5));

            if (symbolTable != null)
            {
                config.AddFunction(new ShowColumnsFunction());
                if (optionSets != null)
                {
                    foreach (var optionSet in optionSets)
                    {
                        config.AddEntity(optionSet);
                    }
                }
            }

            var engine = new Engine(config);
            var opts = new ParserOptions() { NumberIsFloat = numberIsFloat };
            var result = engine.Check(script, opts);
            Assert.Equal(expectedType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }
    }
}
