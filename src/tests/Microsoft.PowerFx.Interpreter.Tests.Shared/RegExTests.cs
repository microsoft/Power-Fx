// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.PowerFx.Types;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
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

        // First of these iss a dangerous Regex, will timeout (should take >2h on a fast CPU) - but only MatchOptions.NumberedSubMatches
        [Theory]
        [InlineData("ababababababababababababababababababababababababababababababababababa", "^((ab)*)+$", true, true, true)]
        [InlineData("ababababababababababababababababababababababababababababababababababa", "^((ab)*)+$", false, false, false)]
        [InlineData("ababababababababababababababababababababababababababababababababababa", "^(<one>(<two>ab)*)+$", false, false, false)]
        public void TestRegExTimeoutWorks(string subject, string pattern, bool subMatches, bool expError, bool expBoolean)
        {
            PowerFxConfig config = new PowerFxConfig();
            config.EnableRegExFunctions(new TimeSpan(0, 0, 5));
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
    }
}
