// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
    }
}
