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

        // test error messages in v1 and pre-v1
        [Theory]
        [InlineData("a", "In(valid", "ErrInvalidRegExUnclosedCaptureGroups")]
        [InlineData("a", "In\\qvalid", "ErrInvalidRegExBadEscape")]
        [InlineData("a", "In[valid", "ErrInvalidRegExBadSquare")]
        [InlineData("a", "In(*valid", "ErrInvalidRegExBadParen", "ErrInvalidRegExQuantifierOnNothing")]
        public void TestRegExV1DisabledExceptionMessage(string subject, string pattern, string errorV1, string errorPreV1 = null)
        {
            PowerFxConfig configV1 = new PowerFxConfig(Features.PowerFxV1);
            configV1.EnableRegExFunctions(new TimeSpan(0, 0, 3));
            RecalcEngine engineV1 = new RecalcEngine(configV1);

            PowerFxConfig configPreV1 = new PowerFxConfig(Features.None);
            configPreV1.EnableRegExFunctions(new TimeSpan(0, 0, 3));
            RecalcEngine enginePreV1 = new RecalcEngine(configPreV1);

            string[] funcs = { "IsMatch", "Match", "MatchAll" };

            if (errorPreV1 == null)
            {
                errorPreV1 = errorV1;
            }
#if NET462
            // we don't have the more granular errors in 4.6.2, but we also don't have anyone running PreV1 there either
            errorPreV1 = "ErrInvalidRegEx";
#endif

            foreach (var func in funcs)
            {
                var formula = $"{func}(\"{subject}\", \"{pattern}\")";

                var checkV1 = engineV1.Check(formula);
                Assert.False(checkV1.IsSuccess);
                Assert.Equal(checkV1.Errors.First().ResourceKey.Key, errorV1);

                var checkPreV1 = enginePreV1.Check(formula);
                Assert.False(checkPreV1.IsSuccess);
                Assert.Equal(checkPreV1.Errors.First().ResourceKey.Key, errorPreV1);
            }
        }
    }
}
