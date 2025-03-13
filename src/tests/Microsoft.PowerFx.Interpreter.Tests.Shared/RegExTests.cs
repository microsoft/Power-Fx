// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
// Allow calling preview EnableRegExFunctions
#pragma warning disable CS0618 // Type or member is obsolete

    public class RegExTests
    {
        [Fact]
        public void TestRegExNegativeTimeout()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new PowerFxConfig().EnableRegExFunctions(TimeSpan.FromMilliseconds(-1));
            });            
        }

        [Fact]
        public void TestRegExNegativeCacheSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {                
                new PowerFxConfig().EnableRegExFunctions(TimeSpan.FromMilliseconds(50), -2);
            });
        }

        [Fact]
        public void TestRegExEnableTwice()
        {
            PowerFxConfig config = new PowerFxConfig();          
            config.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), 20);
            Assert.Throws<InvalidOperationException>(() => config.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), 50));
        }

        [Fact]
        public void TestRegExEnableTwice2()
        {
            PowerFxConfig config = new PowerFxConfig();
            config.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), 20);

            PowerFxConfig config2 = new PowerFxConfig();
            config2.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), 20);
        }

        // catastrophic backtracking
        // 1. short, will succeed with little backtracking
        // 2. short, will fail due to the extra comma at the end of the input, more backtracking but short enough that it completes in a reasonable amount of time
        // 3. long, will still succeed with little backtracking
        // 4. long, will fail due to the extra comma at the end of the input, with a lot of backtracking for such a long input, which will timeout
        [Theory]
        [InlineData("123,123,123,123,123", "^(\\d+,\\d+)+$", false, false, true)]
        [InlineData("123,123,123,123,123,", "^(\\d+,\\d+)+$", false, false, false)]
        [InlineData("123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123", "^(\\d+,\\d+)+$", false, false, true)]
        [InlineData("123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,123,", "^(\\d+,\\d+)+$", false, true, false)]
        public void TestRegExTimeoutWorks(string subject, string pattern, bool subMatches, bool expError, bool expBoolean)
        {
            PowerFxConfig config = new PowerFxConfig();
            config.EnableRegExFunctions(new TimeSpan(0, 0, 3));
            RecalcEngine engine = new RecalcEngine(config);

            var formula = $"IsMatch(\"{subject}\", \"{pattern}\" {(subMatches ? ", MatchOptions.NumberedSubMatches" : string.Empty)})";

            FormulaValue fv = engine.Eval(formula, null, new ParserOptions { AllowsSideEffects = true });

            if (expError)
            {
                Assert.True(fv is ErrorValue);
                ErrorValue ev = (ErrorValue)fv;
                Assert.Equal(ErrorKind.Timeout, ev.Errors.First().Kind);
            }
            else
            {
                Assert.True(fv is BooleanValue);
                BooleanValue bv = (BooleanValue)fv;
                Assert.Equal(expBoolean, bv.Value);
            }
        }

        // test error messages in pre-v1
        // we run these through the .NET regular expression compiler and include the exception.message in our error message to the maker
        // there can be alot of variability in the .NET message, so we just look that there is something there without an exact match
        // in V1, we do our own regular expression validation, with our own localized error messages
        // containsErrorPreV1 is looking for a small part of the .NET message that is hopefully less prone to changing, often the offset of the error
        [Theory]
        [InlineData("a", "In(valid", "ErrInvalidRegExUnclosedCaptureGroups", "8")]
        [InlineData("a", "In\\qvalid", "ErrInvalidRegExBadEscape", "4")]
        [InlineData("a", "In[valid", "ErrInvalidRegExBadSquare", "8")]
        [InlineData("a", "In(*valid", "ErrInvalidRegExBadParen", "4")]
        public void TestRegExV1DisabledExceptionMessage(string subject, string pattern, string errorV1, string containsErrorPreV1)
        {
            PowerFxConfig configV1 = new PowerFxConfig(Features.PowerFxV1);
            configV1.EnableRegExFunctions(new TimeSpan(0, 0, 3));
            RecalcEngine engineV1 = new RecalcEngine(configV1);

            PowerFxConfig configPreV1 = new PowerFxConfig(Features.None);
            configPreV1.EnableRegExFunctions(new TimeSpan(0, 0, 3));
            RecalcEngine enginePreV1 = new RecalcEngine(configPreV1);

            string[] funcs = { "IsMatch", "Match", "MatchAll" };

            foreach (var func in funcs)
            {
                var formula = $"{func}(\"{subject}\", \"{pattern}\")";

                var checkV1 = engineV1.Check(formula);
                Assert.False(checkV1.IsSuccess);
                Assert.Equal(checkV1.Errors.First().ResourceKey.Key, errorV1);

                var checkPreV1 = enginePreV1.Check(formula);
                Assert.False(checkPreV1.IsSuccess);
                Assert.Equal(checkPreV1.Errors.First().ResourceKey.Key, TexlStrings.ErrInvalidRegEx.Key);
                Assert.Contains(containsErrorPreV1, checkPreV1.Errors.First().Message);
            }
        }
    }
}
